using PaymentGateway.Api.Services.Clients;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBankClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IBankClient, BankClient>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:8080");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}
