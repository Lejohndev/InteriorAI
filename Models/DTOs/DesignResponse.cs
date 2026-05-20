namespace InteriorAI.Models.DTOs
{
    public class DesignResponse
    {
        public string DesignId { get; set; } = string.Empty;
        public string OriginalImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DesignStatusResponse
    {
        public string DesignId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string OriginalImageUrl { get; set; } = string.Empty;
        public string? DesignedImageUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DesignStyleResponse
    {
        public int StyleId { get; set; }
        public string StyleName { get; set; } = string.Empty;
    }
}
