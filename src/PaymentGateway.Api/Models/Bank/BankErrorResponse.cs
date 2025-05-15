using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Bank
{
    public class BankErrorResponse
    {
        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }
    }
}
