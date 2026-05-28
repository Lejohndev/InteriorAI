namespace InteriorAI.Data.Seed;

internal static class DesignStyleSeedData
{
    public static IReadOnlyList<StyleAestheticSeed> All { get; } = new[]
    {
        new StyleAestheticSeed(
            "Japandi",
            "Japandi (Japanese-Scandinavian fusion)",
            "The interior space completely redesigned as a residential Japandi interior while preserving the uploaded room geometry, camera angle, and architectural boundaries.",
            new[] { "soft morning sunlight creating soft shadows on textured walls", "warm glowing ambient light from paper lanterns", "diffused golden hour light filtering through bamboo blinds" },
            new[] { "light oak wood grain", "organic linen", "raw plaster walls", "slatted timber screens", "natural bamboo", "muted clay ceramics" },
            new[] { "warm sand", "light oak", "muted sage green", "off-white", "warm beige", "charcoal grey accents" },
            new[] { "serene", "zen", "grounding", "calm", "mindful", "harmonious" },
            "(cluttered:1.4), (messy:1.4), distorted windows, broken frames, melted architecture, extra legs, deformed objects, watermark, text, shiny plastic, neon colors, heavy classical moldings, excessive glossy metal, overly bright artificial lighting, classical symmetry",
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k."),
        new StyleAestheticSeed(
            "Tropical",
            "Tropical",
            "The interior space completely redesigned as a residential Tropical interior while preserving the uploaded room geometry, camera angle, and architectural boundaries.",
            new[] { "bright tropical sunshine filtering through large green leaves", "airy and breezy natural daylight", "warm golden hour light casting palm frond shadows" },
            new[] { "natural bamboo", "woven seagrass", "teak wood", "palm leaf motifs", "terrazzo flooring", "cane furniture" },
            new[] { "airy white", "vibrant jungle green", "natural wood tones", "soft beige", "deep emerald green", "coral pink accents" },
            new[] { "lush", "fresh", "exotic", "breezy", "relaxing", "vacation-like" },
            "(cluttered:1.4), (messy:1.4), distorted windows, broken frames, melted architecture, extra legs, deformed objects, watermark, text, barren, desert aesthetic, cold raw concrete, dark moody industrial, heavy velvet, snow, winter vibe",
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k.")
    };

    public static IReadOnlyList<string> SupportedStyleNames { get; } = All
        .Select(style => style.StyleName)
        .ToArray();
}
