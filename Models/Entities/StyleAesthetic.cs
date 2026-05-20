namespace InteriorAI.Domain.Entities
{
 public class StyleAesthetic
    {
        public int StyleID { get; set; }
        public string StyleName { get; set; }
        public string CoreAesthetic { get; set; }
        public string BaseStructuralPrompt { get; set; }
        
        public List<string> LightingOptions { get; set; } = new List<string>();
        public List<string> MaterialOptions { get; set; } = new List<string>();
        public List<string> ColorRuleOptions { get; set; } = new List<string>();
        public List<string> AtmosphereOptions { get; set; } = new List<string>();
        
        public string SpecificNegative { get; set; }
        public string TechnicalSpecs { get; set; }

        // Thuộc tính điều hướng: 1 phong cách có thể áp dụng cho nhiều phòng
        public virtual ICollection<RoomInterior> RoomInteriors { get; set; } 
            = new List<RoomInterior>();
    }
}
