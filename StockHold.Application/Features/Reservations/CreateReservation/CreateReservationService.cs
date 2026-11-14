
using StockHold.Application.Abstractions.Persistence;
using StockHold.Application.Abstractions.Persistence.Customers;
using StockHold.Application.Abstractions.Persistence.Products;
using StockHold.Application.Abstractions.Persistence.Reservations;
using StockHold.Domain.Entities;

namespace StockHold.Application.Features.Reservations.CreateReservation;

public sealed class CreateReservationService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReservationService(
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IReservationRepository reservationRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Reservation> CreateAsync(
        CreateReservationRequest request)
    {
        if (request.CustomerId <= 0)
            throw new InvalidOperationException("شناسه مشتری معتبر نیست.");

        if (request.ProductId <= 0)
            throw new InvalidOperationException("شناسه کالا معتبر نیست.");

        if (request.Quantity <= 0)
            throw new InvalidOperationException("تعداد رزرو باید بیشتر از صفر باشد.");

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            request.IdempotencyKey.Length > 100)
        {
            throw new InvalidOperationException("کلید درخواست معتبر نیست.");
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // بررسی کلید درخواست با قفل دیتابیس
            var existingReservation =
                await _reservationRepository.GetByIdempotencyKeyAsync(
                    request.CustomerId,
                    request.IdempotencyKey);

            if (existingReservation is not null)
            {
                await _unitOfWork.CommitTransactionAsync();
                return existingReservation;
            }

            var customer =
                await _customerRepository.GetByIdAsync(request.CustomerId);

            if (customer is null)
                throw new InvalidOperationException("مشتری پیدا نشد.");

            if (request.Quantity > customer.GetMaximumReservationQuantity())
            {
                throw new InvalidOperationException(
                    "تعداد رزرو بیشتر از حد مجاز برای این نوع مشتری است.");
            }

            // قفل محصول برای جلوگیری از رزرو هم‌زمان بیش از موجودی
            var product =
                await _productRepository.GetByIdWithLockAsync(request.ProductId);

            if (product is null)
                throw new InvalidOperationException("کالا پیدا نشد.");

            var utcNow = DateTime.UtcNow;

            var activeReservations =
                await _reservationRepository
                    .GetActiveAndUnexpiredReservationsByProductIdAsync(
                        product.Id,
                        utcNow);

            var reservedQuantity =
                activeReservations.Sum(r => r.Quantity);

            var availableQuantity =
                product.Quantity - reservedQuantity;

            if (availableQuantity < request.Quantity)
            {
                throw new InvalidOperationException(
                    "موجودی قابل رزرو کالا کافی نیست.");
            }

            var reservation = new Reservation(
                customer.Id,
                product.Id,
                request.Quantity,
                customer.GetReservationValidity(),
                request.IdempotencyKey);

            await _reservationRepository.AddAsync(reservation);

            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            return reservation;
        }
        catch
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch
            {
                // خطای Rollback نباید خطای اصلی را پنهان کند.
            }

            throw;
        }
    }
}
