using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GarageHub.Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace GarageHub.Service.Ocr;

/// <summary>Calls the Plate Recognizer Snapshot Cloud API (POST /v1/plate-reader/).</summary>
public class PlateRecognizerClient : IPlateRecognizerClient
{
    private const string Endpoint = "https://api.platerecognizer.com/v1/plate-reader/";
    private const string TokenKey = "PLATE_RECOGNIZER_API_TOKEN";
    private const string VietnamRegion = "vn";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PlateRecognizerClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<PlateReadResult?> ReadPlateAsync(Stream image, string fileName, CancellationToken cancellationToken = default)
    {
        // Token is checked on use (like PayOS keys), so the app starts without it.
        var token = _configuration[TokenKey];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException($"Thiếu cấu hình OCR: {TokenKey}. Đặt bằng dotnet user-secrets.");
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(image), "upload", fileName);
        content.Add(new StringContent(VietnamRegion), "regions");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge)
        {
            throw new ValidationException("Ảnh không hợp lệ hoặc quá lớn");
        }
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<PlateReaderResponse>(cancellationToken);
        var best = body?.Results?
            .Where(r => !string.IsNullOrWhiteSpace(r.Plate))
            .OrderByDescending(r => r.Score)
            .FirstOrDefault();

        return best is null ? null : new PlateReadResult(best.Plate!, best.Score);
    }

    private sealed class PlateReaderResponse
    {
        [JsonPropertyName("results")]
        public List<PlateReaderResult>? Results { get; set; }
    }

    private sealed class PlateReaderResult
    {
        [JsonPropertyName("plate")]
        public string? Plate { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }
}
