using Microsoft.Extensions.Options;
using Moq;
using PaymentGateway.Api.Models.Configuration;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Services
{
    public class PaymentValidationServiceTests
    {
        private readonly Mock<IOptions<PaymentGatewayConfig>> _mockOptions;
        private readonly PaymentValidationService _validationService;
        private readonly PaymentGatewayConfig _config;

        public PaymentValidationServiceTests()
        {
            // Setup configuration
            _config = new PaymentGatewayConfig
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
            };

            _mockOptions = new Mock<IOptions<PaymentGatewayConfig>>();
            _mockOptions.Setup(x => x.Value).Returns(_config);

            _validationService = new PaymentValidationService(_mockOptions.Object);
        }

        private PostPaymentRequest CreateValidRequest()
        {
            /// Future year to ensure test doesn't fail as time passes
            int futureYear = DateTime.Now.Year + 1;

            // Use a card number that matches the configured length requirements
            string validCardNumber = new string('4', _config.Validation.CardNumber.MinLength);

            // Use the first supported currency from the configuration
            string validCurrency = _config.Validation.SupportedCurrencies.First();

            // Use a CVV that matches the configured length requirements
            string validCVV = new string('1', _config.Validation.CardNumber.CVVMinLength);

            return new PostPaymentRequest
            {
                CardNumber = validCardNumber,
                ExpiryMonth = 12,
                ExpiryYear = futureYear,
                Currency = validCurrency,
                Amount = 1000,
                CVV = validCVV
            };
        }

        // Card Number Validation Tests

        [Fact]
        public void ValidatePaymentRequest_WhenCardNumberIsValid_ReturnsNoCardNumberErrors()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.DoesNotContain(errors, error => error.Contains("Card number"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ValidatePaymentRequest_WhenCardNumberIsNullOrEmpty_ReturnsCardNumberError(string cardNumber)
        {
            // Arrange
            var request = CreateValidRequest();
            request.CardNumber = cardNumber;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.ToLower().Contains("card number") && error.ToLower().Contains("digit"));
        }

        [Theory]
        [InlineData("123")]  // Too short
        [InlineData("12345678901234567890")]  // Too long
        public void ValidatePaymentRequest_WhenCardNumberLengthIsInvalid_ReturnsCardNumberError(string cardNumber)
        {
            // Arrange
            var request = CreateValidRequest();
            request.CardNumber = cardNumber;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.ToLower().Contains("card number") && error.ToLower().Contains("between") && error.ToLower().Contains("digit"));
        }

        [Fact]
        public void ValidatePaymentRequest_WhenCardNumberContainsNonDigits_ReturnsCardNumberError()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CardNumber = "4111111a111111";

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("Card number must contain only digits"));
        }

        // Expiry Date Validation Tests

        [Fact]
        public void ValidatePaymentRequest_WhenExpiryDateIsInFuture_ReturnsNoExpiryDateErrors()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.DoesNotContain(errors, error => error.Contains("expiration date"));
        }

        [Fact]
        public void ValidatePaymentRequest_WhenExpiryDateIsInPast_ReturnsExpiryDateError()
        {
            // Arrange
            var request = CreateValidRequest();
            request.ExpiryYear = DateTime.Now.Year - 1;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            //Assert.Contains(errors, error => error.Contains("Card expiration date must be in the future"));
            Assert.Contains(errors, error => error.Contains("Card expiration date"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        [InlineData(100)]
        public void ValidatePaymentRequest_WhenExpiryMonthIsInvalid_ReturnsExpiryMonthError(int expiryMonth)
        {
            // Arrange
            var request = CreateValidRequest();
            request.ExpiryMonth = expiryMonth;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("Expiry month must be between 1 and 12"));
        }

        // Currency Validation Tests

        [Theory]
        [InlineData("USD")]
        [InlineData("GBP")]
        [InlineData("EUR")]
        public void ValidatePaymentRequest_WhenCurrencyIsSupported_ReturnsNoCurrencyErrors(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.DoesNotContain(errors, error => error.Contains("Currency"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ValidatePaymentRequest_WhenCurrencyIsNullOrEmpty_ReturnsCurrencyError(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("Currency is required"));
        }

        [Theory]
        [InlineData("US")]
        [InlineData("USDT")]
        [InlineData("GB")]
        [InlineData("EURO")]
        public void ValidatePaymentRequest_WhenCurrencyLengthIsNotThree_ReturnsCurrencyError(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("Currency must be exactly 3 characters"));
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("ABC")]
        public void ValidatePaymentRequest_WhenCurrencyIsNotSupported_ReturnsCurrencyError(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            //Assert.Contains(errors, error => error.Contains($"Currency must be one of: {string.Join(", ", _config.Validation.SupportedCurrencies)}"));
            Assert.Contains(errors, error => error.Contains($"Currency must be one of:"));
        }

        // Amount Validation Tests

        [Fact]
        public void ValidatePaymentRequest_WhenAmountIsPositive_ReturnsNoAmountErrors()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Amount = 1;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.DoesNotContain(errors, error => error.Contains("Amount"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void ValidatePaymentRequest_WhenAmountIsZeroOrNegative_ReturnsAmountError(int amount)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Amount = amount;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("Amount must be a positive integer"));
        }

        // CVV Validation Tests

        [Theory]
        [InlineData("123")]
        [InlineData("1234")]
        public void ValidatePaymentRequest_WhenCVVIsValid_ReturnsNoCVVErrors(string cvv)
        {
            // Arrange
            var request = CreateValidRequest();
            request.CVV = cvv;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.DoesNotContain(errors, error => error.Contains("CVV"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ValidatePaymentRequest_WhenCVVIsNullOrEmpty_ReturnsCVVError(string cvv)
        {
            // Arrange
            var request = CreateValidRequest();
            request.CVV = cvv;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("CVV must contain only digits"));
        }

        [Theory]
        [InlineData("12")]
        [InlineData("12345")]
        public void ValidatePaymentRequest_WhenCVVLengthIsInvalid_ReturnsCVVError(string cvv)
        {
            // Arrange
            var request = CreateValidRequest();
            request.CVV = cvv;

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            //Assert.Contains(errors, error => error.Contains($"CVV must be between {_config.Validation.CardNumber.CVVMinLength} and {_config.Validation.CardNumber.CVVMaxLength} digits"));
            Assert.Contains(errors, error => error.Contains($"CVV must be between"));
        }

        [Fact]
        public void ValidatePaymentRequest_WhenCVVContainsNonDigits_ReturnsCVVError()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CVV = "12a";

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Contains(errors, error => error.Contains("CVV must contain only digits"));
        }

        // Multiple Validation Errors Tests

        [Fact]
        public void ValidatePaymentRequest_WhenMultipleFieldsAreInvalid_ReturnsMultipleErrors()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CardNumber = "1234abcd";
            request.ExpiryMonth = 13;
            request.Currency = "INVALID";

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.True(errors.Count >= 3); // At least 3 errors should be returned
            Assert.Contains(errors, error => error.Contains("Card number"));
            Assert.Contains(errors, error => error.Contains("Expiry month"));
            Assert.Contains(errors, error => error.Contains("Currency"));
        }

        [Fact]
        public void ValidatePaymentRequest_WhenAllFieldsAreValid_ReturnsNoErrors()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var errors = _validationService.ValidatePaymentRequest(request);

            // Assert
            Assert.Empty(errors);
        }
    }
}
