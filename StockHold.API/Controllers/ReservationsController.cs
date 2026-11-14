
using Microsoft.AspNetCore.Mvc;
using StockHold.Application.Abstractions.Persistence.Reservations;
using StockHold.Application.Features.Reservations.CreateReservation;
using StockHold.Application.Features.Reservations.ExpireReservations;
namespace StockHold.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly CreateReservationService _createReservationService;
    private readonly IReservationRepository _reservationRepository;
    private readonly ExpireReservationsService _expireReservationsService;
    public ReservationsController(
        CreateReservationService createReservationService,
        IReservationRepository reservationRepository, ExpireReservationsService expireReservationsService)
    {
        _createReservationService = createReservationService;
        _reservationRepository = reservationRepository;
        _expireReservationsService = expireReservationsService;
    }

    // ثبت رزرو جدید
    [HttpPost]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request)
    {
        var reservation =
            await _createReservationService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetReservationById),
            new { id = reservation.Id },
            reservation);
    }

    // دریافت اطلاعات یک رزرو
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReservationById(int id)
    {
        var reservation =
            await _reservationRepository.GetByIdAsync(id);

        if (reservation is null)
            return NotFound("رزرو موردنظر پیدا نشد.");

        return Ok(reservation);
    }


    [HttpPost("expire")]
    public async Task<IActionResult> ExpireReservations()
    {
        var count = await _expireReservationsService.ExpireAsync();

        return Ok(new
        {
            message = $"{count} reservation(s) expired successfully.",
            expiredCount = count
        });
    }

}
