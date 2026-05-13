using Microsoft.AspNetCore.Http;

namespace InteriorAI.Models.DTOs
{
    public class DesignRequest
    {
        public IFormFile? Image { get; set; }
        public string? Style { get; set; }
    }
}
