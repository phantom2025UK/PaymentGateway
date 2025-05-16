using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Models.Configuration;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register configuration
builder.Services.Configure<PaymentGatewayConfig>(
    builder.Configuration.GetSection("PaymentGateway"));

// Register repositories and services
builder.Services.AddSingleton<PaymentsRepository>();
builder.Services.AddBankClient(builder.Configuration);
builder.Services.AddScoped<IPaymentValidationService, PaymentValidationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

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
