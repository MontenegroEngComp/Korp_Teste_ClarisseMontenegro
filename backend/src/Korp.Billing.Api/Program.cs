using System.Text.Json.Serialization;
using Korp.Billing.Api.Clients;
using Korp.Billing.Api.Data;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddHttpClient<StockApiClient>(client =>
{
    var stockApiUrl =
        builder.Configuration["Services:StockApi"]
        ?? throw new InvalidOperationException(
            "O endereço do serviço de estoque não foi configurado."
        );

    client.BaseAddress = new Uri(stockApiUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();