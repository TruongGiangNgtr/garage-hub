using GarageHub.Service.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsApiController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPayOSClient _payOSClient;
    private readonly IWebHostEnvironment _environment;

    public PaymentsApiController(IPaymentService paymentService, IPayOSClient payOSClient, IWebHostEnvironment environment)
    {
        _paymentService = paymentService;
        _payOSClient = payOSClient;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost("payos-callback")]
    public async Task<IActionResult> PayOSCallback(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var webhook = await _payOSClient.VerifyWebhookAsync(body, cancellationToken);
        await _paymentService.ConfirmPaymentAsync(webhook.OrderCode, webhook.Reference, cancellationToken);
        return Ok();
    }

    // TEMPORARY test endpoint (Development only). The Booking flow will call IPaymentService directly later.
    [HttpPost("{paymentId:int}/create-link")]
    public async Task<IActionResult> CreateLink(int paymentId, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment()) return NotFound();

        var checkoutUrl = await _paymentService.CreatePaymentLinkAsync(paymentId, cancellationToken);
        return Ok(new { checkoutUrl });
    }
}
