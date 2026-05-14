using InteriorAI.Models.DTOs;
using InteriorAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InteriorAI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DesignController : ControllerBase
{
    private readonly DesignManager _designManager;
    private readonly IExternalAIService _externalAIService;
    private readonly ILogger<DesignController> _logger;
    private readonly IWebHostEnvironment _environment;

    public DesignController(
        DesignManager designManager,
        IExternalAIService externalAIService,
        ILogger<DesignController> logger,
        IWebHostEnvironment environment)
    {
        _designManager = designManager;
        _externalAIService = externalAIService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeImage([FromForm] DesignRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { message = "Missing required user-id header." });
        }

        try
        {
            var design = await _designManager.CreateDesignAsync(userId, request.Image!, request.Style);

            return Ok(new DesignResponse
            {
                DesignId = design.Id,
                OriginalImageUrl = design.OriginalImageUrl,
                Status = design.Status
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create design job for user {UserId}.", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating design job." });
        }
    }

    [HttpGet("status/{designId}")]
    public async Task<IActionResult> GetStatus(string designId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { message = "Missing required user-id header." });
        }

        try
        {
            var design = await _designManager.GetDesignStatusAsync(designId, userId);
            if (design == null)
            {
                return NotFound(new { message = "Design job was not found." });
            }

            return Ok(new DesignStatusResponse
            {
                DesignId = design.Id,
                Status = design.Status,
                OriginalImageUrl = design.OriginalImageUrl,
                DesignedImageUrl = design.DesignedImageUrl,
                ErrorMessage = design.ErrorMessage
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for design job {DesignId}.", designId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error getting design status." });
        }
    }

    [HttpPost("generate-design")]
    public async Task<IActionResult> GenerateDesign([FromForm] DesignRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (request.Image == null || request.Image.Length == 0)
        {
            return BadRequest("Please upload an image of the room.");
        }

        if (string.IsNullOrWhiteSpace(request.Style))
        {
            return BadRequest("Please specify a design style.");
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await request.Image.CopyToAsync(memoryStream);
            var imageBytesData = memoryStream.ToArray();

            SixLabors.ImageSharp.ImageInfo imageInfo;
            try
            {
                imageInfo = SixLabors.ImageSharp.Image.Identify(imageBytesData);
                if (imageInfo == null)
                {
                    return BadRequest("The uploaded file is not a valid image. Please upload a valid image file (JPEG, PNG, etc.).");
                }
            }
            catch
            {
                return BadRequest("The uploaded file is not a valid image. Please upload a valid image file (JPEG, PNG, etc.).");
            }

            var base64Image = Convert.ToBase64String(imageBytesData);
            var prompt = await _externalAIService.AnalyzeRoomAndGetDesignPromptAsync(base64Image, request.Style);
            var generatedImageBase64 = await _externalAIService.GenerateImageAsync(prompt, base64Image);
            var imageBytes = Convert.FromBase64String(generatedImageBase64);

            return File(imageBytes, "image/jpeg", "redesigned_room.jpg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating design through development endpoint.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error generating design." });
        }
    }

    private string GetUserId()
    {
        return HttpContext.Request.Headers["user-id"].ToString();
    }
}
