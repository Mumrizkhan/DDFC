using DDFC.Application.Interfaces;

namespace DDFC.Infrastructure.Services;

/// <summary>
/// In-memory OTP service. Configure Twilio settings to enable SMS delivery.
/// </summary>
public class OtpService : IOtpService
{
    private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _store = new();
    private static readonly Random _rng = new();

    private readonly ISmsService _sms;

    public OtpService(ISmsService sms) => _sms = sms;

    public async Task<string> GenerateOtpAsync(string cnic)
    {
        var otp = _rng.Next(100000, 999999).ToString();
        _store[cnic] = (otp, DateTime.UtcNow.AddMinutes(10));
        Console.WriteLine($"[OTP] {cnic} → {otp}");
        await _sms.SendAsync(cnic, $"Your DDFC OTP is: {otp}. Valid for 10 minutes.");
        return otp;
    }

    public Task<bool> ValidateOtpAsync(string cnic, string otp)
    {
        if (_store.TryGetValue(cnic, out var entry))
        {
            if (entry.Expiry > DateTime.UtcNow && entry.Otp == otp)
            {
                _store.Remove(cnic);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}
