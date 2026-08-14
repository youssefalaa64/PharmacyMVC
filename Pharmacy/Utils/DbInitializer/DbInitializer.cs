using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.DataAccess;
using Pharmacy.Models;
using Pharmacy.Utils;
using Pharmacy.Utils.DbInitializer;

namespace Pharmacy.Utlis.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ILogger<DbInitializer> logger)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (_context.Database.GetPendingMigrations().Any())
                {
                    _context.Database.Migrate();
                }
                if (!_roleManager.Roles.Any())
                {
                    await _roleManager.CreateAsync(new IdentityRole(CD.SUPER_ADMIN_ROLE));
                    await _roleManager.CreateAsync(new IdentityRole(CD.ADMIN_ROLE));
                    await _roleManager.CreateAsync(new IdentityRole(CD.PHARMACIST_ROLE));
                    await _roleManager.CreateAsync(new IdentityRole(CD.CUSTOMER_ROLE));

                    var user = new ApplicationUser()
                    {
                        FirstName = "Super",
                        LastName = "Admin",
                        UserName = "SuperAdmin",
                        Email = "superadmin@eraasoft.com",
                        EmailConfirmed = true,
                    };

                    await _userManager.CreateAsync(user, "SuperAdmin@123");

                    await _userManager.AddToRoleAsync(user, CD.SUPER_ADMIN_ROLE);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex.Message); 
            }
            

        }
    }
}
