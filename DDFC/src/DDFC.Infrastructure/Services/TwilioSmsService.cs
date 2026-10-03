using DDFC.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;

namespace DDFC.Infrastructure.Services;

public class TwilioSmsService : ISmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly IHttpClientFactory _http;

    public TwilioSmsService(IConfiguration cfg, IHttpClientFactory http)
    {
        _accountSid = cfg["Twilio:AccountSid"] ?? "";
        _authToken  = cfg["Twilio:AuthToken"]  ?? "";
        _fromNumber = cfg["Twilio:FromNumber"] ?? "";
        _http       = http;
    }

    public async Task SendAsync(string to, string message)
    {
        if (string.IsNullOrWhiteSpace(_accountSid))
        {
            Console.WriteLine($"[SMS-STUB] To={to} | {message}");
            return;
        }

        var client  = _http.CreateClient();
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["From"] = _fromNumber,
            ["To"]   = to,
            ["Body"] = message,
        });

        var url  = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";
        var resp = await client.PostAsync(url, form);
        if (!resp.IsSuccessStatusCode)
            Console.WriteLine($"[SMS-ERROR] Twilio returned {(int)resp.StatusCode}");
    }
}
