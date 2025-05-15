using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IBankClient _bankClient;
        private readonly PaymentsRepository _paymentsRepository;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IBankClient bankClient, PaymentsRepository paymentsRepository, ILogger<PaymentService> logger)
        {
            _bankClient = bankClient;
            _paymentsRepository = paymentsRepository;
            _logger = logger;
        }

        public async Task<(PostPaymentResponse, List<string>)> ProcessPaymentAsync(PostPaymentRequest request)
        {
            // Validate the request
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Any())
            {
                return (null, validationErrors);
            }

            try
            {
                // Map to bank request
                var bankRequest = new BankPaymentRequest
                {
                    CardNumber = request.CardNumber,
                    ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
                    Currency = request.Currency,
                    Amount = request.Amount,
                    CVV = request.CVV
                };

                // Process the payment through the bank
                var bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest);

                // Create and store the payment response
                var paymentResponse = new PostPaymentResponse
                {
                    Id = Guid.NewGuid(),
                    Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
                    CardNumberLastFour = GetLastFourDigits(request.CardNumber),
                    ExpiryMonth = request.ExpiryMonth,
                    ExpiryYear = request.ExpiryYear,
                    Currency = request.Currency,
                    Amount = request.Amount
                };

                // Store the payment
                _paymentsRepository.Add(paymentResponse);

                return (paymentResponse, new List<string>());
            }
            catch (BankClientException ex)
            {
                _logger.LogWarning(ex, "Bank client error during payment processing");
                // Return a rejected payment status with error message
                return (null, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during payment processing");
                return (null, new List<string> { "An unexpected error occurred processing the payment" });
            }
        }

        public GetPaymentResponse GetPayment(Guid id)
        {
            var payment = _paymentsRepository.Get(id);
            if (payment == null)
            {
                return null;
            }

            // Map from storage model to response model
            return new GetPaymentResponse
            {
                Id = payment.Id,
                Status = payment.Status,
                CardNumberLastFour = payment.CardNumberLastFour,
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Currency = payment.Currency,
                Amount = payment.Amount
            };
        }

        private List<string> ValidateRequest(PostPaymentRequest request)
        {
            var errors = new List<string>();

            // Card number validation (addition to annotation validation)
            if (string.IsNullOrWhiteSpace(request.CardNumber) || !request.CardNumber.All(char.IsDigit))
            {
                errors.Add("Card number must contain only digits");
            }
            else if (request.CardNumber.Length < 14 || request.CardNumber.Length > 19)
            {
                errors.Add("Card number must be between 14 and 19 digits");
            }

            // Expiry date validation (check if in the future)
            var today = DateTime.Today;
            var expiryDate = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1).AddMonths(1).AddDays(-1);
            if (expiryDate <= today)
            {
                errors.Add("Card expiration date must be in the future");
            }

            // Currency validation
            var validCurrencies = new[] { "USD", "GBP", "EUR" };
            if (string.IsNullOrWhiteSpace(request.Currency) || !validCurrencies.Contains(request.Currency))
            {
                errors.Add($"Currency must be one of: {string.Join(", ", validCurrencies)}");
            }
            else if (request.Currency.Length != 3)
            {
                errors.Add("Currency must be exactly 3 characters");
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
            else if (request.CVV.Length < 3 || request.CVV.Length > 4)
            {
                errors.Add("CVV must be 3 or 4 digits");
            }

            return errors;
        }

        private int GetLastFourDigits(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
            {
                return 0;
            }

            var lastFour = cardNumber.Substring(cardNumber.Length - 4);
            return int.Parse(lastFour);
        }
    }
}
