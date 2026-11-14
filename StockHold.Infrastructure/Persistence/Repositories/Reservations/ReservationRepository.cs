
using Microsoft.EntityFrameworkCore;
using StockHold.Application.Abstractions.Persistence.Reservations;
using StockHold.Domain.Entities;
using StockHold.Infrastructure.Persistence.Contexts;

namespace StockHold.Infrastructure.Persistence.Repositories.Reservations;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly StockHoldDbContext _context;

    public ReservationRepository(StockHoldDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(int reservationId)
    {
        return await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId);
    }

    public async Task<Reservation?> GetByIdempotencyKeyAsync(
        int customerId,
        string idempotencyKey)
    {
        return await _context.Reservations
            .FromSqlInterpolated($@"
                SELECT *
                FROM [dbo].[Reservations]
                WITH (
                    UPDLOCK,
                    HOLDLOCK,
                    INDEX([UX_Reservations_Customer_IdempotencyKey])
                )
                WHERE [CustomerId] = {customerId}
                AND [IdempotencyKey] = {idempotencyKey}")
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Reservation reservation)
    {
        await _context.Reservations.AddAsync(reservation);
    }

    public async Task<List<Reservation>>
        GetActiveAndUnexpiredReservationsByProductIdAsync(
            int productId,
            DateTime utcNow)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Where(r =>
                r.ProductId == productId &&
                r.Status == StockHold.Domain.Enums.ReservationStatus.Active &&
                r.ExpiresAt > utcNow)
            .ToListAsync();
    }

    public async Task<List<Reservation>> GetExpiredReservationsAsync(
        DateTime utcNow)
    {
        return await _context.Reservations
            .Where(r =>
                r.Status == StockHold.Domain.Enums.ReservationStatus.Active &&
                r.ExpiresAt <= utcNow)
            .ToListAsync();
    }
}
