using assestment.Data;
using assestment.Interfaces.Auth;
using assestment.Interfaces.Dashboard;
using assestment.Interfaces.Email;
using assestment.Interfaces.Favorite;
using assestment.Interfaces.Kyc;
using assestment.Interfaces.Property;
using assestment.Interfaces.Reservation;
using assestment.Services.Auth;
using assestment.Services.Dashboard;
using assestment.Services.Email;
using assestment.Services.Favorite;
using assestment.Services.Kyc;
using assestment.Services.Property;
using assestment.Services.Reservation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// db conection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// authentication — cookie-based, como decidimos para MVC clásico
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
    });

builder.Services.AddAuthorization();

// services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAccesService, AccesService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IAiExtractionService, AiExtractionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // <- agregado, debe ir ANTES de UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Property}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();