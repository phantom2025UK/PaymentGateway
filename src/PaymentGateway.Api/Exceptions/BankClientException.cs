namespace PaymentGateway.Api.Exceptions
{
    // Custom exception for bank client errors
    public class BankClientException : Exception
    {
        public BankClientException(string message) : base(message)
        {
        }

        public BankClientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
