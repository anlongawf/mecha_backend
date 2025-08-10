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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Config discord

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddDiscord(discordOptions =>
    {
        discordOptions.ClientId = builder.Configuration["Discord:ClientId"];
        discordOptions.ClientSecret = builder.Configuration["Discord:ClientSecret"];
        discordOptions.CallbackPath = "/api/auth/discord/callback";
        discordOptions.SaveTokens = true;

        discordOptions.Scope.Add("identify");
        discordOptions.Scope.Add("email");

        discordOptions.Events.OnCreatingTicket = context =>
        {
            return Task.CompletedTask;
        };
    });
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();