namespace DDFC.Application.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string cnic);
    Task<bool> ValidateOtpAsync(string cnic, string otp);
}
