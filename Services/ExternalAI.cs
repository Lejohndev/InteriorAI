using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace InteriorAI.Services;

public interface IExternalAIService
{
    Task<string> UploadImageAsync(string base64Image);
    Task<string> AnalyzeRoomAndGetDesignPromptAsync(string base64Image, string style);
    Task<string> GenerateImageAsync(string prompt, string base64Image);
}

public class ExternalAI : IExternalAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalAI> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _nanoBananaApiKey;

    public ExternalAI(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalAI> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = _configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key is missing.");
        _model = _configuration["Gemini:Model"] ?? "gemini-3.0-flash";
        _nanoBananaApiKey = _configuration["NanoBanana:ApiKey"] ?? "";
    }

    private async Task<T> RetryWithBackoffAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
    {
        int retryCount = 0;
        int delayMs = 1000;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError($"Operation failed after {maxRetries} retries: {ex.Message}");
                    throw;
                }

                _logger.LogWarning($"Retrying operation (attempt {retryCount}) after {delayMs}ms due to: {ex.Message}");
                await Task.Delay(delayMs);
                delayMs = (int)(delayMs * 1.5);
            }
        }
    }

    // Upload ảnh lên ImgBB 
    public Task<string> UploadImageAsync(string base64Image)
    {
        return UploadImageToImgBBAsync(base64Image);
    }

    private async Task<string> UploadImageToImgBBAsync(string base64Image)
    {
        // Đọc trực tiếp từ appsettings.json
        var apiKey = _configuration["ImgBB:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("Thiếu cấu hình ImgBB:ApiKey trong appsettings.json.");
        }

        var url = $"https://api.imgbb.com/1/upload?key={apiKey}";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("image", base64Image)
        });

        _logger.LogInformation("Đang upload ảnh lên ImgBB...");
        var response = await _httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Lỗi upload ImgBB: {responseString}");
        }

        using var document = JsonDocument.Parse(responseString);
        var root = document.RootElement;

        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("url", out var imageUrlElement))
        {
            var publicUrl = imageUrlElement.GetString()!;
            _logger.LogInformation($"Upload thành công! URL: {publicUrl}");
            return publicUrl;
        }

        throw new Exception("Không lấy được URL từ response của ImgBB.");
    }

    public async Task<string> AnalyzeRoomAndGetDesignPromptAsync(string base64Image, string style)
    {
        return await RetryWithBackoffAsync(async () =>
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var systemPrompt =$"{style}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = systemPrompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API failed: {errorDetail}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);
            var root = document.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentElement) &&
                    contentElement.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString() ?? "Failed to extract text.";
                }
            }

            throw new Exception("Unexpected JSON structure from Gemini API.");
        });
    }

    public async Task<string> GenerateImageAsync(string prompt, string base64Image)
    {
        return await RetryWithBackoffAsync(async () =>
        {
            var generateUrl = "https://api.nanobananaapi.ai/api/v1/nanobanana/generate-2";

            // Bước 1: Nén ảnh để upload lên ImgBB
            var imageBytes = Convert.FromBase64String(base64Image);

            // 1.1 Tách riêng việc lấy Format (để lúc sau Save lại đúng chuẩn)
            var format = Image.DetectFormat(imageBytes);

            // 1.2 Load ảnh vào bộ nhớ 
            using var image = Image.Load(imageBytes);

            if (image.Width > 1024 || image.Height > 1024)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max, 
                    Size = new Size(1024, 1024)
                }));
            }

            using var ms = new MemoryStream();
            // 1.3 Lưu lại ảnh bằng format gốc đã nhận diện 
            if (format != null)
            {
                image.Save(ms, format);
            }
            else
            {
                image.SaveAsJpeg(ms); 
            }

            var optimizedBase64 = Convert.ToBase64String(ms.ToArray());

            // Bước 2: Upload lấy link Public
            var publicImageUrl = await UploadImageToImgBBAsync(optimizedBase64);

            // Bước 3: Tạo payload chuẩn của NanoBanana với link ảnh vừa lấy được
            var payload = new
            {
                prompt = prompt,
                imageUrls = new[] { publicImageUrl }, 
                aspectRatio = "auto",                 
                resolution = "1K",                   
                outputFormat = "jpg"
            };

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, generateUrl);
            request.Content = content;

            if (!string.IsNullOrEmpty(_nanoBananaApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nanoBananaApiKey);
            }

            _logger.LogInformation("Gửi yêu cầu tạo ảnh đến NanoBanana...");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync();
                throw new Exception($"NanoBanana API lỗi HTTP {response.StatusCode}: {errorDetail}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);
            var root = document.RootElement;

            if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 200)
            {
                var msg = root.TryGetProperty("message", out var msgElement) ? msgElement.GetString() : "Unknown error";
                throw new Exception($"NanoBanana từ chối (code {codeElement.GetInt32()}): {msg}");
            }

            string taskId = root.GetProperty("data").GetProperty("taskId").GetString()!;
            _logger.LogInformation($"Task tạo ảnh ID: {taskId}. Đang chờ kết quả...");

            // Bước 4: Polling kết quả
            var recordInfoUrl = "https://api.nanobananaapi.ai/api/v1/nanobanana/record-info";
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(5000);
                var pollRequest = new HttpRequestMessage(HttpMethod.Get, $"{recordInfoUrl}?taskId={taskId}");
                pollRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nanoBananaApiKey);

                var pollResp = await _httpClient.SendAsync(pollRequest);
                var pollStr = await pollResp.Content.ReadAsStringAsync();
                using var pollDoc = JsonDocument.Parse(pollStr);
                var pollData = pollDoc.RootElement.GetProperty("data");

                int successFlag = pollData.GetProperty("successFlag").GetInt32();
                if (successFlag == 1) // Thành công
                {
                    var imageUrl = pollData.GetProperty("response").GetProperty("resultImageUrl").GetString();
                    _logger.LogInformation($"Render thành công! Đang tải về...");

                    var imgDownload = await _httpClient.GetAsync(imageUrl);
                    return Convert.ToBase64String(await imgDownload.Content.ReadAsByteArrayAsync());
                }
                else if (successFlag == 2 || successFlag == 3) // Thất bại
                {
                    var errorMsg = pollData.TryGetProperty("errorMessage", out var errMsg) ? errMsg.GetString() : "Unknown";
                    throw new Exception($"AI xử lý ảnh thất bại: {errorMsg}");
                }
            }

            throw new Exception("Quá thời gian chờ kết quả (5 phút).");
        });
    }
}
