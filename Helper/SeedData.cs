using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;

namespace DNTU_JOBS.Helper
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            // Dữ liệu mẫu cho bảng JobCategory
            if (!context.JobCategories.Any())
            {
                var jobCategories = new List<JobCategoryTable>
                {
                    new JobCategoryTable { CategoryName = "Công nghệ thông tin (IT)", Description = "Phát triển phần mềm, quản lý hệ thống mạng, bảo mật thông tin và các dịch vụ CNTT." },
                    new JobCategoryTable { CategoryName = "Kế toán - Kiểm toán", Description = "Quản lý tài chính, hạch toán chi phí, lập báo cáo tài chính và kiểm toán độc lập." },
                    new JobCategoryTable { CategoryName = "Nhân sự (HR)", Description = "Tuyển dụng, đào tạo, quản lý phúc lợi và phát triển nguồn nhân lực." },
                    new JobCategoryTable { CategoryName = "Tiếp thị - Marketing", Description = "Quảng bá thương hiệu, nghiên cứu thị trường và chiến lược truyền thông." },
                    new JobCategoryTable { CategoryName = "Bán hàng (Sales)", Description = "Tiếp cận khách hàng, giới thiệu sản phẩm và đạt mục tiêu doanh số." },
                    new JobCategoryTable { CategoryName = "Hành chính - Văn phòng", Description = "Quản lý công việc hành chính, hỗ trợ hoạt động văn phòng và lưu trữ tài liệu." },
                    new JobCategoryTable { CategoryName = "Kỹ thuật (Engineering)", Description = "Thiết kế, sản xuất, vận hành máy móc, công trình xây dựng và hệ thống kỹ thuật." },
                    new JobCategoryTable { CategoryName = "Y tế - Chăm sóc sức khỏe", Description = "Bác sĩ, y tá, dược sĩ, chuyên gia dinh dưỡng và chăm sóc bệnh nhân." },
                    new JobCategoryTable { CategoryName = "Giáo dục - Đào tạo", Description = "Giảng dạy, nghiên cứu và phát triển kỹ năng cho học sinh, sinh viên và người lao động." },
                    new JobCategoryTable { CategoryName = "Thiết kế đồ họa - Mỹ thuật", Description = "Thiết kế logo, bộ nhận diện thương hiệu, đồ họa web và các sản phẩm sáng tạo." },
                    new JobCategoryTable { CategoryName = "Xây dựng", Description = "Quản lý dự án, giám sát công trình, thi công, thiết kế kiến trúc và kết cấu." },
                    new JobCategoryTable { CategoryName = "Thời trang", Description = "Thiết kế trang phục, quản lý sản xuất và phân phối thời trang." },
                    new JobCategoryTable { CategoryName = "Nông nghiệp", Description = "Trồng trọt, chăn nuôi, công nghệ thực phẩm và phát triển bền vững trong nông nghiệp." },
                    new JobCategoryTable { CategoryName = "Logistics và Chuỗi cung ứng", Description = "Quản lý vận chuyển, kho bãi, xuất nhập khẩu và cung ứng sản phẩm." },
                    new JobCategoryTable { CategoryName = "Dịch vụ khách hàng", Description = "Tư vấn, giải đáp thắc mắc, hỗ trợ khách hàng trước và sau khi mua hàng." },
                    new JobCategoryTable { CategoryName = "Báo chí - Truyền thông", Description = "Phóng viên, biên tập viên, sản xuất nội dung truyền thông và quản lý các chiến dịch truyền thông." },
                    new JobCategoryTable { CategoryName = "Nhà hàng - Khách sạn", Description = "Quản lý khách sạn, nhà hàng, đầu bếp, phục vụ và tổ chức sự kiện." },
                    new JobCategoryTable { CategoryName = "Pháp lý", Description = "Luật sư, cố vấn pháp lý, xử lý tranh chấp và quản lý hợp đồng." },
                    new JobCategoryTable { CategoryName = "Tài chính - Ngân hàng", Description = "Quản lý quỹ, đầu tư, tín dụng, giao dịch ngoại hối và bảo hiểm." },
                    new JobCategoryTable { CategoryName = "Thương mại điện tử (E-commerce)", Description = "Phát triển nền tảng mua sắm trực tuyến, marketing số, quản lý đơn hàng và logistics." }
                };

                context.JobCategories.AddRange(jobCategories);
                await context.SaveChangesAsync();
            }

            // Dữ liệu mẫu cho bảng District
            if (!context.Districts.Any())
            {
                var districts = new List<District>
                {
                    new District { Name = "TP. Biên Hòa" },
                    new District { Name = "TP. Long Khánh" },
                    new District { Name = "Huyện Tân Phú" },
                    new District { Name = "Huyện Vĩnh Cửu" },
                    new District { Name = "Huyện Định Quán" },
                    new District { Name = "Huyện Trảng Bom" },
                    new District { Name = "Huyện Thống Nhất" },
                    new District { Name = "Huyện Cẩm Mỹ" },
                    new District { Name = "Huyện Long Thành" },
                    new District { Name = "Huyện Xuân Lộc" },
                    new District { Name = "Huyện Nhơn Trạch" },
                };

                context.Districts.AddRange(districts);
                await context.SaveChangesAsync();
            }

            // Dữ liệu mẫu cho bảng Ward
            if (!context.Wards.Any())
            {
                var wards = new List<Ward>
                {
                    // Wards of TP. Biên Hòa
                    new Ward { Name = "Phường Trảng Dài", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Phong", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Hiệp", DistrictId = 1 },
                    new Ward { Name = "Phường Hố Nai", DistrictId = 1 },
                    new Ward { Name = "Phường Tam Hòa", DistrictId = 1 },
                    new Ward { Name = "Phường Thống Nhất", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Biên", DistrictId = 1 },
                    new Ward { Name = "Phường Long Bình", DistrictId = 1 },
                    new Ward { Name = "Phường Tam Hiệp", DistrictId = 1 },
                    new Ward { Name = "Phường Bình Đa", DistrictId = 1 },
                    new Ward { Name = "Phường Long Bình Tân", DistrictId = 1 },
                    new Ward { Name = "Phường An Bình", DistrictId = 1 },
                    new Ward { Name = "Phường Bửu Hòa", DistrictId = 1 },
                    new Ward { Name = "Phường Hóa An", DistrictId = 1 },
                    new Ward { Name = "Phường Bửu Long", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Hạnh", DistrictId = 1 },
                    new Ward { Name = "Phường Hiệp Hòa", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Vạn", DistrictId = 1 },
                    new Ward { Name = "Phường Quang Vinh", DistrictId = 1 },
                    new Ward { Name = "Phường Thanh Bình", DistrictId = 1 },
                    new Ward { Name = "Phường Quyết Thắng", DistrictId = 1 },
                    new Ward { Name = "Phường Trung Dũng", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Mai", DistrictId = 1 },
                    new Ward { Name = "Phường Tân Tiến", DistrictId = 1 },
                    new Ward { Name = "Phường Thạnh Phú", DistrictId = 1 },

                    new Ward { Name = "Phường Xuân Trung", DistrictId = 2 },
                    new Ward { Name = "Phường Xuân Thanh", DistrictId = 2 },
                    new Ward { Name = "Phường Xuân An", DistrictId = 2 },
                    new Ward { Name = "Phường Bảo Vinh", DistrictId = 2 },
                    new Ward { Name = "Phường Bàu Sen", DistrictId = 2 },
                    new Ward { Name = "Xã Bảo Quang", DistrictId = 2 },
                    new Ward { Name = "Xã Xuân Lập", DistrictId = 2 },
                    new Ward { Name = "Xã Xuân Tân", DistrictId = 2 },

                    // Wards of TP. Tân Phú
                    new Ward { Name = "Xã Phú Bình", DistrictId = 3 },
                    new Ward { Name = "Xã Phú Điền", DistrictId = 3 },
                    new Ward { Name = "Xã Phú An", DistrictId = 3 },
                    new Ward { Name = "Xã Phú Trung", DistrictId = 3 },
                    new Ward { Name = "Xã Phú Xuân", DistrictId = 3 },
                    new Ward { Name = "Xã Thanh Sơn", DistrictId = 3 },
                    new Ward { Name = "Xã Tà Lài", DistrictId = 3 },

                    // Wards of Huyện Vĩnh Cửu
                    new Ward { Name = "Xã Vĩnh Tân", DistrictId = 4 },
                    new Ward { Name = "Xã Bình Hòa", DistrictId = 4 },
                    new Ward { Name = "Xã Mã Đà", DistrictId = 4 },
                    new Ward { Name = "Xã Thạnh Phú", DistrictId = 4 },
                    new Ward { Name = "Xã Hiếu Liêm", DistrictId = 4 },
                    new Ward { Name = "Xã Trị An", DistrictId = 4 },

                    // Wards of Huyện Định Quán
                    new Ward { Name = "Xã Gia Canh", DistrictId = 5 },
                    new Ward { Name = "Xã Phú Hòa", DistrictId = 5 },
                    new Ward { Name = "Xã Phú Vinh", DistrictId = 5 },
                    new Ward { Name = "Xã Suối Nho", DistrictId = 5 },
                    new Ward { Name = "Xã La Ngà", DistrictId = 5 },
                    new Ward { Name = "Xã Túc Trưng", DistrictId = 5 },

                    // Wards of Huyện Trảng Bom
                    new Ward { Name = "Xã An Viễn", DistrictId = 6 },
                    new Ward { Name = "Xã Bắc Sơn", DistrictId = 6 },
                    new Ward { Name = "Xã Bình Minh", DistrictId = 6 },
                    new Ward { Name = "Xã Cây Gáo", DistrictId = 6 },
                    new Ward { Name = "Xã Đồi 61", DistrictId = 6 },
                    new Ward { Name = "Xã Hưng Thịnh", DistrictId = 6 },

                    // Wards of Huyện Thống Nhất
                    new Ward { Name = "Xã Hưng Lộc", DistrictId = 7 },
                    new Ward { Name = "Xã Lộ 25", DistrictId = 7 },
                    new Ward { Name = "Xã Quang Trung", DistrictId = 7 },
                    new Ward { Name = "Xã Xuân Thạnh", DistrictId = 7 },
                    new Ward { Name = "Xã Gia Kiệm", DistrictId = 7 },
                    new Ward { Name = "Xã Gia Tân 1", DistrictId = 7 },
                    new Ward { Name = "Xã Gia Tân 2", DistrictId = 7 },
                    new Ward { Name = "Xã Gia Tân 3", DistrictId = 7 },

                    // Wards of Huyện Cẩm Mỹ
                    new Ward { Name = "Xã Xuân Đường", DistrictId = 8 },
                    new Ward { Name = "Xã Xuân Mỹ", DistrictId = 8 },
                    new Ward { Name = "Xã Xuân Bảo", DistrictId = 8 },
                    new Ward { Name = "Xã Xuân Đông", DistrictId = 8 },
                    new Ward { Name = "Xã Sông Nhạn", DistrictId = 8 },
                    new Ward { Name = "Xã Sông Ray", DistrictId = 8 },
                    new Ward { Name = "Xã Nhân Nghĩa", DistrictId = 8 },
                    new Ward { Name = "Xã Long Giao", DistrictId = 8 },
                    new Ward { Name = "Xã Bảo Bình", DistrictId = 8 },

                    // Wards of Huyện Long Thành
                    new Ward { Name = "Xã An Phước", DistrictId = 9 },
                    new Ward { Name = "Xã Bình An", DistrictId = 9 },
                    new Ward { Name = "Xã Bình Sơn", DistrictId = 9 },
                    new Ward { Name = "Xã Cẩm Đường", DistrictId = 9 },
                    new Ward { Name = "Xã Lộc An", DistrictId = 9 },
                    new Ward { Name = "Xã Long An", DistrictId = 9 },
                    new Ward { Name = "Xã Long Phước", DistrictId = 9 },
                    new Ward { Name = "Xã Phước Bình", DistrictId = 9 },
                    new Ward { Name = "Xã Phước Thái", DistrictId = 9 },
                    new Ward { Name = "Xã Tam An", DistrictId = 9 },

                    // Wards of Huyện Xuân Lộc
                    new Ward { Name = "Xã Xuân Bắc", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Định", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Hiệp", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Hòa", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Hưng", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Phú", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Tâm", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Thọ", DistrictId = 10 },
                    new Ward { Name = "Xã Xuân Trường", DistrictId = 10 },

                    // Wards of Huyện Nhơn Trạch
                    new Ward { Name = "Xã Đại Phước", DistrictId = 11 },
                    new Ward { Name = "Xã Hiệp Phước", DistrictId = 11 },
                    new Ward { Name = "Xã Long Tân", DistrictId = 11 },
                    new Ward { Name = "Xã Phú Hội", DistrictId = 11 },
                    new Ward { Name = "Xã Phú Thạnh", DistrictId = 11 },
                    new Ward { Name = "Xã Vĩnh Thanh", DistrictId = 11 },
                    new Ward { Name = "Xã Nhơn Trạch", DistrictId = 11 },
                    new Ward { Name = "Xã Quận 2", DistrictId = 11 }
                };

                context.Wards.AddRange(wards);
                await context.SaveChangesAsync();
            }
        }
    }
}
