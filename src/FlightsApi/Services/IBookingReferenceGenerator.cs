namespace TonysTunit.Flights.Services;

/// <summary>
/// Produces a short booking reference such as AF-7K2M. Tests replace this
/// with a predictable fake so assertions can use a known value.
/// </summary>
public interface IBookingReferenceGenerator
{
    string Next();
}
