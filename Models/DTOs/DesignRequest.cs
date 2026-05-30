using Microsoft.AspNetCore.Http;

namespace InteriorAI.Models.DTOs
{
    public class DesignRequest
    {
        public IFormFile? Image { get; set; }
        public int? StyleId { get; set; }
        public string? StyleName { get; set; }
        public string? Style { get; set; }
        public string? RoomType { get; set; }
    }
}
