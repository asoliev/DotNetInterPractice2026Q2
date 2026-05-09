using Microsoft.EntityFrameworkCore;
using TicketingSystem.AsyncApi;
using TicketingSystem.AsyncApi.Services;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.Interfaces;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string dbPath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "..",
    "..",
    "03_persistence_level",
    "sources",
    "ticketing.db"));

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ICartStore, InMemoryCartStore>();
builder.Services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    TicketingDbContext dbContext = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

await app.RunAsync();