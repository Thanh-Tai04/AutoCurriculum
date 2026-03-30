using Microsoft.AspNetCore.Identity;
using AutoCurriculum.Models; // Đảm bảo gọi đúng Namespace chứa ApplicationUser của bạn

namespace AutoCurriculum.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. TẠO ROLE "Admin" VÀ "User" (Nếu chưa có trong DB)
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. TẠO TÀI KHOẢN ADMIN MẶC ĐỊNH
            string adminEmail = "tainguyen280404@gmail.com"; 
            string adminPassword = "Admin@"; // Mật khẩu phải có chữ hoa, thường, số và ký tự đặc biệt

            // Kiểm tra xem email này đã tồn tại trong DB chưa
            var _user = await userManager.FindByEmailAsync(adminEmail);

            if (_user == null)
            {
                // Nếu chưa có, tiến hành tạo mới
                var createPowerUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên Tối Cao", // Cột FullName từ Model của bạn
                    EmailConfirmed = true
                };

                var createPowerUserResult = await userManager.CreateAsync(createPowerUser, adminPassword);

                // Nếu tạo thành công thì gán quyền "Admin"
                if (createPowerUserResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(createPowerUser, "Admin");
                }
            }
        }
    }
}