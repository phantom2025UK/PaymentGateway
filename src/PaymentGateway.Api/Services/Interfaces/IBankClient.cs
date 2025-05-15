using PaymentGateway.Api.Models.Bank;

namespace PaymentGateway.Api.Services.Interfaces
{
    public interface IBankClient
    {
        Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request);
    }
}
