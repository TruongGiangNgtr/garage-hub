namespace GarageHub.Repository.Repositories;

public enum PaymentConfirmResult
{
    /// <summary>Payment was already Confirmed (or does not exist); nothing changed.</summary>
    AlreadyProcessed,
    /// <summary>Payment and booking both confirmed.</summary>
    Confirmed,
    /// <summary>Payment confirmed but the booking was no longer Pending (canceled/expired); needs manual handling.</summary>
    ConfirmedBookingNotPending
}
