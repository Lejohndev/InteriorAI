using System.Text;
using InteriorAI.Data;
using InteriorAI.Domain.Entities;
using InteriorAI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InteriorAI.Services;

public interface IDesignPromptService
{
    Task EnsureDefaultStylesAsync();
    Task<List<DesignStyleResponse>> GetDesignStylesAsync();
    Task<string> GetConfiguredPromptAsync(int? styleId, string? styleName, string? legacyStyle);
}

public class DesignPromptService : IDesignPromptService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DesignPromptService> _logger;

    public DesignPromptService(AppDbContext context, ILogger<DesignPromptService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnsureDefaultStylesAsync()
    {
        if (await _context.StyleAesthetics.AnyAsync())
        {
            return;
        }

        _context.StyleAesthetics.AddRange(CreateDefaultStyles());
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded default design prompt styles.");
    }

    public async Task<List<DesignStyleResponse>> GetDesignStylesAsync()
    {
        return await _context.StyleAesthetics
            .AsNoTracking()
            .OrderBy(style => style.StyleName)
            .Select(style => new DesignStyleResponse
            {
                StyleId = style.StyleID,
                StyleName = style.StyleName,
                CoreAesthetic = style.CoreAesthetic,
                LightingOptions = style.LightingOptions,
                MaterialOptions = style.MaterialOptions,
                ColorRuleOptions = style.ColorRuleOptions,
                AtmosphereOptions = style.AtmosphereOptions
            })
            .ToListAsync();
    }

    public async Task<string> GetConfiguredPromptAsync(int? styleId, string? styleName, string? legacyStyle)
    {
        var styleKey = FirstNotEmpty(styleName, legacyStyle);
        if (styleId == null && string.IsNullOrWhiteSpace(styleKey))
        {
            throw new ArgumentException("Design styleId or styleName is required.");
        }

        StyleAesthetic? styleConfig = null;
        if (styleId != null)
        {
            styleConfig = await _context.StyleAesthetics
                .AsNoTracking()
                .FirstOrDefaultAsync(style => style.StyleID == styleId.Value);
        }

        if (styleConfig == null && !string.IsNullOrWhiteSpace(styleKey))
        {
            styleKey = styleKey.Trim();

            if (int.TryParse(styleKey, out var parsedStyleId))
            {
                styleConfig = await _context.StyleAesthetics
                    .AsNoTracking()
                    .FirstOrDefaultAsync(style => style.StyleID == parsedStyleId);
            }

            styleConfig ??= await _context.StyleAesthetics
                .AsNoTracking()
                .FirstOrDefaultAsync(style => style.StyleName == styleKey);
        }

        if (styleConfig == null)
        {
            var requestedStyle = styleId?.ToString() ?? styleKey;
            throw new KeyNotFoundException($"Design style '{requestedStyle}' was not found. Use an existing styleId or styleName.");
        }

        var prompt = BuildPrompt(styleConfig);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException($"Design style '{styleConfig.StyleName}' does not have a configured prompt.");
        }

        return prompt;
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string BuildPrompt(StyleAesthetic style)
    {
        var promptBuilder = new StringBuilder();

        AppendLineIfPresent(promptBuilder, style.BaseStructuralPrompt);
        AppendLineIfPresent(promptBuilder, style.CoreAesthetic);
        AppendListIfPresent(promptBuilder, "Lighting options", style.LightingOptions);
        AppendListIfPresent(promptBuilder, "Material options", style.MaterialOptions);
        AppendListIfPresent(promptBuilder, "Color rule options", style.ColorRuleOptions);
        AppendListIfPresent(promptBuilder, "Atmosphere options", style.AtmosphereOptions);
        AppendLineIfPresent(promptBuilder, style.TechnicalSpecs);

        if (!string.IsNullOrWhiteSpace(style.SpecificNegative))
        {
            promptBuilder.Append("Avoid: ");
            promptBuilder.AppendLine(style.SpecificNegative.Trim());
        }

        return promptBuilder.ToString().Trim();
    }

    private static void AppendLineIfPresent(StringBuilder promptBuilder, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            promptBuilder.AppendLine(value.Trim());
        }
    }

    private static void AppendListIfPresent(StringBuilder promptBuilder, string label, IEnumerable<string> values)
    {
        var configuredValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

        if (configuredValues.Length == 0)
        {
            return;
        }

        promptBuilder.Append(label);
        promptBuilder.Append(": ");
        promptBuilder.AppendLine(string.Join(", ", configuredValues));
    }

    private static IReadOnlyList<StyleAesthetic> CreateDefaultStyles()
    {
        return new[]
        {
            new StyleAesthetic
            {
                StyleName = "Modern",
                CoreAesthetic = "Modern interior design with clean lines, functional layout, balanced proportions, and refined contemporary furniture.",
                BaseStructuralPrompt = "Transform the uploaded room into a photorealistic modern interior while preserving the original room geometry, camera angle, walls, windows, and major architectural structure.",
                LightingOptions = new List<string> { "natural daylight", "soft ambient ceiling lighting", "subtle accent lights" },
                MaterialOptions = new List<string> { "matte painted walls", "light wood", "stone or marble accents", "fabric upholstery" },
                ColorRuleOptions = new List<string> { "neutral base palette", "white, gray, beige, black accents", "controlled contrast" },
                AtmosphereOptions = new List<string> { "premium", "calm", "spacious", "realistic" },
                SpecificNegative = "cartoon style, distorted perspective, extra doors or windows, warped furniture, unrealistic colors, people, text, watermark",
                TechnicalSpecs = "High quality architectural visualization, photorealistic, sharp details, realistic shadows, no fisheye distortion."
            },
            new StyleAesthetic
            {
                StyleName = "Minimalist",
                CoreAesthetic = "Minimalist interior design with uncluttered composition, simple furniture, open negative space, and quiet visual hierarchy.",
                BaseStructuralPrompt = "Redesign the uploaded room as a photorealistic minimalist interior while preserving the original room layout, perspective, and structural boundaries.",
                LightingOptions = new List<string> { "soft natural light", "hidden warm lighting", "gentle shadows" },
                MaterialOptions = new List<string> { "smooth painted surfaces", "light oak", "linen", "matte finishes" },
                ColorRuleOptions = new List<string> { "warm white", "soft gray", "light wood", "minimal accent colors" },
                AtmosphereOptions = new List<string> { "clean", "serene", "airy", "organized" },
                SpecificNegative = "clutter, busy decoration, excessive patterns, unrealistic furniture, text, watermark, people",
                TechnicalSpecs = "Photorealistic interior render, realistic lighting, clean composition, accurate scale."
            },
            new StyleAesthetic
            {
                StyleName = "Scandinavian",
                CoreAesthetic = "Scandinavian interior design with cozy simplicity, natural materials, bright surfaces, and practical furniture.",
                BaseStructuralPrompt = "Convert the uploaded room into a photorealistic Scandinavian interior while keeping the original architecture, perspective, and room dimensions.",
                LightingOptions = new List<string> { "bright natural daylight", "warm floor lamps", "soft diffused lighting" },
                MaterialOptions = new List<string> { "pale wood", "white walls", "woven textiles", "ceramic decor", "soft rugs" },
                ColorRuleOptions = new List<string> { "white and warm neutral base", "pale wood tones", "muted pastel accents" },
                AtmosphereOptions = new List<string> { "cozy", "bright", "natural", "welcoming" },
                SpecificNegative = "dark heavy furniture, ornate luxury details, clutter, distorted objects, text, watermark, people",
                TechnicalSpecs = "Photorealistic, high detail, balanced natural lighting, realistic textures."
            }
        };
    }
}
