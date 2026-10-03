using DDFC.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace DDFC.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int    _port;
    private readonly bool   _enableSsl;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _from;

    public SmtpEmailService(IConfiguration cfg)
    {
        var s      = cfg.GetSection("Smtp");
        _host      = s["Host"]      ?? "";
        _port      = int.TryParse(s["Port"], out var p) ? p : 587;
        _enableSsl = bool.TryParse(s["EnableSsl"], out var ssl) ? ssl : true;
        _userName  = s["UserName"]  ?? "";
        _password  = s["Password"]  ?? "";
        _from      = s["From"]      ?? "noreply@ddfc.com";
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_host))
        {
            Console.WriteLine($"[EMAIL-STUB] To={to} | Subject={subject}");
            return;
        }

        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl,
            Credentials = !string.IsNullOrWhiteSpace(_userName)
                ? new NetworkCredential(_userName, _password)
                : CredentialCache.DefaultNetworkCredentials,
        };
        using var msg = new MailMessage(_from, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(msg);
    }
}
