using dress_ordering_system.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace dress_ordering_system
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = Directory.GetCurrentDirectory()
            });


            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<myContext>(options =>
     options.UseSqlServer(
         builder.Configuration.GetConnectionString("myconnection")));

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
            
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=customer}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
