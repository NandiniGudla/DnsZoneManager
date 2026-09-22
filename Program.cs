using DnsZoneManager.Data;
using DnsZoneManager.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();  
builder.Services.AddSwaggerGen(); 
builder.Services.AddScoped<IDnsZoneService, DnsZoneService>();
builder.Services.AddSingleton<IDnsZoneRule, DnsZoneRule>();
builder.Services.AddDbContext<DnsContext>(o => o.UseInMemoryDatabase("DnsZoneManagerDb"));

var app = builder.Build();   
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();   

app.Run();


