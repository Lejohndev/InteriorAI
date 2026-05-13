using System.ComponentModel.DataAnnotations;

namespace InteriorAI.Models.Entities
{
    public class DesignResult
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string OriginalImageUrl { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? DesignedImageUrl { get; set; }

        public string? DesignPrompt { get; set; }

        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = "pending";

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
