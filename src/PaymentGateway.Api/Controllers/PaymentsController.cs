using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
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
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync([FromBody] PostPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            // Create a list of validation errors from ModelState
            var validationErrors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                .ToList();

            // Get the service to create a rejected payment
            var rejectedResponse = _paymentService.CreateRejectedPayment(request, validationErrors);

            // Return with 422 Unprocessable Entity status
            return UnprocessableEntity(rejectedResponse);
        }

        var response = await _paymentService.ProcessPaymentAsync(request);

        if (response.ValidationErrors.Any())
        {
            _logger.LogWarning("Payment rejected: {Errors}", string.Join(", ", response.ValidationErrors));
            return UnprocessableEntity(response);
        }
        
        return CreatedAtRoute("GetPayment", new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}", Name = "GetPayment")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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