using System.Security.Cryptography;

namespace TonysTunit.Flights.Services;

public sealed class BookingReferenceGenerator : IBookingReferenceGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Next()
    {
        Span<char> chars = stackalloc char[4];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"AF-{new string(chars)}";
    }
}
