using System.Net;
using System.Net.Sockets;
using DDFC.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DDFC.Tests;

public class SmtpEmailServiceTests
{
    [Fact]
    public async Task MissingHostFailsInsteadOfReportingSuccess()
    {
        var sender = new SmtpEmailService(Configuration(new()));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("recipient@example.com", "Subject", "Body"));
        Assert.Contains("Smtp:Host", error.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("invalid")]
    public async Task InvalidPortIsRejected(string port)
    {
        var sender = new SmtpEmailService(Configuration(new() { ["Smtp:Host"] = "localhost", ["Smtp:Port"] = port }));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("recipient@example.com", "Subject", "Body"));
        Assert.Contains("Smtp:Port", error.Message);
    }

    [Fact]
    public async Task IncompleteCredentialsAreRejected()
    {
        var sender = new SmtpEmailService(Configuration(new() { ["Smtp:Host"] = "localhost", ["Smtp:From"] = "sender@example.com", ["Smtp:UserName"] = "user" }));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("recipient@example.com", "Subject", "Body"));
        Assert.Contains("Smtp:Password", error.Message);
    }

    [Fact]
    public async Task MissingSenderIsRejected()
    {
        var sender = new SmtpEmailService(Configuration(new() { ["Smtp:Host"] = "localhost" }));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("recipient@example.com", "Subject", "Body"));
        Assert.Contains("Smtp:From", error.Message);
    }

    [Fact]
    public async Task InvalidTlsSettingFailsWhenSending()
    {
        var sender = new SmtpEmailService(Configuration(new() { ["Smtp:Host"] = "localhost", ["Smtp:EnableSsl"] = "invalid" }));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("recipient@example.com", "Subject", "Body"));
        Assert.Contains("Smtp:EnableSsl", error.Message);
    }

    [Fact]
    public async Task SendsHtmlThroughConfiguredSmtpServer()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var received = ReceiveMessageAsync(listener, timeout.Token);
        var sender = new SmtpEmailService(Configuration(new()
        {
            ["Smtp:Host"] = "127.0.0.1", ["Smtp:Port"] = port.ToString(), ["Smtp:EnableSsl"] = "false",
            ["Smtp:From"] = "sender@example.com", ["Smtp:FromName"] = "DDFC"
        }));

        await sender.SendAsync("recipient@example.com", "Soil test", "<p>Scheduled</p>");
        var message = await received;
        Assert.Contains("sender@example.com", message);
        Assert.Contains("DDFC", message);
        Assert.Contains("recipient@example.com", message);
        Assert.Contains("Soil test", message);
        Assert.Contains("<p>Scheduled</p>", message);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static async Task<string> ReceiveMessageAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
        using var reader = new StreamReader(connection.GetStream());
        using var writer = new StreamWriter(connection.GetStream()) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteLineAsync("220 localhost ready");
        var message = new System.Text.StringBuilder();
        var receivingData = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (receivingData)
            {
                if (line == ".")
                {
                    receivingData = false;
                    await writer.WriteLineAsync("250 accepted");
                }
                else message.AppendLine(line);
            }
            else if (line.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO ", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 localhost");
            else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 accepted");
            else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                receivingData = true;
                await writer.WriteLineAsync("354 send message");
            }
            else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 goodbye");
                break;
            }
            else await writer.WriteLineAsync("250 accepted");
        }
        return message.ToString();
    }
}