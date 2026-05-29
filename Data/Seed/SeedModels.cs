namespace InteriorAI.Data.Seed;

internal sealed record StyleAestheticSeed(
    string StyleName,
    string CoreAesthetic,
    string BaseStructuralPrompt,
    IReadOnlyList<string> LightingOptions,
    IReadOnlyList<string> MaterialOptions,
    IReadOnlyList<string> ColorRuleOptions,
    IReadOnlyList<string> AtmosphereOptions,
    string SpecificNegative,
    string TechnicalSpecs);

internal sealed record RoomStylePromptSeed(
    string StyleName,
    string RoomTypeKey,
    string RoomTypeName,
    string Variant,
    string Lighting,
    string Material,
    string Color,
    string Furniture,
    string Atmosphere);
