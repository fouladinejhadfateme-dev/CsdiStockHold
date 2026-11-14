
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockHold.Application.Features.Reservations.ExpireReservations;

namespace StockHold.API.BackgroundServices;

public sealed class ReservationExpirationBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpirationBackgroundService> _logger;

    public ReservationExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Reservation Expiration BackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var expirationService = scope.ServiceProvider
                    .GetRequiredService<ExpireReservationsService>();

                var count = await expirationService.ExpireAsync();

                _logger.LogInformation(
                    "Reservation expiration check completed. Expired count: {Count}",
                    count);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while expiring reservations.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Reservation Expiration BackgroundService stopped.");
    }
}
