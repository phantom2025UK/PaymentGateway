using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest request);
        GetPaymentResponse GetPayment(Guid id);
        PostPaymentResponse CreateRejectedPayment(PostPaymentRequest request, List<string> validationErrors);
    }
}
