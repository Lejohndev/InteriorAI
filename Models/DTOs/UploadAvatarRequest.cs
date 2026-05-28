using Microsoft.AspNetCore.Http;

namespace InteriorAI.Models.DTOs
{
    public class UploadAvatarRequest
    {
        public string UserId { get; set; } = string.Empty;

        public IFormFile File { get; set; } = null!;
    }
}