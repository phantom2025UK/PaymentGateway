using Microsoft.Extensions.Options;

using PaymentGateway.Api.Models.Configuration;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Services
{
    public class PaymentValidationService : IPaymentValidationService
    {
        private readonly ValidationConfig _validationConfig;

        public PaymentValidationService(IOptions<PaymentGatewayConfig> config)
        {
            _validationConfig = config.Value.Validation;
        }

        public List<string> ValidatePaymentRequest(PostPaymentRequest request)
        {
            var errors = new List<string>();

            // Card number validation
            if (string.IsNullOrWhiteSpace(request.CardNumber) || !request.CardNumber.All(char.IsDigit))
            {
                errors.Add("Card number must contain only digits");
            }
            else if (request.CardNumber.Length < _validationConfig.CardNumber.MinLength ||
                     request.CardNumber.Length > _validationConfig.CardNumber.MaxLength)
            {
                errors.Add($"Card number must be between {_validationConfig.CardNumber.MinLength} and {_validationConfig.CardNumber.MaxLength} digits");
            }


            // Currency validation
            if (string.IsNullOrWhiteSpace(request.Currency))
            {
                errors.Add("Currency is required");
            }
            else if (request.Currency.Length != 3)
            {
                errors.Add("Currency must be exactly 3 characters");
            }
            else if (!_validationConfig.SupportedCurrencies.Contains(request.Currency))
            {
                errors.Add($"Currency must be one of: {string.Join(", ", _validationConfig.SupportedCurrencies)}");
            }

            // Amount validation
            if (request.Amount <= 0)
            {
                errors.Add("Amount must be a positive integer");
            }

            // CVV validation
            if (string.IsNullOrWhiteSpace(request.CVV) || !request.CVV.All(char.IsDigit))
            {
                errors.Add("CVV must contain only digits");
            }
            else if (request.CVV.Length < _validationConfig.CardNumber.CVVMinLength ||
                     request.CVV.Length > _validationConfig.CardNumber.CVVMaxLength)
            {
                errors.Add($"CVV must be between {_validationConfig.CardNumber.CVVMinLength} and {_validationConfig.CardNumber.CVVMaxLength} digits");
            }


            // Expiry month validation
            if (request.ExpiryMonth < 1 || request.ExpiryMonth > 12)
            {
                errors.Add("Expiry month must be between 1 and 12");

                // Don't proceed further as datetime computations wont work.
                return errors;
            }

            var today = DateTime.Today;

            int currentYear = today.Year;
            int maxAllowedYear = currentYear + 100;

            if (request.ExpiryYear > maxAllowedYear)
            {
                errors.Add($"Card expiration date must be within {maxAllowedYear} years");

                // Don't proceed further
                return errors;
            }

            // Expiry date validation
            var expiryDate = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1).AddMonths(1).AddDays(-1);
            if (expiryDate <= today)
            {
                errors.Add("Card expiration date must be in the future");
            }

            return errors;
        }
    }
}