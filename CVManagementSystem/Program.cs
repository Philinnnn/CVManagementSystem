using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
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
using CVManagementSystem.Services.Localization;
using CVManagementSystem.Services.Positions;
using CVManagementSystem.Services.Search;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
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
builder.Services.AddSingleton<IAppLocalizer, JsonAppLocalizer>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.CallbackPath = "/signin-google";
        options.Events.OnCreatingTicket = async context =>
        {
            var providerKey = context.Identity!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(providerKey))
            {
                context.Fail("Google did not return a subject identifier");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var flow = context.Properties!.Items.TryGetValue("flow", out var flowValue) ? flowValue : "login";

            if (flow == "link")
            {
                context.Properties.Items.TryGetValue("userId", out var userIdStr);
                if (!int.TryParse(userIdStr, out var linkUserId))
                {
                    context.Fail("Missing user context for linking");
                    return;
                }

                var externalAuthService = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthService>();
                var linkResult = await externalAuthService.LinkAsync(linkUserId, "Google", providerKey);
                if (!linkResult.Success)
                {
                    context.Fail(linkResult.Error ?? "Failed to link account");
                    return;
                }

                var linkedUser = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstAsync(u => u.Id == linkUserId);
                ReplaceIdentityWithAppClaims(context.Identity!, linkedUser);
                return;
            }

            var email = context.Identity.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.Identity.FindFirst(ClaimTypes.Name)?.Value;

            var authService = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthService>();
            var loginResult = await authService.LoginAsync("Google", providerKey, email, name);
            if (!loginResult.Success)
            {
                context.Fail(loginResult.Error ?? "Login failed");
                return;
            }

            ReplaceIdentityWithAppClaims(context.Identity!, loginResult.Value!.User);

            if (loginResult.Value.IsNewUser)
                context.Properties!.RedirectUri = "/Account/SetPassword?fromSocialSignup=true";
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/Login?externalError=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.CallbackPath = "/signin-github";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.Scope.Add("user:email");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey("urn:github:login", "login");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.UserAgent.ParseAdd("CVManagementSystem");

                var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var githubUser = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                context.RunClaimActions(githubUser.RootElement);

                var providerKey = context.Identity!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(providerKey))
                {
                    context.Fail("GitHub did not return a user id");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var flow = context.Properties!.Items.TryGetValue("flow", out var flowValue) ? flowValue : "login";

                if (flow == "link")
                {
                    context.Properties.Items.TryGetValue("userId", out var userIdStr);
                    if (!int.TryParse(userIdStr, out var linkUserId))
                    {
                        context.Fail("Missing user context for linking");
                        return;
                    }

                    var externalAuthService = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthService>();
                    var linkResult = await externalAuthService.LinkAsync(linkUserId, "GitHub", providerKey);
                    if (!linkResult.Success)
                    {
                        context.Fail(linkResult.Error ?? "Failed to link account");
                        return;
                    }

                    var linkedUser = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstAsync(u => u.Id == linkUserId);
                    ReplaceIdentityWithAppClaims(context.Identity!, linkedUser);
                    return;
                }

                var email = githubUser.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;

                if (string.IsNullOrEmpty(email))
                {
                    var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                    emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    emailRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    emailRequest.Headers.UserAgent.ParseAdd("CVManagementSystem");

                    var emailResponse = await context.Backchannel.SendAsync(emailRequest, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    if (emailResponse.IsSuccessStatusCode)
                    {
                        using var emails = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync());
                        foreach (var e in emails.RootElement.EnumerateArray())
                        {
                            if (e.TryGetProperty("primary", out var isPrimary) && isPrimary.GetBoolean())
                            {
                                email = e.GetProperty("email").GetString();
                                break;
                            }
                        }
                    }
                }

                var name = context.Identity.FindFirst(ClaimTypes.Name)?.Value
                    ?? context.Identity.FindFirst("urn:github:login")?.Value;

                var authService = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthService>();
                var loginResult = await authService.LoginAsync("GitHub", providerKey, email, name);
                if (!loginResult.Success)
                {
                    context.Fail(loginResult.Error ?? "Login failed");
                    return;
                }

                ReplaceIdentityWithAppClaims(context.Identity!, loginResult.Value!.User);

                if (loginResult.Value.IsNewUser)
                    context.Properties!.RedirectUri = "/Account/SetPassword?fromSocialSignup=true";
            },
            OnRemoteFailure = context =>
            {
                context.Response.Redirect("/Account/Login?externalError=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en", "ru"];
    options.SetDefaultCulture(supportedCultures[0]);
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider()
    ];
});
var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

static void ReplaceIdentityWithAppClaims(ClaimsIdentity identity, User user)
{
    foreach (var claim in identity.Claims.ToList())
        identity.RemoveClaim(claim);

    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
    identity.AddClaim(new Claim(ClaimTypes.Name, user.Login));
    identity.AddClaim(new Claim("Fullname", user.Fullname));
    foreach (var ur in user.UserRoles)
        identity.AddClaim(new Claim(ClaimTypes.Role, ur.Role.Name));
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
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

app.UseStaticFiles();

app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<DiscussionHub>("hubs/discussion");

app.Run();