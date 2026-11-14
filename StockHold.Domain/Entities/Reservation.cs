using StockHold.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Domain.Entities
{
    public class Reservation
    {
        public int Id { get; private set; }
        public int CustomerId { get; private set; }
        public int ProductId { get; private set; }
        public int Quantity { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public ReservationStatus Status { get; private set; }
        public string IdempotencyKey { get; private set; }

        private Reservation() { }
        public Reservation(int customerId, int productId, int quantity, TimeSpan validity, string idempotencyKey)
        {
            if (customerId <= 0)
                throw new ArgumentException("شناسه مشتری معتبر نیست.");

            if (productId <= 0)
                throw new ArgumentException("شناسه کالا معتبر نیست.");

            if (quantity <= 0)
                throw new ArgumentException("تعداد رزرو باید بیشتر از صفر باشد.");

            if (validity <= TimeSpan.Zero)
                throw new ArgumentException("مدت اعتبار رزرو معتبر نیست.");

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("کلید درخواست الزامی است.");

            CustomerId = customerId;
            ProductId = productId;
            Quantity = quantity;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = CreatedAt.Add(validity);
            Status = ReservationStatus.Active;
            IdempotencyKey = idempotencyKey;

        }
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }
        public void MarkAsExpired()
        {
            if (Status != ReservationStatus.Active)
                return;

            if (!IsExpired())
                throw new InvalidOperationException(
                    "هنوز زمان اعتبار رزرو تمام نشده است.");

            Status = ReservationStatus.Expired;
        }

        public void Cancel()
        {
            if (Status != ReservationStatus.Active)
                throw new InvalidOperationException(
                    "فقط رزرو فعال قابل لغو است.");

            Status = ReservationStatus.Cancelled;
        }
    }
}
