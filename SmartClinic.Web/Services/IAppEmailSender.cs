namespace SmartClinic.Web.Services;

public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}

/// <summary>Dev stub: logs the email. Replace with a MailKit/SMTP implementation in Sprint 3 (#13).</summary>
public class ConsoleEmailSender : IAppEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _log;
    public ConsoleEmailSender(ILogger<ConsoleEmailSender> log) => _log = log;

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _log.LogInformation("EMAIL to {To} | {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
