using Korp.Stock.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

var connectionString =
    builder.Configuration.GetConnectionString("StockDatabase")
    ?? throw new InvalidOperationException(
        "A conexão StockDatabase não foi configurada."
    );

builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseNpgsql(connectionString)
);

var app = builder.Build();

// Desabilitado por padrão; o docker-compose.yml habilita para que uma
// instalação nova já tenha o banco criado.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();

    await scope.ServiceProvider
        .GetRequiredService<StockDbContext>()
        .Database
        .MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();
app.Run();