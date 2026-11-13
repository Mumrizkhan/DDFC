using DDFC.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace DDFC.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int    _port;
    private readonly bool?  _enableSsl;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _from;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration cfg)
    {
        var s      = cfg.GetSection("Smtp");
        _host      = s["Host"]      ?? "";
        _port      = int.TryParse(s["Port"] ?? "587", out var p) ? p : 0;
        _enableSsl = bool.TryParse(s["EnableSsl"] ?? "true", out var ssl) ? ssl : null;
        _userName  = s["UserName"]  ?? "";
        _password  = s["Password"]  ?? "";
        _from      = s["From"]      ?? "";
        _fromName  = s["FromName"]  ?? "";
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new InvalidOperationException("Smtp:Host must be configured before sending email.");
        if (_port is < 1 or > 65535)
            throw new InvalidOperationException("Smtp:Port must be between 1 and 65535.");
        if (!_enableSsl.HasValue)
            throw new InvalidOperationException("Smtp:EnableSsl must be true or false.");
        if (string.IsNullOrWhiteSpace(_from))
            throw new InvalidOperationException("Smtp:From must be configured before sending email.");
        if (string.IsNullOrWhiteSpace(_userName) != string.IsNullOrWhiteSpace(_password))
            throw new InvalidOperationException("Smtp:UserName and Smtp:Password must both be configured for authentication.");

        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl.Value,
            UseDefaultCredentials = false,
        };
        if (!string.IsNullOrWhiteSpace(_userName))
            client.Credentials = new NetworkCredential(_userName, _password);
        using var msg = new MailMessage
        {
            From = new MailAddress(_from, _fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        msg.To.Add(to);
        await client.SendMailAsync(msg);
    }
}
