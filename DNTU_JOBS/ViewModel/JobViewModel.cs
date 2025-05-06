using DNTU_JOBS.Models;
using System.ComponentModel.DataAnnotations;

namespace DNTU_JOBS.ViewModel
{
    public class JobViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập vị trí cần tuyển.")]
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập yêu cầu công việc.")]
        public string? JobRequirements { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả công việc.")]
        public string? JobDescription { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập trình độ học vấn.")]
        public string? Education { get; set; }

        public bool? IsActive { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lương tối thiểu.")]
        [Range(0, double.MaxValue, ErrorMessage = "Lương tối thiểu phải lớn hơn hoặc bằng 0.")]
        public decimal? SalaryMin { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lương tối đa.")]
        [Range(0, double.MaxValue, ErrorMessage = "Lương tối đa phải lớn hơn hoặc bằng 0.")]
        public decimal? SalaryMax { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thể loại công việc.")]
        public int? JobCategoryId { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng nhập quyền lợi.")]
        public string? Benefits { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian làm việc.")]
        public string? WorkingTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tuyển.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng tuyển phải lớn hơn hoặc bằng 1.")]
        public int HiringQuantity { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thành phố / huyện.")]
        public int? DistrictId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phường / xã.")]
        public int? WardId { get; set; }  

        // This could be helpful if you want to load Districts and Wards in the View
        public IEnumerable<District> Districts { get; set; } = new List<District>();
        public IEnumerable<Ward> Wards { get; set; } = new List<Ward>();

        //New

        public int? CompanyId { get; set; }

        [StringLength(100, ErrorMessage = "Tên thể loại phải dưới 100 ký tự.")]
        public string? OtherCategoryName { get; set; }
    }
}
