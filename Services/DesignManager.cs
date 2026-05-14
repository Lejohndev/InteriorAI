using InteriorAI.Data;
using InteriorAI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace InteriorAI.Services
{
    public class DesignManager
    {
        private const long MaxImageBytes = 10 * 1024 * 1024;
        private const string DefaultStyle = "Modern interior design";

        private readonly AppDbContext _context;
        private readonly IExternalAIService _externalAIService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DesignManager> _logger;

        public DesignManager(
            AppDbContext context,
            IExternalAIService externalAIService,
            IServiceScopeFactory scopeFactory,
            ILogger<DesignManager> logger)
        {
            _context = context;
            _externalAIService = externalAIService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<DesignResult> CreateDesignAsync(string userId, IFormFile image, string? style)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId is required.", nameof(userId));
            }

            if (!await _context.Users.AnyAsync(user => user.Id == userId))
            {
                throw new KeyNotFoundException("User does not exist.");
            }

            var imageBytes = await ReadAndValidateImageAsync(image);
            var base64Image = Convert.ToBase64String(imageBytes);
            var originalImageUrl = await _externalAIService.UploadImageAsync(base64Image);

            var design = new DesignResult
            {
                UserId = userId,
                OriginalImageUrl = originalImageUrl,
                Status = DesignStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DesignResults.Add(design);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Design job {DesignId} was created for user {UserId}.", design.Id, userId);
            StartProcessingInBackground(design.Id, base64Image, style);

            return design;
        }

        public async Task<DesignResult?> GetDesignStatusAsync(string designId, string userId)
        {
            if (string.IsNullOrWhiteSpace(designId))
            {
                throw new ArgumentException("DesignId is required.", nameof(designId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId is required.", nameof(userId));
            }

            if (!await _context.Users.AnyAsync(user => user.Id == userId))
            {
                throw new KeyNotFoundException("User does not exist.");
            }

            var design = await _context.DesignResults
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == designId && !item.IsDeleted);

            if (design == null)
            {
                return null;
            }

            if (design.UserId != userId)
            {
                throw new UnauthorizedAccessException("Design does not belong to this user.");
            }

            return design;
        }

        public async Task ProcessDesignAsync(string designId, string base64Image, string? style)
        {
            var design = await _context.DesignResults.FirstOrDefaultAsync(item => item.Id == designId);
            if (design == null)
            {
                _logger.LogWarning("Design job {DesignId} was not found for processing.", designId);
                return;
            }

            if (design.Status == DesignStatuses.Completed)
            {
                _logger.LogInformation("Design job {DesignId} is already completed. Skipping duplicate processing.", designId);
                return;
            }

            try
            {
                var selectedStyle = string.IsNullOrWhiteSpace(style) ? DefaultStyle : style.Trim();
                var prompt = await _externalAIService.AnalyzeRoomAndGetDesignPromptAsync(base64Image, selectedStyle);
                var designedImageBase64 = await _externalAIService.GenerateImageAsync(prompt, base64Image);
                var designedImageUrl = await _externalAIService.UploadImageAsync(designedImageBase64);

                design.DesignPrompt = prompt;
                design.DesignedImageUrl = designedImageUrl;
                design.Status = DesignStatuses.Completed;
                design.ErrorMessage = null;
                design.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Design job {DesignId} completed.", designId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Design job {DesignId} failed.", designId);

                design.Status = DesignStatuses.Failed;
                design.ErrorMessage = Truncate(ex.Message, 2000);
                design.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        private void StartProcessingInBackground(string designId, string base64Image, string? style)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var manager = scope.ServiceProvider.GetRequiredService<DesignManager>();

                    await manager.ProcessDesignAsync(designId, base64Image, style);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to start background processing for design job {DesignId}.", designId);
                }
            });
        }

        private static async Task<byte[]> ReadAndValidateImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                throw new InvalidDataException("Please upload an image of the room.");
            }

            if (image.Length > MaxImageBytes)
            {
                throw new InvalidDataException("Image is too large. Maximum size is 10MB.");
            }

            if (!string.IsNullOrWhiteSpace(image.ContentType) &&
                !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The uploaded file must be an image.");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            try
            {
                if (Image.Identify(imageBytes) == null)
                {
                    throw new InvalidDataException("The uploaded file is not a valid image.");
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new InvalidDataException("The uploaded file is not a valid image.");
            }

            return imageBytes;
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
