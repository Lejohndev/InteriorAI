namespace InteriorAI.Models.DTOs
{
    public class RegisterDeviceRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
public class UserProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}