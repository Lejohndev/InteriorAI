using System.Text;
using InteriorAI.Data;
using InteriorAI.Data.Seed;
using InteriorAI.Domain.Entities;
using InteriorAI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InteriorAI.Services;

public interface IDesignPromptService
{
    Task<List<DesignStyleResponse>> GetDesignStylesAsync();
    Task<string> GetConfiguredPromptAsync(int? styleId, string? styleName, string? legacyStyle, string? roomType = null);
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

    public async Task<List<DesignStyleResponse>> GetDesignStylesAsync()
    {
        var supportedStyleNames = DesignStyleSeedData.SupportedStyleNames;

        return await _context.StyleAesthetics
            .AsNoTracking()
            .Where(style => supportedStyleNames.Contains(style.StyleName))
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

    public async Task<string> GetConfiguredPromptAsync(
        int? styleId,
        string? styleName,
        string? legacyStyle,
        string? roomType = null)
    {
        var styleKey = FirstNotEmpty(styleName, legacyStyle);
        if (styleId == null && string.IsNullOrWhiteSpace(styleKey))
        {
            throw new ArgumentException("Design styleId or styleName is required.");
        }

        var styleConfig = await FindStyleAsync(styleId, styleKey);
        if (styleConfig == null)
        {
            var requestedStyle = styleId?.ToString() ?? styleKey;
            throw new KeyNotFoundException($"Design style '{requestedStyle}' was not found. Use an existing styleId or styleName.");
        }

        var roomTypeKey = NormalizeRoomTypeKey(roomType);
        if (!string.IsNullOrWhiteSpace(roomTypeKey))
        {
            var roomPrompt = await _context.RoomStylePrompts
                .AsNoTracking()
                .FirstOrDefaultAsync(prompt =>
                    prompt.StyleId == styleConfig.StyleID &&
                    prompt.RoomTypeKey == roomTypeKey);

            if (roomPrompt != null)
            {
                return BuildRoomPrompt(styleConfig, roomPrompt);
            }

            _logger.LogInformation(
                "No room-specific prompt found for style {StyleId} and roomType {RoomTypeKey}. Falling back to style-only prompt.",
                styleConfig.StyleID,
                roomTypeKey);
        }

        var prompt = BuildPrompt(styleConfig);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException($"Design style '{styleConfig.StyleName}' does not have a configured prompt.");
        }

        return prompt;
    }

    private async Task<StyleAesthetic?> FindStyleAsync(int? styleId, string? styleKey)
    {
        StyleAesthetic? styleConfig = null;
        if (styleId != null)
        {
            styleConfig = await _context.StyleAesthetics
                .AsNoTracking()
                .FirstOrDefaultAsync(style => style.StyleID == styleId.Value);
        }

        if (styleConfig != null || string.IsNullOrWhiteSpace(styleKey))
        {
            return styleConfig;
        }

        styleKey = styleKey.Trim();
        if (int.TryParse(styleKey, out var parsedStyleId))
        {
            styleConfig = await _context.StyleAesthetics
                .AsNoTracking()
                .FirstOrDefaultAsync(style => style.StyleID == parsedStyleId);
        }

        return styleConfig ?? await _context.StyleAesthetics
            .AsNoTracking()
            .FirstOrDefaultAsync(style => style.StyleName == styleKey);
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? NormalizeRoomTypeKey(string? roomType)
    {
        if (string.IsNullOrWhiteSpace(roomType))
        {
            return null;
        }

        var normalized = roomType.Trim().ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ");

        return normalized switch
        {
            "living room" => "living_room",
            "bedroom" or "master bedroom" or "master" => "master_bedroom",
            "kitchen" => "kitchen",
            "dining room" => "dining_room",
            "bathroom" => "bathroom",
            "study room" or "office" => "study_room",
            "kids room" or "kid room" or "children room" => "kids_room",
            "walk in closet" or "walk-in closet" or "closet" => "walk_in_closet",
            "hallway" => "hallway",
            "guest room" => "guest_room",
            _ => normalized.Replace(" ", "_")
        };
    }

    private static string BuildRoomPrompt(StyleAesthetic style, RoomStylePrompt roomPrompt)
    {
        var styleCore = style.StyleName switch
        {
            "Japandi" => "Japandi (Japanese-Scandinavian fusion)",
            "Tropical" => "Tropical",
            _ => style.CoreAesthetic
        };

        const string styleBase = "The interior space";

        return $"A photorealistic architectural interior photography of a {roomPrompt.RoomTypeName}. " +
               $"{styleBase} completely redesigned in a residential {styleCore}. " +
               $"The scene features {roomPrompt.Lighting}. " +
               $"Materials strictly limited to {roomPrompt.Material}. " +
               $"Color grading follows a strict rule: {roomPrompt.Color}. " +
               $"Key elements include {roomPrompt.Furniture}. " +
               $"The atmosphere is {roomPrompt.Atmosphere}. " +
               "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k.";
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
}
