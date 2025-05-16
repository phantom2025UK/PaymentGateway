using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Services.Interfaces;
using PaymentGateway.Api.Models.Configuration;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Api.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IBankClient _bankClient;
        private readonly PaymentsRepository _paymentsRepository;
        private readonly IPaymentValidationService _validationService;
        private readonly ILogger<PaymentService> _logger;

        private readonly PaymentGatewayConfig _config;

        public PaymentService(IBankClient bankClient, PaymentsRepository paymentsRepository,
                              IPaymentValidationService validationService,
                              ILogger<PaymentService> logger, 
                              IOptions<PaymentGatewayConfig> config)
        {
            _bankClient = bankClient;
            _paymentsRepository = paymentsRepository;
            _logger = logger;
            _validationService = validationService;
            _config = config.Value;
        }

        public async Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest request)
        {
            // Validate the request
            var validationErrors = _validationService.ValidatePaymentRequest(request);
            if (validationErrors.Any())
            {
                _logger.LogWarning("Payment rejected: {Errors}", string.Join(", ", validationErrors));
                
                var rejectedResponse = CreateRejectedPayment(request, validationErrors);

                return rejectedResponse;
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

                return paymentResponse;
            }
            catch (BankClientException ex)
            {
                _logger.LogWarning(ex, "Bank client error during payment processing");
                
                var rejectedResponse = CreateRejectedPayment(request, new List<string> { ex.Message });
                return rejectedResponse;
            }
            catch (Exception ex)
            {
                var err = "Unexpected error during payment processing";
                _logger.LogError(ex, err);
                
                var rejectedResponse = CreateRejectedPayment(request, new List<string> { err });
                return rejectedResponse;
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
                Amount = payment.Amount,
                ValidationErrors = payment.ValidationErrors
            };
        }

        public PostPaymentResponse CreateRejectedPayment(PostPaymentRequest request, List<string> validationErrors)
        {
            var rejectedResponse = new PostPaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Rejected,
                CardNumberLastFour = request.CardNumber?.Length >= 4
                    ? int.Parse(request.CardNumber.Substring(request.CardNumber.Length - 4))
                    : 0,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
                ValidationErrors = validationErrors
            };

            // Store the rejected payment
            _paymentsRepository.Add(rejectedResponse);

            return rejectedResponse;
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
