using System.Text;
using CvPlatform.API.Security;
using CvPlatform.API.Services;
using CvPlatform.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using CvPlatform.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.OAuth;
using CvPlatform.Infrastructure.Integrations.Salesforce;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.Configure<SalesforceOptions>(builder.Configuration.GetSection("Salesforce"));
builder.Services.AddHttpClient<SalesforceService>();
builder.Services.Configure<CvPlatform.Infrastructure.Integrations.Support.SupportOptions>(builder.Configuration.GetSection("Support"));
builder.Services.AddScoped<CvPlatform.Infrastructure.Integrations.Support.DropboxConnectionStore>();
builder.Services.AddHttpClient("dropbox-oauth", client => client.Timeout = TimeSpan.FromSeconds(45))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient<CvPlatform.Infrastructure.Integrations.Support.DropboxTicketUploader>(client => client.Timeout = TimeSpan.FromSeconds(45))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("support", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        { PermitLimit = 3, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = CurrentUserValidation.Validate
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var oauthEnabled = builder.Configuration.GetValue<bool>("OAuth:Enabled");
var googleClientId = builder.Configuration["OAuth:Google:ClientId"];
var googleClientSecret = builder.Configuration["OAuth:Google:ClientSecret"];
if (oauthEnabled && !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        ConfigureExternalProvider(options);
    });
}

var facebookAppId = builder.Configuration["OAuth:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["OAuth:Facebook:AppSecret"];
if (oauthEnabled && !string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authenticationBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        ConfigureExternalProvider(options);
    });
}

if (oauthEnabled)
{
    authenticationBuilder.AddCookie(IdentityConstants.ExternalScheme, options =>
    {
        options.Cookie.Name = "CvPlatform.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    });
}

builder.Services.AddAuthorization();

builder.Services.AddCors(options => options.AddPolicy("spa", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

var publicOriginValue = builder.Configuration["OAuth:PublicOrigin"];
if (oauthEnabled && !string.IsNullOrWhiteSpace(publicOriginValue))
{
    var publicOrigin = new Uri(publicOriginValue, UriKind.Absolute);
    if (publicOrigin.Scheme != Uri.UriSchemeHttps || publicOrigin.AbsolutePath != "/"
        || publicOrigin.Query.Length > 0 || publicOrigin.Fragment.Length > 0 || publicOrigin.UserInfo.Length > 0)
        throw new InvalidOperationException("OAuth:PublicOrigin must be an HTTPS origin without a path.");

    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api/account")
            || context.Request.Path == "/signin-google" || context.Request.Path == "/signin-facebook")
        {
            context.Request.Scheme = publicOrigin.Scheme;
            context.Request.Host = HostString.FromUriComponent(publicOrigin.IsDefaultPort
                ? publicOrigin.Host : publicOrigin.Authority);
        }
        await next();
    });
}

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = exception switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ForbiddenException => StatusCodes.Status403Forbidden,
        ConcurrencyConflictException => StatusCodes.Status409Conflict,
        ValidationException => StatusCodes.Status400BadRequest,
        InvalidOperationException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
    await context.Response.WriteAsJsonAsync(new { message = exception?.Message ?? "Unexpected error." });
}));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));
    app.MapGet("/swagger/index.html", () => Results.Content("""
        <!doctype html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <title>CvPlatform API</title>
            <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
        </head>
        <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
            <script>SwaggerUIBundle({ url: '/openapi/v1.json', dom_id: '#swagger-ui' });</script>
        </body>
        </html>
        """, "text/html"));
}

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

var spaCandidates = new[]
{
    Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "CvPlatform.Web", "wwwroot"))
};
var spaRoot = Array.Find(spaCandidates, Directory.Exists);

if (spaRoot is not null)
{
    var spaFiles = new PhysicalFileProvider(spaRoot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = spaFiles });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = spaFiles });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = spaFiles });
}
else if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var requestPath = context.Request.Path.Value ?? string.Empty;
        if (requestPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var queryString = context.Request.QueryString.Value ?? string.Empty;
        context.Response.Redirect("http://localhost:5173" + requestPath + queryString, permanent: false);
    });
}

await CvPlatform.Infrastructure.Persistence.SeedData.EnsureSeededAsync(app.Services);

app.Run();

void ConfigureExternalProvider(OAuthOptions options)
{
    options.SignInScheme = IdentityConstants.ExternalScheme;
    options.SaveTokens = false;
    options.Events.OnRemoteFailure = context =>
    {
        context.HandleResponse();
        var frontend = (builder.Configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');
        context.Response.Redirect(frontend + "/login?error=external");
        return Task.CompletedTask;
    };
    options.Events.OnAccessDenied = context =>
    {
        context.HandleResponse();
        var frontend = (builder.Configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');
        context.Response.Redirect(frontend + "/login?error=cancelled");
        return Task.CompletedTask;
    };
}
