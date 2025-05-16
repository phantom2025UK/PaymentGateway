// Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PaymentGateway.Api.Models.Configuration;
using PaymentGateway.Api.Services.Clients;
using PaymentGateway.Api.Services.Interfaces;

using Polly;
using Polly.Extensions.Http;

using System;
using System.Net.Http;

namespace PaymentGateway.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBankClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration options
            services.Configure<PaymentGatewayConfig>(
                configuration.GetSection("PaymentGateway"));

            // Register HttpClient with Polly policies
            services.AddHttpClient<IBankClient, BankClient>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<PaymentGatewayConfig>>().Value;

                // Use configuration value or fallback to default
                var baseUrl = string.IsNullOrEmpty(config.BankClient?.BaseUrl)
                    ? "http://localhost:8080"
                    : config.BankClient.BaseUrl;

                var timeout = config.BankClient?.TimeoutSeconds > 0
                    ? config.BankClient.TimeoutSeconds
                    : 15;

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(timeout);
            })
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<PaymentGatewayConfig>>().Value;
                return GetRetryPolicy(serviceProvider, config.Resilience);
            });

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider, ResilienceConfig config)
        {
            // Set default values if configuration is missing
            int maxRetries = config?.MaxRetries > 0 ? config.MaxRetries : 2;
            int initialBackoffSeconds = config?.InitialBackoffSeconds > 0 ? config.InitialBackoffSeconds : 1;

            // Get logger factory for logging
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("HttpClientPolicies");

            // This policy handles network-related exceptions only
            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles HttpRequestException, 5xx and 408 status codes
                .Or<TaskCanceledException>() // Handle timeouts
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    maxRetries,
                    //https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly
                    retryAttempt => TimeSpan.FromSeconds(initialBackoffSeconds * Math.Pow(2, retryAttempt - 1)), // Exponential backoff
                    onRetry: (outcome, timespan, retryAttempt, _) =>
                    {
                        // Log the retry attempt
                        logger.LogWarning("Retrying bank client request after {Timespan}. Attempt {RetryAttempt}. Exception: {Exception}",
                            timespan, retryAttempt, outcome.Exception?.Message);
                    });
        }
    }
}