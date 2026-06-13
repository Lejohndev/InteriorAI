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
            "Industrial",
            "Modern residential industrial loft aesthetic",
            new[] { "warm glowing LED under-shelf lighting paired with industrial pendant lights", "dramatic lighting from large grid-pane factory windows", "warm glowing Edison bulbs suspended from black cords" },
            new[] { "exposed red brick", "matte black wood", "stainless steel", "distressed leather", "reclaimed rough wood", "matte dark charcoal walls" },
            new[] { "50% matte black, 30% rustic brick red, 20% warm wood", "60% dark charcoal, 30% warm cognac leather, 10% subtle brass" },
            new[] { "moody, bold, and highly textured", "raw, masculine, and edgy", "urban, rugged, and historic" },
            "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k.")
    };

    public static IReadOnlyList<string> SupportedStyleNames { get; } = All
        .Select(style => style.StyleName)
        .ToArray();
}
