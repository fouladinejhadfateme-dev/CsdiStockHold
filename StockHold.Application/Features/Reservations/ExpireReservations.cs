
using StockHold.Application.Abstractions.Persistence;
using StockHold.Application.Abstractions.Persistence.Reservations;

namespace StockHold.Application.Features.Reservations.ExpireReservations;

public sealed class ExpireReservationsService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpireReservationsService(
        IReservationRepository reservationRepository,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExpireAsync()
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var utcNow = DateTime.UtcNow;

            var expiredReservations =
                await _reservationRepository
                    .GetExpiredReservationsAsync(utcNow);

            if (expiredReservations.Count == 0)
            {
                await _unitOfWork.CommitTransactionAsync();
                return 0;
            }

            foreach (var reservation in expiredReservations)
            {
                reservation.MarkAsExpired();
            }

            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            return expiredReservations.Count;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
