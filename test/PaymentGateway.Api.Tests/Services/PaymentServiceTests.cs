using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PaymentGateway.Api.Models.Configuration;
using PaymentGateway.Api.Services.Interfaces;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IBankClient> _mockBankClient;
        private readonly PaymentsRepository _repository; // Real repository, not mocked
        private readonly Mock<IPaymentValidationService> _mockValidationService;
        private readonly Mock<ILogger<PaymentService>> _mockLogger;
        private readonly Mock<IOptions<PaymentGatewayConfig>> _mockOptions;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _mockBankClient = new Mock<IBankClient>();
            _repository = new PaymentsRepository();
            _mockValidationService = new Mock<IPaymentValidationService>();
            _mockLogger = new Mock<ILogger<PaymentService>>();
            _mockOptions = new Mock<IOptions<PaymentGatewayConfig>>();

            _mockOptions.Setup(x => x.Value).Returns(new PaymentGatewayConfig
            {
                Validation = new ValidationConfig
                {
                    SupportedCurrencies = new[] { "USD", "GBP", "EUR" },
                    CardNumber = new CardNumberConfig
                    {
                        MinLength = 14,
                        MaxLength = 19,
                        CVVMinLength = 3,
                        CVVMaxLength = 4
                    }
                }
            });

            _paymentService = new PaymentService(
                _mockBankClient.Object,
                _repository,
                _mockValidationService.Object,
                _mockLogger.Object,
                _mockOptions.Object
            );
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithValidRequest_ReturnsAuthorizedPayment()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "4111111111111111", // Card ending in odd digit (1)
                ExpiryMonth = 12,
                ExpiryYear = DateTime.Now.Year + 1,
                Currency = "USD",
                Amount = 1000,
                CVV = "123"
            };

            var bankResponse = new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = "test-auth-code"
            };

            _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ReturnsAsync(bankResponse);

            _mockValidationService.Setup(x => x.ValidatePaymentRequest(request))
                .Returns(new List<string>()); // No validation errors

            // Act
            var result = await _paymentService.ProcessPaymentAsync(request);

            // Assert
            Assert.Equal(PaymentStatus.Authorized, result.Status);
            Assert.Equal(1111, result.CardNumberLastFour); // Last 4 digits of the card
            Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(request.ExpiryYear, result.ExpiryYear);
            Assert.Equal(request.Currency, result.Currency);
            Assert.Equal(request.Amount, result.Amount);
            Assert.Empty(result.ValidationErrors);

            // Verify the payment was stored
            var storedPayment = _repository.Get(result.Id);
            Assert.NotNull(storedPayment);
            Assert.Equal(PaymentStatus.Authorized, storedPayment.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithValidRequestButDeclinedByBank_ReturnsDeclinedPayment()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "4111111111111112", // Card ending in even digit (2)
                ExpiryMonth = 12,
                ExpiryYear = DateTime.Now.Year + 1,
                Currency = "USD",
                Amount = 1000,
                CVV = "123"
            };

            var bankResponse = new BankPaymentResponse
            {
                Authorized = false,
                AuthorizationCode = null
            };

            _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ReturnsAsync(bankResponse);

            _mockValidationService.Setup(x => x.ValidatePaymentRequest(request))
                .Returns(new List<string>()); // No validation errors

            // Act
            var result = await _paymentService.ProcessPaymentAsync(request);

            // Assert
            Assert.Equal(PaymentStatus.Declined, result.Status);
            Assert.Equal(1112, result.CardNumberLastFour);
            Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(request.ExpiryYear, result.ExpiryYear);
            Assert.Equal(request.Currency, result.Currency);
            Assert.Equal(request.Amount, result.Amount);
            Assert.Empty(result.ValidationErrors);

            // Verify the payment was stored
            var storedPayment = _repository.Get(result.Id);
            Assert.NotNull(storedPayment);
            Assert.Equal(PaymentStatus.Declined, storedPayment.Status);
        }

        [Fact]
        public void GetPayment_WithExistingId_ReturnsPayment()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var storedPayment = new PostPaymentResponse
            {
                Id = paymentId,
                Status = PaymentStatus.Authorized,
                CardNumberLastFour = 1111,
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 1000
            };

            // Add the payment to the real repository
            _repository.Add(storedPayment);

            // Act
            var result = _paymentService.GetPayment(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.Id);
            Assert.Equal(PaymentStatus.Authorized, result.Status);
            Assert.Equal(1111, result.CardNumberLastFour);
            Assert.Equal(12, result.ExpiryMonth);
            Assert.Equal(2025, result.ExpiryYear);
            Assert.Equal("USD", result.Currency);
            Assert.Equal(1000, result.Amount);
        }
    }
}
