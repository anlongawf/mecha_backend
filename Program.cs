using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.Discord;

var builder = WebApplication.CreateBuilder(args);

// Kết nối MariaDB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MariaDb"),
        new MySqlServerVersion(new Version(10, 5, 0)) // version MariaDB
    ));

builder.Services.AddScoped<JwtService>();

builder.Services.AddControllers();

// Add Data Protection (required for secure cookies)
builder.Services.AddDataProtection();

// Configure Authentication - SINGLE configuration
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/api/auth/access-denied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        
        // Important: Configure cookie for cross-origin requests
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Use SameAsRequest in development
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "MechaAuth";
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    })
    .AddDiscord(discordOptions =>
    {
        discordOptions.ClientId = builder.Configuration["Discord:ClientId"];
        discordOptions.ClientSecret = builder.Configuration["Discord:ClientSecret"];
        discordOptions.CallbackPath = "/api/auth/discord/callback";
        discordOptions.SaveTokens = true;

        discordOptions.Scope.Add("identify");
        discordOptions.Scope.Add("email");

        // Configure correlation cookie for cross-origin requests
        discordOptions.CorrelationCookie.SameSite = SameSiteMode.None;
        discordOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always; // Use SameAsRequest in development

        discordOptions.Events.OnCreatingTicket = context =>
        {
            return Task.CompletedTask;
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enhanced CORS configuration for authentication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")  // Both HTTP and HTTPS
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // Required for cookies
              .SetIsOriginAllowed(origin => true); // Allow any origin in development
    });
});

var app = builder.Build();

// Important: CORS must be before Authentication
app.UseCors("AllowLocalFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();