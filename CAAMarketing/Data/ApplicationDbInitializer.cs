using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CAAMarketing.Data
{
    public static class ApplicationDbInitializer
    {
        public static async void Seed(IApplicationBuilder applicationBuilder)
        {
            ApplicationDbContext context = applicationBuilder.ApplicationServices.CreateScope()
                .ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                ////Delete the database if you need to apply a new Migration
                //context.Database.EnsureDeleted();
                //Create the database if it does not exist and apply the Migration
                context.Database.Migrate();

                //Create Roles
                var RoleManager = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                string[] roleNames = { "Admin", "Supervisor" };
                IdentityResult roleResult;
                foreach (var roleName in roleNames)
                {
                    var roleExist = await RoleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
                //Create Users
                var userManager = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                if (userManager.FindByEmailAsync("admin@caaniagara.ca").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "admin@caaniagara.ca",
                        Email = "admin@caaniagara.ca"
                    };

                    IdentityResult result = userManager.CreateAsync(user, "Admins@123").Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Admin").Wait();
                    }
                }
                if (userManager.FindByEmailAsync("super@caaniagara.ca").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "super@caaniagara.ca",
                        Email = "super@caaniagara.ca"
                    };

                    IdentityResult result = userManager.CreateAsync(user, "Supers@123").Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Supervisor").Wait();
                    }
                }
                if (userManager.FindByEmailAsync("user@caaniagara.ca").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "user@caaniagara.ca",
                        Email = "user@caaniagara.ca"
                    };

                    IdentityResult result = userManager.CreateAsync(user, "Users@123").Result;
                    //Not in any role
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetBaseException().Message);
            }
        }
    }

}
