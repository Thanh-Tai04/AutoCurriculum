using Microsoft.AspNetCore.Identity;
using AutoCurriculum.Models; 
using Microsoft.EntityFrameworkCore; 

namespace AutoCurriculum.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            // 1. TẠO ROLE "Admin" VÀ "User" 
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
            string adminPassword = "Admin@"; 

            var _user = await userManager.FindByEmailAsync(adminEmail);
            if (_user == null)
            {
                var createPowerUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên Tối Cao", 
                    EmailConfirmed = true
                };

                var createPowerUserResult = await userManager.CreateAsync(createPowerUser, adminPassword);

                if (createPowerUserResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(createPowerUser, "Admin");
                }
            }
            
        }
    }
}