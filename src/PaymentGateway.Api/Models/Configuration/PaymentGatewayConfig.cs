namespace PaymentGateway.Api.Models.Configuration
{
    public class PaymentGatewayConfig
    {
        public BankClientConfig BankClient { get; set; } = new BankClientConfig();
        public ValidationConfig Validation { get; set; } = new ValidationConfig();
        public ResilienceConfig Resilience { get; set; } = new ResilienceConfig();
    }

    public class BankClientConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:8080";
        public int TimeoutSeconds { get; set; } = 15;
    }

    public class ValidationConfig
    {
        public string[] SupportedCurrencies { get; set; } = new[] { "USD", "GBP", "EUR" };
        public CardNumberConfig CardNumber { get; set; } = new CardNumberConfig();
    }

    public class CardNumberConfig
    {
        public int MinLength { get; set; } = 14;
        public int MaxLength { get; set; } = 19;
        public int CVVMinLength { get; set; } = 3;
        public int CVVMaxLength { get; set; } = 4;
    }

    public class ResilienceConfig
    {
        public int MaxRetries { get; set; } = 2;
        public int InitialBackoffSeconds { get; set; } = 1;
    }
}
