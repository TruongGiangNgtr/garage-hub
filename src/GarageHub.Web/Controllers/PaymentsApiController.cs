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

    public PaymentsApiController(IPaymentService paymentService, IPayOSClient payOSClient)
    {
        _paymentService = paymentService;
        _payOSClient = payOSClient;
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
}
