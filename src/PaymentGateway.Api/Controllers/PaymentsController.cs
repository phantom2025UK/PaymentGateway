using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync([FromBody] PostPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (response, errors) = await _paymentService.ProcessPaymentAsync(request);

        if (errors.Any())
        {
            _logger.LogWarning("Payment rejected: {Errors}", string.Join(", ", errors));
            return BadRequest(new { Errors = errors });
        }
        
        return CreatedAtRoute("GetPayment", new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}", Name = "GetPayment")]
    public ActionResult<GetPaymentResponse> GetPaymentAsync(Guid id)
    {
        var payment = _paymentService.GetPayment(id);

        if (payment == null)
        {
            _logger.LogWarning("Payment with ID {PaymentId} not found", id);
            return NotFound();
        }

        return Ok(payment);
    }
}