using System.Text;
using EPApi.DataAccess;
using EPApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EPApi", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, new List<string>() } });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-change-me-please-0123456789";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EPApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EPApiAudience";
var jwtExpireMinutes = int.TryParse(builder.Configuration["Jwt:ExpireMinutes"], out var exp) ? exp : 60;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(10)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService>(sp => new JwtTokenService(jwtKey, jwtIssuer, jwtAudience, TimeSpan.FromMinutes(jwtExpireMinutes)));
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

await DatabaseInitializer.EnsureCreatedAndSeedAsync(app.Configuration.GetConnectionString("Default"));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();