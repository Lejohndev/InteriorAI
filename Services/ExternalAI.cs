using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace InteriorAI.Services;

public interface IExternalAIService
{
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
        // Sử dụng model chuẩn xác định của Google AI Studio
        _model = _configuration["Gemini:Model"] ?? "gemini-3.0-flash";
        _nanoBananaApiKey = _configuration["NanoBanana:ApiKey"] ?? "";
    }

    private async Task<T> RetryWithBackoffAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
    {
        int retryCount = 0;
        int delayMs = 1000; // Start with 1 second

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
                delayMs = (int)(delayMs * 1.5); // Exponential backoff
            }
        }
    }

    public async Task<string> AnalyzeRoomAndGetDesignPromptAsync(string base64Image, string style)
    {
        return await RetryWithBackoffAsync(async () =>
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            // Tối ưu prompt: Đóng vai chuyên gia, ép giữ nguyên cấu trúc và thêm từ khóa Render 2K/8K siêu thực
            var systemPrompt = $@"You are an expert interior designer and AI prompt engineer. 
Analyze the provided image of a room carefully. Your goal is to generate a highly detailed prompt for an AI image generator to redesign this room in the '{style}' style.
CRITICAL INSTRUCTIONS:
1. Describe the exact structural layout of the room in the image (walls, windows, doors, main furniture placement) so the AI generator keeps the same structure.
2. Apply the '{style}' design style to the room, describing specific materials, colors, lighting, and decor elements suitable for this style.
3. Keep the prompt in English.
4. Add these keywords at the end of the prompt for maximum realism: photorealistic, 8k resolution, architectural photography, highly detailed, unreal engine 5 render, volumetric lighting --iw 2.0
Only return the prompt text, nothing else.";


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
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new Exception($"Rate limit exceeded. Please try again later: {errorDetail}");
                }
                throw new Exception($"Gemini Analysis API failed: {errorDetail}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);
            var root = document.RootElement;

            // Bóc tách JSON an toàn (Tránh Crash do Safety Block)
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];

                // Kiểm tra xem AI có từ chối trả lời vì lý do an toàn không
                if (firstCandidate.TryGetProperty("finishReason", out var finishReason) && finishReason.GetString() == "SAFETY")
                {
                    throw new Exception("Ảnh tải lên vi phạm chính sách an toàn của Google (ví dụ: chứa người).");
                }

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

            // Bước 1: Gửi yêu cầu tạo ảnh
            // Giải mã base64 để lấy kích thước ảnh
            using var image = Image.Load(Convert.FromBase64String(base64Image));

            // Tính toán Aspect Ratio (Tỷ lệ khung hình) để truyền vào prompt cho Midjourney/NanoBanana
            var finalPrompt = $"{prompt} --ar {image.Width}:{image.Height}";

            var payload = new
            {
                prompt = finalPrompt,
                type = "IMAGETOIMAGE",
                numImages = 1,
                base64Array = new[] { "data:image/jpeg;base64," + base64Image }
            };

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, generateUrl);
            request.Content = content;

            if (!string.IsNullOrEmpty(_nanoBananaApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nanoBananaApiKey);
            }

            _logger.LogInformation($"Sending image generation request to {generateUrl}");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync();
                _logger.LogError($"NanoBanana API error: {response.StatusCode} - {errorDetail}");
                throw new Exception($"Image generation failed: {errorDetail}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"NanoBanana response: {responseString}");

            using var document = JsonDocument.Parse(responseString);
            var root = document.RootElement;

            // Kiểm tra code response trước
            if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 200)
            {
                var errorMsg = "Unknown error";
                if (root.TryGetProperty("msg", out var msgElement))
                {
                    errorMsg = msgElement.GetString() ?? "Unknown error";
                }
                throw new Exception($"NanoBanana API error (code {codeElement.GetInt32()}): {errorMsg}");
            }

            // Trích xuất taskId từ response
            string? taskId = null;

            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                if (dataElement.TryGetProperty("taskId", out var taskIdElement))
                {
                    taskId = taskIdElement.GetString();
                }
            }

            if (string.IsNullOrEmpty(taskId))
            {
                _logger.LogError($"Invalid response structure. Full response: {responseString}");
                throw new Exception("Failed to get taskId from NanoBanana API response. Data: " + responseString);
            }

            _logger.LogInformation($"Task created with ID: {taskId}. Polling for completion...");

            // Bước 2: Poll cho đến khi tác vụ hoàn thành
            var recordInfoUrl = "https://api.nanobananaapi.ai/api/v1/nanobanana/record-info";
            var maxPolls = 60; // 60 * 5 seconds = 300 seconds (5 phút)
            var pollCount = 0;

            while (pollCount < maxPolls)
            {
                await Task.Delay(5000); // Chờ 5 giây trước poll
                pollCount++;

                var pollRequest = new HttpRequestMessage(HttpMethod.Get, $"{recordInfoUrl}?taskId={taskId}");
                pollRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nanoBananaApiKey);

                var pollResponse = await _httpClient.SendAsync(pollRequest);
                var pollResponseString = await pollResponse.Content.ReadAsStringAsync();

                _logger.LogInformation($"Poll attempt {pollCount}: {pollResponseString}");

                using var pollDocument = JsonDocument.Parse(pollResponseString);
                var pollRoot = pollDocument.RootElement;

                if (pollRoot.TryGetProperty("data", out var pollDataElement) &&
                    pollDataElement.ValueKind == JsonValueKind.Object)
                {
                    if (pollDataElement.TryGetProperty("successFlag", out var successFlagElement))
                    {
                        var successFlag = successFlagElement.GetInt32();

                        if (successFlag == 1) // SUCCESS
                        {
                            if (pollDataElement.TryGetProperty("response", out var responseElement) &&
                                responseElement.ValueKind == JsonValueKind.Object)
                            {
                                if (responseElement.TryGetProperty("resultImageUrl", out var imageUrlElement))
                                {
                                    var imageUrl = imageUrlElement.GetString();
                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        _logger.LogInformation($"Image generation succeeded. Downloading from: {imageUrl}");

                                        // Tải ảnh từ URL
                                        var imageResponse = await _httpClient.GetAsync(imageUrl);
                                        if (imageResponse.IsSuccessStatusCode)
                                        {
                                            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
                                            _logger.LogInformation($"Image downloaded successfully. Size: {imageBytes.Length} bytes");
                                            return Convert.ToBase64String(imageBytes);
                                        }
                                        else
                                        {
                                            _logger.LogError($"Failed to download image. Status: {imageResponse.StatusCode}");
                                            throw new Exception($"Failed to download image from {imageUrl}: {imageResponse.StatusCode}");
                                        }
                                    }
                                }
                            }
                            throw new Exception("Response element missing resultImageUrl. Full response: " + pollResponseString);
                        }
                        else if (successFlag == 2 || successFlag == 3) // FAILED
                        {
                            if (pollDataElement.TryGetProperty("errorMessage", out var errorMsgElement))
                            {
                                throw new Exception($"Image generation failed: {errorMsgElement.GetString()}");
                            }
                            throw new Exception("Image generation failed.");
                        }
                        // successFlag == 0 means still generating, continue polling
                    }
                }
            }

            throw new Exception("Image generation timed out after 5 minutes.");
        });
    }
}