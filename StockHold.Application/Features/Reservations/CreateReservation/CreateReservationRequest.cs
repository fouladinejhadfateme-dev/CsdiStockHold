using StockHold.Application.Abstractions.Persistence.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Application.Features.Reservations.CreateReservation
{
    public sealed record CreateReservationRequest(int CustomerId,
    int ProductId,
    int Quantity,
    string IdempotencyKey);
    
}
