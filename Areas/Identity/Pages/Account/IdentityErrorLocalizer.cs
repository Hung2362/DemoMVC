using Microsoft.AspNetCore.Identity;

namespace DemoMVC.Areas.Identity.Pages.Account;

internal static class IdentityErrorLocalizer
{
    public static string Translate(IdentityError error)
    {
        return error.Code switch
        {
            "ConcurrencyFailure" => "Dữ liệu tài khoản vừa được thay đổi. Vui lòng thử lại.",
            "DefaultError" => "Đã xảy ra lỗi. Vui lòng thử lại.",
            "DuplicateEmail" => "Địa chỉ email này đã được sử dụng.",
            "DuplicateUserName" => "Tên đăng nhập này đã được sử dụng.",
            "InvalidEmail" => "Địa chỉ email không hợp lệ.",
            "InvalidRoleName" => "Tên vai trò không hợp lệ.",
            "InvalidToken" => "Liên kết xác nhận hoặc đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.",
            "InvalidUserName" => "Tên đăng nhập không hợp lệ.",
            "PasswordMismatch" => "Mật khẩu hiện tại không chính xác.",
            "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
            "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ cái thường.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt.",
            "PasswordRequiresUniqueChars" => "Mật khẩu phải có thêm các ký tự khác nhau.",
            "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ cái in hoa.",
            "PasswordTooShort" => "Mật khẩu chưa đủ độ dài tối thiểu.",
            "RoleNotFound" => "Không tìm thấy vai trò.",
            "UserAlreadyHasPassword" => "Tài khoản đã có mật khẩu.",
            "UserAlreadyInRole" => "Tài khoản đã thuộc vai trò này.",
            "UserLockoutNotEnabled" => "Tài khoản không hỗ trợ khóa đăng nhập.",
            "UserNotInRole" => "Tài khoản không thuộc vai trò này.",
            _ => "Không thể hoàn tất yêu cầu. Vui lòng kiểm tra thông tin và thử lại."
        };
    }
}
