using System.Configuration;
using FluentValidation;
using LeakTestService;
using LeakTestService.Configuration;
using LeakTestService.Middleware;
using LeakTestService.Models;
using LeakTestService.Models.Validation;
using LeakTestService.Repositories;
using LeakTestService.Services;
using LeakTestService.Startup;
using RabbitMQ.Client;


var builder = WebApplication.CreateBuilder(args);

// uses the extension method to read from the wanted appsettings.json file. This information is stored in the 
// builder.Configuration().
builder.Host.ConfigureAppSettings();



// Add services to the container. First we add the InfluxDbConfig to the dependency injection container. 
builder.Services.Configure<InfluxDbConfig>(builder.Configuration.GetSection("InfluxDbConfigSettings"));

// Adding the rabbitMqConfigs
builder.Services.Configure<LeakTestRabbitMqConfig>(builder.Configuration.GetSection("RabbitMqConfigurations:LeakTestServiceConfig"));

builder.Services.AddControllers();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<ILeakTestRepository, LeakTestRepository>();
builder.Services.AddTransient<IValidator<LeakTest>, LeakTestValidator>();
builder.Services.AddTransient<IRabbitMqProducer, RabbitMqProducer>();

// Adding the rabbitmq consumer as a singleton
builder.Services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();

// Adding the BackGroundService consumer as a hosted service.
builder.Services.AddHostedService<ConsumerBackgroundService>();


var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var rabbitMqConsumer = app.Services.GetRequiredService<IRabbitMqConsumer>();

lifetime.ApplicationStopping.Register(() => rabbitMqConsumer.Dispose());


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<RoutingMiddleware>();

app.MapControllers();

app.Run();

