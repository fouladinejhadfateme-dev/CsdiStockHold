using StockHold.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Application.Abstractions.Persistence.Reservations
{
    public interface IReservationRepository
    {
        Task<Reservation?> GetByIdAsync(int reservationId);

        Task<Reservation?> GetByIdempotencyKeyAsync(
            int customerId,
            string idempotencyKey);

        Task AddAsync(Reservation reservation);

        Task<List<Reservation>>
            GetActiveAndUnexpiredReservationsByProductIdAsync(
                int productId,
                DateTime utcNow);

        Task<List<Reservation>> GetExpiredReservationsAsync(
       DateTime utcNow);
    }
}
