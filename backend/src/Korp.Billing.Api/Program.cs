using System.Text;
using System.Text.Json.Serialization;
using Korp.Billing.Api.Clients;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "BillingDatabase"
        )
    )
);

builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<StockApiClient>(client =>
{
    var stockApiUrl =
        builder.Configuration["Services:StockApi"]
        ?? throw new InvalidOperationException(
            "O endereço do serviço de estoque não foi configurado."
        );

    client.BaseAddress = new Uri(stockApiUrl);
});

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "A chave JWT não foi configurada."
    );

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();