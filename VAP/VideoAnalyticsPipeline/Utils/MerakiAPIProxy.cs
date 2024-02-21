using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace VideoAnalyticsPipeline;

internal class MerakiAPIProxy(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly HttpClient httpClientWithRetry = httpClientFactory.CreateClient("HttpClientWithRetry");

    internal async ValueTask<string> GetImageUrl<T>(T payload, string camSerial, CancellationToken cancellationToken)
    {
        httpClientWithRetry.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", configuration["MerakiApi:BearerToken"]);
        
        httpClientWithRetry.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var jsonPayload = JsonSerializer.Serialize(payload);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var url = string.Format(configuration["MerakiApi:Url"]!, camSerial);

        var response = await httpClientWithRetry.PostAsync(url, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        var errorMessage = await BuildErrorMessage(response, cancellationToken);
        errorMessage += $"Payload: {jsonPayload}";

        throw new Exception(errorMessage);
    }

    internal async ValueTask<Stream> GetImage(string url, CancellationToken cancellationToken)
    {
        var response = await httpClientWithRetry.GetAsync(url, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }

        throw new Exception(await BuildErrorMessage(response, cancellationToken));
    }

    private static async Task<string> BuildErrorMessage(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = response.StatusCode;
        var reasonPhrase = response.ReasonPhrase;
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new StringBuilder()
            .AppendLine($"Status Code: {statusCode}")
            .AppendLine($"Reason Phrase: {reasonPhrase}")
            .AppendLine($"Content: {content}")
            .ToString();
    }
}