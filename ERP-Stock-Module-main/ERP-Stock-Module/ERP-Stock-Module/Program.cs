using Application.IServices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Infrastructure.Services;
using Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);
// إعداد Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // يقرأ الإعدادات من appsettings.json
    .WriteTo.Console()                             // يكتب على شاشة Terminal
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day) // يكتب في ملف يومي
    .CreateLogger();

builder.Host.UseSerilog(); // استبدال الـ Logger الافتراضي بـ Serilog

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Stock"),
        b => b.MigrationsAssembly("Infrastructure")   
    ));



// داخل builder.Services
builder.Services.AddScoped<IStockAllocationService, StockAllocationService>();
builder.Services.AddScoped<ISellService, SellService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSignalR();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ILotRepository, LotRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapHub<StockHub>("/stockHub");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
