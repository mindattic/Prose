using System.Net;
using System.Net.Mail;
using Serilog;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Sends email via SMTP. Configure via Settings page (SMTP section) or environment variables.
/// </summary>
public class EmailService
{
    private readonly SettingsService settings;

    public EmailService(SettingsService settings)
    {
        this.settings = settings;
    }

    /// <summary>Returns true if SMTP host and from address are configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
        !string.IsNullOrWhiteSpace(settings.SmtpFrom);

    /// <summary>Sends a plain-text email. Throws InvalidOperationException if SMTP is not configured.</summary>
    public async Task SendAsync(string to, string subject, string body)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("SMTP is not configured. Ask an administrator to configure it in Settings.");

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.SmtpEnableSsl,
            Credentials = string.IsNullOrEmpty(settings.SmtpUsername)
                ? null
                : new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(settings.SmtpFrom),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message);
            Log.Information("Email sent to {To} — subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
