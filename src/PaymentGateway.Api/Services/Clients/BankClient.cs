﻿using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Services.Interfaces;

using Polly;

using System.Text.Json;

namespace PaymentGateway.Api.Services.Clients
{
    public class BankClient : IBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BankClient> _logger;

        public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/payments", request);

                // Handle different response types
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
                    return content;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var error = await response.Content.ReadFromJsonAsync<BankErrorResponse>();
                    _logger.LogWarning("Bank returned bad request: {ErrorMessage}", error?.ErrorMessage);
                    throw new BankClientException($"Bank validation failed: {error?.ErrorMessage}");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogWarning("Bank service unavailable");
                    throw new BankClientException("Bank service is currently unavailable");
                }

                // Generic error for other status codes
                _logger.LogError("Unexpected response from bank: {StatusCode}", response.StatusCode);
                throw new BankClientException($"Unexpected response from bank: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error communicating with bank");
                throw new BankClientException("Network error communicating with bank", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout communicating with bank");
                throw new BankClientException("Request to bank timed out", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing bank response");
                throw new BankClientException("Error parsing bank response", ex);
            }
            catch (BankClientException)
            {
                // Just rethrow - we've already logged these
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during bank communication");
                throw new BankClientException("Unexpected error during bank communication", ex);
            }
        }
    }
}
