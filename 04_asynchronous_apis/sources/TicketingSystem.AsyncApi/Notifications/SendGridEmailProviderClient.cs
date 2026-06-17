using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace TicketingSystem.AsyncApi.Notifications;

public sealed class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Ticketing System";
}

public interface IEmailProviderClient
{
    Task<EmailSendResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
}

public sealed class SendGridEmailProviderClient(HttpClient httpClient, IOptions<SendGridOptions> options) : IEmailProviderClient
{
    private readonly SendGridOptions _options = options.Value;

    public async Task<EmailSendResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return new EmailSendResult(false, "SendGrid API key is not configured.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            return new EmailSendResult(false, "SendGrid from email is not configured.");

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = request.ToEmail, name = request.ToName } },
                    subject = request.Subject
                }
            },
            from = new { email = _options.FromEmail, name = _options.FromName },
            content = new[]
            {
                new { type = "text/plain", value = request.Body }
            }
        });

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
            return new EmailSendResult(true, "Email accepted by SendGrid.");

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new EmailSendResult(false, $"SendGrid returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }
}