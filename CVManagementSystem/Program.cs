using CVManagementSystem.Data;
using CVManagementSystem.Hubs;
using CVManagementSystem.Models.Attributes;
using CVManagementSystem.Models.Identity;
using CVManagementSystem.Services.Admin;
using CVManagementSystem.Services.Attributes;
using CVManagementSystem.Services.Auth;
using CVManagementSystem.Services.Candidates;
using CVManagementSystem.Services.Cvs;
using CVManagementSystem.Services.Dashboard;
using CVManagementSystem.Services.Discussions;
using CVManagementSystem.Services.Positions;
using CVManagementSystem.Services.Search;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAttributeService, AttributeService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICvService, CvService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "ru" };
    options.SetDefaultCulture(supportedCultures[0]);
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider()
    ];
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { Name = "Candidate" },
            new Role { Name = "Recruiter" },
            new Role { Name = "Administrator" });
        db.SaveChanges();
    }
    if (!db.Attributes.Any(a => a.IsBuiltIn))
    {
        var personalInfoCategory = db.Categories.FirstOrDefault(c => c.Name == "Personal Information")
                                   ?? new Category { Name = "Personal Information" };

        if (personalInfoCategory.Id == 0)
            db.Categories.Add(personalInfoCategory);

        db.Attributes.AddRange(
            new AttributeDefinition { Name = "First Name", DataType = AttributeDataTypes.String, Category = personalInfoCategory, IsBuiltIn = true, Version = 1 },
            new AttributeDefinition { Name = "Last Name", DataType = AttributeDataTypes.String, Category = personalInfoCategory, IsBuiltIn = true, Version = 1 },
            new AttributeDefinition { Name = "Location", DataType = AttributeDataTypes.String, Category = personalInfoCategory, IsBuiltIn = true, Version = 1 },
            new AttributeDefinition { Name = "Photo", DataType = AttributeDataTypes.Image, Category = personalInfoCategory, IsBuiltIn = true, Version = 1 }
        );
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<DiscussionHub>("/discussion");

app.Run();