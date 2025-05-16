using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    [Required(ErrorMessage = "Card number is required")]
    public string CardNumber { get; set; }

    [Required(ErrorMessage = "Expiry month is required")]
    public int ExpiryMonth { get; set; }

    [Required(ErrorMessage = "Expiry year is required")]
    public int ExpiryYear { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    public string Currency { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "CVV is required")]
    public string CVV { get; set; }
}