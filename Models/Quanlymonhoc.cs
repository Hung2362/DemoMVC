using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Quanlymonhoc
    {
        [Key]
        [Required(ErrorMessage = "Mã môn học không được để trống")]
        [Display(Name = "Mã môn học")]
        public string MaMH { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên môn học không được để trống")]
        [Display(Name = "Tên môn học")]
        public string TenMH { get; set; } = string.Empty;

        [Display(Name = "Số tín chỉ")]
        public int SoTinChi { get; set; }

        [Required(ErrorMessage = "Khoa phụ trách không được để trống")]
        [Display(Name = "Khoa phụ trách")]
        public string KhoaPhuTrach { get; set; } = string.Empty;
    }
}
