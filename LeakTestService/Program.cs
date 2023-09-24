using System.Configuration;
using FluentValidation;
using LeakTestService.Configuration;
using LeakTestService.Models;
using LeakTestService.Models.Validation;
using LeakTestService.Repositories;
using LeakTestService.Startup;

var builder = WebApplication.CreateBuilder(args);

// uses the extension method to read from the wanted appsettings.json file. This information is stored in the 
// builder.Configuration().
builder.Host.ConfigureAppSettings();



// Add services to the container. First we add the InfluxDbConfig to the dependency injection container. 
builder.Services.Configure<InfluxDbConfig>(builder.Configuration.GetSection("InfluxDbConfigSettings"));
builder.Services.AddControllers();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ILeakTestRepository, LeakTestRepository>();
builder.Services.AddTransient<IValidator<LeakTest>, LeakTestValidator>();

var app = builder.Build();

// var config = new ConfigurationBuilder()
//     .AddJsonFile("appsettings.json")
//     .AddEnvironmentVariables()
//     .Build();

// Load the influxDB configurations from appsettings.json
// var influxDbConfigSettings = builder.Configuration.GetRequiredSection("InfluxDbConfigSettings").Get<InfluxDbConfig>();
// var bucket = influxDbConfigSettings?.Bucket;
// var url = influxDbConfigSettings?.Url;
// var token = influxDbConfigSettings?.Token;


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