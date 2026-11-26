using DemoMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers;

[Authorize]
public class ExcelImportController : Controller
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly ExcelImportService _excelImportService;

    public ExcelImportController(ExcelImportService excelImportService)
    {
        _excelImportService = excelImportService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(_excelImportService.GetBusinessTableNames());
    }

    [HttpGet]
    public IActionResult DownloadTemplate(string tableName)
    {
        if (!_excelImportService.GetBusinessTableNames()
            .Contains(tableName, StringComparer.OrdinalIgnoreCase))
        {
            return NotFound("Không tìm thấy bảng dữ liệu nghiệp vụ.");
        }

        var fileContents = _excelImportService.GenerateTemplateExcel(tableName);
        return File(fileContents, ExcelContentType, $"{tableName}_Template.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        [FromForm] string tableName,
        [FromForm] IFormFile file,
        [FromForm] string importMode)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return Json(new { success = false, message = "Vui lòng chọn bảng dữ liệu." });
        }

        if (file is null || file.Length <= 0)
        {
            return Json(new { success = false, message = "Vui lòng chọn file Excel không rỗng." });
        }

        if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Chỉ chấp nhận file có định dạng .xlsx." });
        }

        var isOverwrite = string.Equals(importMode, "overwrite", StringComparison.Ordinal);
        await using var fileStream = file.OpenReadStream();
        var result = await _excelImportService.ImportExcelAsync(tableName, fileStream, isOverwrite);

        return Json(new { success = result.Success, message = result.Message });
    }
}
