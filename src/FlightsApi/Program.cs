using Microsoft.EntityFrameworkCore;
using TonysTunit.Flights.Data;
using TonysTunit.Flights.Models;
using TonysTunit.Flights.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBookingReferenceGenerator, BookingReferenceGenerator>();
builder.Services.AddDbContext<FlightsDbContext>(options =>
{
    var name = builder.Configuration["Testing:DbName"] ?? "Flights";
    options.UseInMemoryDatabase(name);
});
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlightsDbContext>();
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    Seed.Ensure(db, clock);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/flights", async (BookingService bookings, CancellationToken cancellationToken) =>
    Results.Ok(await bookings.ListFlightsAsync(cancellationToken)));

app.MapGet("/flights/{id:guid}", async (Guid id, BookingService bookings, CancellationToken cancellationToken) =>
{
    var flight = await bookings.GetFlightAsync(id, cancellationToken);
    return flight is null
        ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Flight not found",
            detail: $"No flight exists with id '{id}'.")
        : Results.Ok(flight);
});

app.MapPost("/flights", async (CreateFlightRequest request, BookingService bookings, CancellationToken cancellationToken) =>
{
    var flight = await bookings.ScheduleAsync(request, cancellationToken);
    return Results.Created($"/flights/{flight.Id}", flight);
});

app.MapPost("/bookings", async (CreateBookingRequest request, BookingService bookings, CancellationToken cancellationToken) =>
{
    var result = await bookings.BookAsync(request, cancellationToken);
    return result switch
    {
        BookingResult.Success success => Results.Created($"/bookings/{success.Booking.Id}", success.Booking),
        BookingResult.FlightNotFound => Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Flight not found",
            detail: $"No flight exists with id '{request.FlightId}'."),
        BookingResult.NotEnoughSeats seats => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Not enough seats",
            detail: $"Only {seats.Available} seat(s) remain on this flight."),
        BookingResult.DepartureInThePast => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Flight has already departed",
            detail: "Bookings cannot be made for a flight that has already departed."),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Unexpected booking result")
    };
});

app.MapGet("/bookings/{id:guid}", async (Guid id, BookingService bookings, CancellationToken cancellationToken) =>
{
    var booking = await bookings.GetBookingAsync(id, cancellationToken);
    return booking is null
        ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Booking not found",
            detail: $"No booking exists with id '{id}'.")
        : Results.Ok(booking);
});

app.MapDelete("/bookings/{id:guid}", async (Guid id, BookingService bookings, CancellationToken cancellationToken) =>
{
    var cancelled = await bookings.CancelAsync(id, cancellationToken);
    return cancelled
        ? Results.NoContent()
        : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Booking not found",
            detail: $"No booking exists with id '{id}'.");
});

app.Run();

internal static class Seed
{
    public static void Ensure(FlightsDbContext db, TimeProvider clock)
    {
        if (db.Flights.Any())
        {
            return;
        }

        var now = clock.GetUtcNow();
        db.Flights.AddRange(
            new Flight
            {
                Id = Guid.Parse("11111111-1111-7111-8111-111111111111"),
                FlightNumber = "AF101",
                Origin = "LHR",
                Destination = "JFK",
                Departure = now.AddDays(7),
                SeatsAvailable = 200
            },
            new Flight
            {
                Id = Guid.Parse("22222222-2222-7222-8222-222222222222"),
                FlightNumber = "AF202",
                Origin = "LGW",
                Destination = "AMS",
                Departure = now.AddDays(2),
                SeatsAvailable = 150
            },
            new Flight
            {
                Id = Guid.Parse("33333333-3333-7333-8333-333333333333"),
                FlightNumber = "AF303",
                Origin = "MAN",
                Destination = "DUB",
                Departure = now.AddHours(6),
                SeatsAvailable = 80
            });
        db.SaveChanges();
    }
}

public partial class Program;
