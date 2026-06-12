namespace InteriorAI.Data.Seed;

internal static class DesignStyleSeedData
{
    public static IReadOnlyList<StyleAestheticSeed> All { get; } = new[]
    {
        new StyleAestheticSeed(
            "Japandi",
            "Japandi (Japanese-Scandinavian fusion)",
            new[] { "soft morning sunlight creating soft shadows on textured walls", "warm glowing ambient light from paper lanterns", "diffused golden hour light filtering through bamboo blinds" },
            new[] { "light oak wood grain", "organic linen", "raw plaster walls", "slatted timber screens", "natural bamboo", "muted clay ceramics" },
            new[] { "warm sand", "light oak", "muted sage green", "off-white", "warm beige", "charcoal grey accents" },
            new[] { "serene", "zen", "grounding", "calm", "mindful", "harmonious" },
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k."),
        new StyleAestheticSeed(
            "Tropical",
            "Tropical",
            new[] { "bright tropical sunshine filtering through large green leaves", "airy and breezy natural daylight", "warm golden hour light casting palm frond shadows" },
            new[] { "natural bamboo", "woven seagrass", "teak wood", "palm leaf motifs", "terrazzo flooring", "cane furniture" },
            new[] { "airy white", "vibrant jungle green", "natural wood tones", "soft beige", "deep emerald green", "coral pink accents" },
            new[] { "lush", "fresh", "exotic", "breezy", "relaxing", "vacation-like" },
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k."),
        new StyleAestheticSeed(
            "Indochine",
            "Refined Indochine aesthetic",
            new[] { "soft ambient cove lighting from a round recessed ceiling", "warm glows from symmetrical table lamps", "hanging lantern-style pendants" },
            new[] { "dark mahogany wood", "crisp white painted moldings", "rich silk and velvet textiles" },
            new[] { "50% crisp white", "30% dark espresso wood", "20% jade green with subtle yellow accents" },
            new[] { "elegant", "highly symmetrical", "culturally rich" },
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 4k.")
    };

    public static IReadOnlyList<string> SupportedStyleNames { get; } = All
        .Select(style => style.StyleName)
        .ToArray();
}
