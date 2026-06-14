namespace InteriorAI.Data.Seed;

internal static class DesignStyleSeedData
{
    public static IReadOnlyList<StyleAestheticSeed> All { get; } = new[]
    {
        new StyleAestheticSeed(
            "Indochine",
            "Refined Indochine aesthetic",
            new[] { "soft ambient cove lighting from a round recessed ceiling", "warm glows from symmetrical table lamps", "hanging lantern-style pendants" },
            new[] { "dark mahogany wood", "crisp white painted moldings", "rich silk and velvet textiles" },
            new[] { "50% crisp white", "30% dark espresso wood", "20% jade green with subtle yellow accents" },
            new[] { "elegant", "highly symmetrical", "culturally rich" },
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 4k."),

        new StyleAestheticSeed(
            "Neoclassic",
            "Refined residential Neoclassic style",
            new[] { "soft ambient spring morning light with warm shadows", "bright airy summer daylight", "romantic autumn evening lighting" },
            new[] { "polished marble", "painted white wood moldings", "premium velvet", "chevron oak wood flooring", "brushed gold hardware" },
            new[] { "60% creamy whites", "30% champagne and taupe", "10% dusty rose", "10% brushed gold and brass metallic accents" },
            new[] { "elegant", "romantic", "luxurious", "majestic", "refined" },
            "Ultra-realistic architectural photography, luxury interior magazine quality, HDR rendering, realistic global illumination, physically based materials, ultra-detailed textures, soft shadows, realistic reflections, 8K resolution."),

        new StyleAestheticSeed(
            "Scandinavian",
            "Cozy, 'Hygge'-inspired residential Scandinavian style",
            new[] { "abundant bright natural daylight", "soft warm indoor lighting against a cold window view", "diffused overcast daylight" },
            new[] { "light pine wood", "birch wood veneer", "chunky knit wool", "matte white surfaces", "boucle fabric" },
            new[] { "70% crisp white", "20% light wood", "10% pastel accents", "60% warm off-white" },
            new[] { "cozy", "hygge", "inviting", "bright", "functional", "serene" },
            "Ultra-realistic architectural photography, luxury interior magazine quality, HDR rendering, realistic global illumination, physically based materials, ultra-detailed textures, soft shadows, realistic reflections, 8K resolution.")
    };

    public static IReadOnlyList<string> SupportedStyleNames { get; } = All
        .Select(style => style.StyleName)
        .ToArray();
}