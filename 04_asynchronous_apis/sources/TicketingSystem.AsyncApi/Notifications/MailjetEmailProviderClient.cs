using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace TicketingSystem.AsyncApi.Notifications;

public sealed class MailjetOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Ticketing System";
}

public interface IEmailProviderClient
{
    Task<EmailSendResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
}

public sealed class MailjetEmailProviderClient(HttpClient httpClient, IOptions<MailjetOptions> options) : IEmailProviderClient
{
    private readonly MailjetOptions _options = options.Value;

    public async Task<EmailSendResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            return new EmailSendResult(true, "Mailjet is not configured locally; using mock success response.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            return new EmailSendResult(true, "Mailjet from email is not configured locally; using mock success response.");
        }

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, "https://api.mailjet.com/v3.1/send");
        string basicAuthValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.ApiKey}:{_options.ApiSecret}"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthValue);
        httpRequest.Content = JsonContent.Create(new
        {
            Messages = new[]
            {
                new
                {
                    From = new { Email = _options.FromEmail, Name = _options.FromName },
                    To = new[] { new { Email = request.ToEmail, Name = request.ToName } },
                    Subject = request.Subject,
                    TextPart = request.Body
                }
            }
        });

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
            return new EmailSendResult(true, "Email accepted by Mailjet.");

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new EmailSendResult(false, $"Mailjet returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }
}