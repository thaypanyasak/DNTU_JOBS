using DNTU_JOBS.Authorization;
using DNTU_JOBS.Data;
using DNTU_JOBS.Helper;
using DNTU_JOBS.Hubs;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();



builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;  // Yêu cầu email xác thực trước khi bật 2FA
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Khoảng thời gian mặc định
    options.Lockout.MaxFailedAccessAttempts = 5; // Số lần thử truy cập không thành công trước khi Lock
    options.Lockout.AllowedForNewUsers = true; // Cho phép Lock cho người dùng mới

})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(48); 
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Cookie tồn tại trong 30 ngày nếu chọn "Remember Me"
    options.LoginPath = "/Identity/Account/Login";
    //options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true; // Làm mới cookie khi người dùng tiếp tục sử dụng
});



builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;  // Mặc định sẽ là Google nếu không có provider khác
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = new PathString("/callback");
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    options.CallbackPath = new PathString("/signin-facebook");  // Đường dẫn callback khi đăng nhập qua Facebook
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true; 
});

//EmailSender

builder.Services.AddTransient<IEmailSender, EmailSender>();



//Authorize
builder.Services.AddScoped<IAuthorizationHandler, CanManageAccountHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanManageChatHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanManageJobHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageAccountPolicy", policy =>
        policy.Requirements.Add(new CanManageAccountRequirement()));
    options.AddPolicy("CanManageChatPolicy", policy =>
        policy.Requirements.Add(new CanManageChatRequirement()));
    options.AddPolicy("CanManageJobPolicy", policy =>
        policy.Requirements.Add(new CanManageJobRequirement()));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); 
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5242880; // 5 MB
});
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});



builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddMvc();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    

    // Truyền IWebHostEnvironment vào phương thức Initialize
    await SeedData.Initialize(services, context);
}




app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=User}/{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/ChatHub");
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
