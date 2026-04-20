using InteriorAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InteriorAI.Controllers;

public class AnalyzeRoomRequest
{
    public IFormFile? Image { get; set; }
    public string? Style { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class DesignController : ControllerBase
{
    private readonly IExternalAIService _externalAIService;

    public DesignController(IExternalAIService externalAIService)
    {
        _externalAIService = externalAIService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeRoom([FromForm] AnalyzeRoomRequest request)
    {
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest("Please upload an image of the room.");

        if (string.IsNullOrWhiteSpace(request.Style))
            return BadRequest("Please specify a design style (e.g., 'Modern', 'Scandinavian', 'Minimalist').");

        try
        {
            // Đọc file ảnh và chuyển thành chuỗi Base64
            using var memoryStream = new MemoryStream();
            await request.Image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            // Kiểm tra xem ảnh có hợp lệ không
            try
            {
                SixLabors.ImageSharp.Image.Identify(imageBytes);
            }
            catch
            {
                return BadRequest("The uploaded file is not a valid image. Please upload a valid image file (JPEG, PNG, etc.).");
            }

            var base64Image = Convert.ToBase64String(imageBytes);

            // Gọi service Gemini API
            var prompt = await _externalAIService.AnalyzeRoomAndGetDesignPromptAsync(base64Image, request.Style);

            return Ok(new
            {
                Success = true,
                StyleRequested = request.Style,
                SuggestedPrompt = prompt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "An error occurred while communicating with Gemini API.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("generate-design")]
    public async Task<IActionResult> GenerateDesign([FromForm] AnalyzeRoomRequest request)
    {
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest("Please upload an image of the room.");

        if (string.IsNullOrWhiteSpace(request.Style))
            return BadRequest("Please specify a design style.");

        try
        {
            // Bước 1: Đọc ảnh và lấy Prompt từ Gemini Vision
            using var memoryStream = new MemoryStream();
            await request.Image.CopyToAsync(memoryStream);
            var imageBytesData = memoryStream.ToArray();

            // Kiểm tra xem ảnh có hợp lệ không
            SixLabors.ImageSharp.ImageInfo imageInfo;
            try
            {
                imageInfo = SixLabors.ImageSharp.Image.Identify(imageBytesData);
                if (imageInfo == null)
                    return BadRequest("The uploaded file is not a valid image. Please upload a valid image file (JPEG, PNG, etc.).");
            }
            catch
            {
                return BadRequest("The uploaded file is not a valid image. Please upload a valid image file (JPEG, PNG, etc.).");
            }

            var base64Image = Convert.ToBase64String(imageBytesData);
            int width = imageInfo.Width;
            int height = imageInfo.Height;

            var prompt = await _externalAIService.AnalyzeRoomAndGetDesignPromptAsync(base64Image, request.Style);

            // Bước 2: Dùng Prompt đó để gọi NanoBanana API tạo ảnh
            var generatedImageBase64 = await _externalAIService.GenerateImageAsync(prompt, base64Image);
            var imageBytes = Convert.FromBase64String(generatedImageBase64);

            // Trả về trực tiếp dưới dạng File ảnh JPEG
            return File(imageBytes, "image/jpeg", "redesigned_room.jpg");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error generating design", Error = ex.Message });
        }
    }
}
