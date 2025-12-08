
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session (Oturum) Servisleri
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();


// Sisteme Cookie (Çerez) ile giriþ yapýlacaðýný ve giriþ yolunu söylüyoruz
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
    options.DefaultSignInScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Hesap/Login";
    options.LogoutPath = "/Hesap/Logout";
    options.AccessDeniedPath = "/Home/ErisimEngellendi";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();


app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();