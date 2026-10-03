namespace DDFC.Application.Interfaces;

public interface ISmsService
{
    Task SendAsync(string to, string message);
}
