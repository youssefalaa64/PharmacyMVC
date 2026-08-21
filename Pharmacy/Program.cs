using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Pharmacy.DataAccess;
using Pharmacy.Hubs;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Services;
using Pharmacy.Utils;
using Pharmacy.Utils.DbInitializer;
using Pharmacy.Utlis.DbInitializer;

namespace Pharmacy
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var connectionString =
                builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string"
                    + "'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options => {
                options.UseSqlServer(connectionString);
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
           .AddEntityFrameworkStores<ApplicationDbContext>()
           .AddDefaultTokenProviders();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });
            builder.Services.AddSignalR();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddTransient<IDbInitializer, DbInitializer>();
            builder.Services.AddScoped<IRepository<Customer>, Repository<Customer>>();
            builder.Services.AddScoped<IRepository<Category>, Repository<Category>>();
            builder.Services.AddScoped<IRepository<Product>, Repository<Product>>();
            builder.Services.AddScoped<IRepository<ProductBatch>, Repository<ProductBatch>>();
            builder.Services.AddScoped<IRepository<SalesInvoice>, Repository<SalesInvoice>>();
            builder.Services.AddScoped<IRepository<SalesInvoiceItem>, Repository<SalesInvoiceItem>>();
            builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
            builder.Services.AddScoped<IRepository<OrderItem>, Repository<OrderItem>>();
            builder.Services.AddScoped<IRepository<Cart>, Repository<Cart>>();
            builder.Services.AddScoped<IRepository<CartItem>, Repository<CartItem>>();
            builder.Services.AddScoped<IRepository<Notification>, Repository<Notification>>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
                await dbInitializer.InitializeAsync();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.MapHub<NotificationHub>("/notificationHub");
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=customer}/{controller=home}/{action=index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
