using GarageHub.Service.Payments;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

// PayOS redirects the customer here (returnUrl / cancelUrl). Anonymous on purpose: see HandleCancelAsync.
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Return(long orderCode, CancellationToken cancellationToken)
        => View(await _paymentService.HandleReturnAsync(orderCode, cancellationToken));

    public async Task<IActionResult> Cancel(long orderCode, CancellationToken cancellationToken)
        => View(await _paymentService.HandleCancelAsync(orderCode, cancellationToken));
}
