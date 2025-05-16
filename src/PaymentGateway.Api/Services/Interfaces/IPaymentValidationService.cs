using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services.Interfaces
{
    public interface IPaymentValidationService
    {
        List<string> ValidatePaymentRequest(PostPaymentRequest request);
    }
}
