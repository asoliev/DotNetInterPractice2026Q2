using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketingSystem.AsyncApi;
using TicketingSystem.AsyncApi.Caching;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.Interfaces;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string dbPath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "ticketing.asyncapi.db"));

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IEventResourceCache, EventResourceCache>();
builder.Services.AddSingleton<ISeatBookingGate, SeatBookingGate>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    TicketingDbContext dbContext = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketingSystem.AsyncApi v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();

await app.RunAsync();