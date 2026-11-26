using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using DemoMVC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DemoMVC.Services;

public sealed record ExcelImportResult(bool Success, string Message);

public sealed class ExcelImportService
{
    private readonly ApplicationDbContext _context;

    public ExcelImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<string> GetBusinessTableNames()
    {
        return GetBusinessEntityTypes()
            .Select(entityType => entityType.GetTableName()!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tableName => tableName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public byte[] GenerateTemplateExcel(string tableName)
    {
        var entityType = FindBusinessEntityType(tableName)
            ?? throw new ArgumentException("Không tìm thấy bảng dữ liệu nghiệp vụ.", nameof(tableName));
        var properties = GetImportableProperties(entityType);
        if (properties.Count == 0)
        {
            throw new InvalidOperationException("Entity không có thuộc tính public có thể nhập dữ liệu.");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Template");
        for (var index = 0; index < properties.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = properties[index].Name;
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, properties.Count).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ExcelImportResult> ImportExcelAsync(
        string tableName,
        Stream fileStream,
        bool isOverwrite)
    {
        var entityType = FindBusinessEntityType(tableName);
        if (entityType is null)
        {
            return new ExcelImportResult(false, "Không tìm thấy bảng dữ liệu nghiệp vụ.");
        }

        if (fileStream is null || !fileStream.CanRead)
        {
            return new ExcelImportResult(false, "File Excel không thể đọc.");
        }

        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey is null)
        {
            return new ExcelImportResult(false, "Entity không có khóa chính để kiểm tra dữ liệu.");
        }

        var properties = GetImportableProperties(entityType);
        if (entityType.ClrType.GetConstructor(Type.EmptyTypes) is null)
        {
            return new ExcelImportResult(false, "Entity không có constructor mặc định để tạo dữ liệu.");
        }

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            return new ExcelImportResult(false, $"File Excel không hợp lệ: {exception.Message}");
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet is null)
            {
                return new ExcelImportResult(false, "File Excel không có worksheet.");
            }

            var headerLastCell = worksheet.Row(1).LastCellUsed();
            if (headerLastCell is null)
            {
                return new ExcelImportResult(false, "File Excel không có dòng tiêu đề.");
            }

            var propertiesByName = properties.ToDictionary(
                property => property.Name,
                StringComparer.OrdinalIgnoreCase);
            var columns = new List<(int ColumnIndex, IProperty Property)>();
            var mappedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var headerErrors = new List<string>();
            var lastColumn = headerLastCell.Address.ColumnNumber;

            for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
            {
                var headerCell = worksheet.Cell(1, columnIndex);
                var propertyName = headerCell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    headerErrors.Add($"Cột {columnIndex} chưa có tên thuộc tính.");
                }
                else if (!propertiesByName.TryGetValue(propertyName, out var property))
                {
                    headerErrors.Add($"Không tìm thấy thuộc tính '{propertyName}' trong Entity.");
                }
                else if (!mappedPropertyNames.Add(property.Name))
                {
                    headerErrors.Add($"Thuộc tính '{property.Name}' bị lặp trong tiêu đề.");
                }
                else
                {
                    columns.Add((columnIndex, property));
                }
            }

            foreach (var keyProperty in primaryKey.Properties)
            {
                if (!mappedPropertyNames.Contains(keyProperty.Name))
                {
                    headerErrors.Add($"Thiếu thuộc tính khóa chính '{keyProperty.Name}' trong tiêu đề.");
                }
            }

            if (headerErrors.Count > 0)
            {
                return new ExcelImportResult(
                    false,
                    $"Tiêu đề Excel không hợp lệ: {string.Join(" ", headerErrors)}");
            }

            var entities = new List<object>();
            var inputKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rowErrors = new List<string>();
            var errorCount = 0;
            var skippedCount = 0;
            var emptyRowCount = 0;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                if (Enumerable.Range(1, lastColumn)
                    .All(columnIndex => IsCellEmpty(worksheet.Cell(rowNumber, columnIndex))))
                {
                    emptyRowCount++;
                    continue;
                }

                try
                {
                    var entity = Activator.CreateInstance(entityType.ClrType)
                        ?? throw new InvalidOperationException("Không thể tạo Entity.");

                    foreach (var (columnIndex, property) in columns)
                    {
                        var cell = worksheet.Cell(rowNumber, columnIndex);
                        var propertyInfo = property.PropertyInfo!;
                        if (primaryKey.Properties.Contains(property) && IsCellEmpty(cell))
                        {
                            throw new FormatException($"Khóa chính '{property.Name}' không được để trống.");
                        }

                        propertyInfo.SetValue(entity, ConvertCellValue(cell, propertyInfo.PropertyType));
                    }

                    var keyValues = primaryKey.Properties
                        .Select(property => property.PropertyInfo!.GetValue(entity))
                        .ToArray();
                    var keyToken = CreateKeyToken(keyValues);
                    if (!inputKeys.Add(keyToken))
                    {
                        if (isOverwrite)
                        {
                            throw new FormatException("Khóa chính bị trùng trong file Excel.");
                        }

                        skippedCount++;
                        continue;
                    }

                    entities.Add(entity);
                }
                catch (Exception exception) when (
                    exception is FormatException or
                    OverflowException or
                    InvalidCastException or
                    ArgumentException or
                    TargetInvocationException or
                    MemberAccessException or
                    InvalidOperationException)
                {
                    errorCount++;
                    if (rowErrors.Count < 10)
                    {
                        rowErrors.Add($"Dòng {rowNumber}: {exception.Message}");
                    }
                }
            }

            if (errorCount > 0)
            {
                return new ExcelImportResult(
                    false,
                    $"Không có dữ liệu nào được ghi do file có {errorCount} dòng lỗi. " +
                    $"Đã thêm: 0; bỏ qua do trùng khóa: {skippedCount}; " +
                    $"dòng trống: {emptyRowCount}; lỗi: {errorCount}. " +
                    string.Join(" ", rowErrors));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var addedCount = 0;
            try
            {
                if (isOverwrite)
                {
                    addedCount = await ReplaceRowsAsync(entityType.ClrType, entities);
                }
                else
                {
                    foreach (var entity in entities)
                    {
                        var keyValues = primaryKey.Properties
                            .Select(property => property.PropertyInfo!.GetValue(entity))
                            .ToArray();
                        var existingEntity = await _context.FindAsync(entityType.ClrType, keyValues);
                        if (existingEntity is not null)
                        {
                            skippedCount++;
                            continue;
                        }

                        _context.Add(entity);
                        addedCount++;
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync();
                return new ExcelImportResult(
                    false,
                    $"Không thể ghi dữ liệu. Đã thêm: 0; bỏ qua do trùng khóa: {skippedCount}; " +
                    $"dòng trống: {emptyRowCount}; lỗi: 1. " +
                    exception.GetBaseException().Message);
            }

            return new ExcelImportResult(
                true,
                $"Nhập Excel thành công. Đã thêm: {addedCount}; bỏ qua do trùng khóa: {skippedCount}; " +
                $"dòng trống: {emptyRowCount}; lỗi: 0.");
        }
    }

    private IReadOnlyList<IEntityType> GetBusinessEntityTypes()
    {
        return _context.Model.GetEntityTypes()
            .Where(entityType =>
                entityType.ClrType is { IsClass: true, IsAbstract: false } &&
                entityType.GetTableName() is not null &&
                !IsIdentityEntity(entityType))
            .ToList();
    }

    private IEntityType? FindBusinessEntityType(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return null;
        }

        return GetBusinessEntityTypes().FirstOrDefault(entityType =>
            string.Equals(
                entityType.GetTableName(),
                tableName.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsIdentityEntity(IEntityType entityType)
    {
        var tableName = entityType.GetTableName() ?? string.Empty;
        var clrType = entityType.ClrType;
        var clrTypeName = clrType.Name;
        var clrTypeNamespace = clrType.Namespace ?? string.Empty;

        return HasIdentityPrefix(tableName) ||
            HasIdentityPrefix(clrTypeName) ||
            clrTypeNamespace.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasIdentityPrefix(string name)
    {
        return name.StartsWith("Identity", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<IProperty> GetImportableProperties(IEntityType entityType)
    {
        return entityType.GetProperties()
            .Where(property =>
                !property.IsShadowProperty() &&
                property.PropertyInfo is
                {
                    GetMethod.IsPublic: true,
                    SetMethod.IsPublic: true
                } propertyInfo &&
                propertyInfo.GetIndexParameters().Length == 0)
            .ToList();
    }

    private static bool IsCellEmpty(IXLCell cell)
    {
        return cell.Value.IsBlank ||
            (cell.Value.IsText && string.IsNullOrWhiteSpace(cell.GetString()));
    }

    private static object? ConvertCellValue(IXLCell cell, Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (targetType == typeof(string))
        {
            return cell.GetString();
        }

        if (IsCellEmpty(cell))
        {
            if (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) is not null)
            {
                return null;
            }

            return Activator.CreateInstance(propertyType);
        }

        if (targetType == typeof(int))
        {
            if (cell.TryGetValue<int>(out var value))
            {
                return value;
            }
        }
        else if (targetType == typeof(double))
        {
            if (cell.TryGetValue<double>(out var value))
            {
                return value;
            }
        }
        else if (targetType == typeof(decimal))
        {
            if (cell.TryGetValue<decimal>(out var value))
            {
                return value;
            }
        }
        else if (targetType == typeof(DateTime))
        {
            if (cell.TryGetValue<DateTime>(out var value))
            {
                return value;
            }

            var dateText = cell.GetString();
            if (DateTime.TryParse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date) ||
                DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date;
            }
        }
        else if (targetType == typeof(bool))
        {
            if (cell.TryGetValue<bool>(out var value))
            {
                return value;
            }

            var booleanText = cell.GetString();
            if (booleanText == "1")
            {
                return true;
            }

            if (booleanText == "0")
            {
                return false;
            }
        }
        else
        {
            var text = cell.GetString();
            if (targetType == typeof(Guid))
            {
                return Guid.Parse(text);
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, text, ignoreCase: true);
            }

            return Convert.ChangeType(text, targetType, CultureInfo.CurrentCulture);
        }

        throw new FormatException($"Giá trị '{cell.GetString()}' không đúng kiểu {targetType.Name}.");
    }

    private static string CreateKeyToken(IEnumerable<object?> keyValues)
    {
        return string.Join(
            "|",
            keyValues.Select(value =>
            {
                var text = value switch
                {
                    null => string.Empty,
                    DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => value.ToString()
                } ?? string.Empty;

                if (value is string)
                {
                    text = text.ToUpperInvariant();
                }

                return $"{text.Length}:{text}";
            }));
    }

    private async Task<int> ReplaceRowsAsync(Type entityClrType, List<object> entities)
    {
        var method = typeof(ExcelImportService)
            .GetMethod(nameof(ReplaceRowsGenericAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(entityClrType);
        var operation = (Task<int>)method.Invoke(this, [entities])!;
        return await operation;
    }

    private async Task<int> ReplaceRowsGenericAsync<TEntity>(List<object> entities)
        where TEntity : class
    {
        var entitySet = _context.Set<TEntity>();
        var oldEntities = await entitySet.ToListAsync();
        if (oldEntities.Count > 0)
        {
            entitySet.RemoveRange(oldEntities);
            await _context.SaveChangesAsync();
        }

        if (entities.Count == 0)
        {
            return 0;
        }

        _context.AddRange(entities);
        return await _context.SaveChangesAsync();
    }
}
