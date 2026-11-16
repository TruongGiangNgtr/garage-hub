using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IBookingRepository
{
    Task<IReadOnlyList<ServiceBooking>> GetScheduleForCustomerAsync(
        int customerId, CancellationToken cancellationToken = default);
    Task<int> CreatePendingBookingAsync(
        ServiceBooking booking, Payment payment, DateTime now, CancellationToken cancellationToken = default);
}
