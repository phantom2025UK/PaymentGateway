using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Moq;

using PaymentGateway.Api.Models.Bank;
using Moq.Protected;

using PaymentGateway.Api.Services.Clients;
using System.Text.Json;
using PaymentGateway.Api.Exceptions;

namespace PaymentGateway.Api.Tests.Services.Clients
{
    public class BankClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<BankClient>> _mockLogger;

        public BankClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<BankClient>>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithSuccessfulResponse_ReturnsAuthorizedPayment()
        {
            // Arrange
            var request = new BankPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryDate = "12/2025",
                Currency = "USD",
                Amount = 1000,
                CVV = "123"
            };

            var expectedResponse = new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = "test-auth-code"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://test-bank.com")
            };

            var bankClient = new BankClient(httpClient, _mockLogger.Object);

            // Act
            var result = await bankClient.ProcessPaymentAsync(request);

            // Assert
            Assert.True(result.Authorized);
            Assert.Equal("test-auth-code", result.AuthorizationCode);

            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().EndsWith("/payments")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithServiceUnavailableResponse_ThrowsBankClientException()
        {
            // Arrange
            var request = new BankPaymentRequest
            {
                CardNumber = "4111111111111110", // Card ending in 0 (causes service unavailable)
                ExpiryDate = "12/2025",
                Currency = "USD",
                Amount = 1000,
                CVV = "123"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://test-bank.com")
            };

            var bankClient = new BankClient(httpClient, _mockLogger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BankClientException>(() =>
                bankClient.ProcessPaymentAsync(request));

            Assert.Contains("Bank service is currently unavailable", exception.Message);
        }


    }
}
