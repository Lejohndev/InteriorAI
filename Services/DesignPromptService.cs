using System.Text;
using SmartFormat;
using InteriorAI.Data;
using InteriorAI.Data.Seed;
using InteriorAI.Domain.Entities;
using InteriorAI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InteriorAI.Services;

public interface IDesignPromptService
{
    Task<List<DesignStyleResponse>> GetDesignStylesAsync();
    Task<string> GetConfiguredPromptAsync(
        int? styleId,
        string? styleName,
        string? legacyStyle,
        string? roomType = null,
        string? featureId = null);
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
                CoreAesthetic = style.CoreAesthetic
            })
            .ToListAsync();
    }

    public async Task<string> GetConfiguredPromptAsync(
        int? styleId,
        string? styleName,
        string? legacyStyle,
        string? roomType = null,
        string? featureId = null)
    {
        var styleKey = FirstNotEmpty(styleName, legacyStyle);
        var normalizedFeatureId = NormalizeFeatureId(featureId);
        var roomTypeKey = NormalizeRoomTypeKey(roomType);

        if (normalizedFeatureId == "remove_furniture")
        {
            var removePrompt  = BuildRemoveFurniturePrompt(GetFallbackRoomTypeName(roomTypeKey));
            _logger.LogInformation(
                "REMOVE_FURNITURE_FINAL_PROMPT: {Prompt}",
                removePrompt);
            return removePrompt;
        }

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

        if (!string.IsNullOrWhiteSpace(roomTypeKey))
        {
            var roomPrompt = await _context.RoomStylePrompts
                .AsNoTracking()
                .FirstOrDefaultAsync(rp =>
                    rp.StyleId == styleConfig.StyleID &&
                    rp.RoomTypeKey == roomTypeKey);

            if (roomPrompt != null)
            {
                return normalizedFeatureId switch
                {
                    "furnish_empty_room" => BuildFurnishEmptyRoomPrompt(styleConfig, roomPrompt),
                    _ => BuildRoomPrompt(styleConfig, roomPrompt)
                };
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

    private static string NormalizeFeatureId(string? featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            return "interior_design";
        }

        var normalized = featureId.Trim().ToLowerInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");

        return normalized switch
        {
            "interior_design" => "interior_design",
            "furnish_empty_room" or "furnish_room" or "empty_room" => "furnish_empty_room",
            "remove_furniture" or "clear_room" or "empty_existing_room" => "remove_furniture",
            _ => "interior_design"
        };
    }

    private static string GetFallbackRoomTypeName(string? roomTypeKey)
    {
        return roomTypeKey switch
        {
            "living_room" => "Living Room",
            "master_bedroom" => "Master Bedroom",
            "kitchen" => "Kitchen",
            "dining_room" => "Dining Room",
            "bathroom" => "Bathroom",
            "study_room" => "Study Room",
            "kids_room" => "Kids Room",
            "walk_in_closet" => "Walk-in Closet",
            _ => "room"
        };
    }

private static string BuildRoomPrompt(StyleAesthetic style, RoomStylePrompt roomPrompt)
{
    var prompt = Smart.Format(roomPrompt.PromptTemplate, roomPrompt);

    prompt +=
        " Treat the uploaded image as an existing furnished or partially furnished room when furniture is present. " +
        "Preserve the original architecture, camera angle, walls, floor, ceiling, windows, doors, built-in fixtures, and room geometry. " +

        "Replace all existing furniture with redesigned furniture matching the selected style. " +
        "Restyle, upgrade, and transform existing furniture instead of adding additional furniture. " +

        "Do NOT duplicate furniture. " +
        "Do NOT create multiple versions of the same object. " +
        "Do NOT place new furniture on top of existing furniture. " +

        "If a sofa already exists, replace and redesign that sofa. " +
        "If a bed already exists, replace and redesign that bed. " +
        "If a table already exists, replace and redesign that table. " +

        "Maintain realistic furniture count and room balance. " +
        "The final room should contain only one coherent furniture layout matching the selected style. " +
        "Remove outdated furniture concepts and replace them with a single consistent furniture composition. ";
    return AppendAvoidClause(prompt, roomPrompt.SpecificNegative);
}

    private static string BuildFurnishEmptyRoomPrompt(StyleAesthetic style, RoomStylePrompt roomPrompt)
    {
        var styleCore = style.StyleName switch
        {
            "Japandi" => "Japandi (Japanese-Scandinavian fusion)",
            "Tropical" => "Tropical",
            _ => style.CoreAesthetic
        };

        var prompt = $"A photorealistic architectural interior photography of an empty or mostly empty {roomPrompt.RoomTypeName}. " +
                     $"Furnish the space from scratch as a residential {styleCore} while preserving the original architecture, camera angle, walls, floor, ceiling, windows, doors, and room geometry. " +
                     $"The scene features {roomPrompt.Lighting}. " +
                     $"Materials strictly limited to {roomPrompt.Material}. " +
                     $"Color grading follows a strict rule: {roomPrompt.Color}. " +
                     $"Add suitable furniture and decor for this room type, including {roomPrompt.Furniture}. " +
                     $"The atmosphere is {roomPrompt.Atmosphere}. " +
                     "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k.";

        return AppendAvoidClause(prompt, roomPrompt.SpecificNegative);
    }

    private static string BuildRemoveFurniturePrompt(string roomTypeName)
    {
        var prompt = $"A photorealistic architectural image-editing result of a completely empty, vacant, unfurnished {roomTypeName}. " +
                     "REMOVE ALL EXISTING FURNITURE, DECOR, AND MOVABLE OBJECTS: sofas, couches, sectionals, beds, mattresses, armchairs, daybeds, chairs, stools, benches, tables, coffee tables, dining tables, desks, nightstands, dressers, wardrobes, cabinets, shelves, bookcases, TVs, screens, monitors, speakers, laptops, electronics, appliances, lamps, chandeliers, wall sconces, ceiling fans, rugs, carpets, curtains, blinds, plants, flowers, vases, paintings, frames, mirrors, wall art, decorative items, toys, books, personal belongings, and any other movable object. " +
                     "Replace every removed object with accurately reconstructed empty floor, wall, ceiling, or built-in surface. " +
                     "Strip the room to bare architecture only: walls, floor, ceiling, windows, doors, baseboards, moldings, and fixed built-in fixtures. " +
                     "PRESERVE EXACTLY: the original architectural structure, walls, floor, ceiling, baseboards, crown molding, windows, doors, built-in fixtures, camera angle, and room geometry. Do not alter, damage, or modify any architectural elements. " +
                     "RECONSTRUCT HIDDEN AREAS: Realistically fill all floor, wall, and ceiling areas that were covered by removed objects. Match existing floor texture, wall paint color, and ceiling finish exactly. Blend all reconstructed areas seamlessly—no visible seams, warping, color mismatches, or texture discontinuities. " +
                     "FINAL RESULT: A completely empty room, vacant room, and unfurnished room with bare architecture only. No furniture, no decor, no accessories, no artwork, no electronics, no plants, no personal belongings. No traces, shadows, outlines, or ghosts of removed items. " +
                     "Photorealistic, natural room lighting, hyper-detailed, architectural photography, 8k.";

        return AppendRemovalNegativeClause(prompt);
    }

    private static string BuildPrompt(StyleAesthetic style)
    {
        var promptBuilder = new StringBuilder();

        AppendLineIfPresent(promptBuilder, style.CoreAesthetic);
        AppendLineIfPresent(promptBuilder, style.TechnicalSpecs);

        return promptBuilder.ToString().Trim();
    }

    private static string AppendAvoidClause(string prompt, string? specificNegative)
    {
        var trimmedPrompt = prompt.Trim();
        if (string.IsNullOrWhiteSpace(specificNegative))
        {
            return trimmedPrompt;
        }

        return $"{trimmedPrompt} Avoid: {specificNegative.Trim()}";
    }

    private static string AppendRemovalNegativeClause(string prompt)
    {
        // Specific negative clause for remove_furniture feature (NOT interior_design)
        var removalNegatives =
            "Avoid: any furniture, any sofas, any couches, any sectionals, any beds, any mattresses, any chairs, any stools, any benches, " +
            "any tables, any desks, any nightstands, any wardrobes, any cabinets, any shelves, any bookcases, any TVs, any monitors, any electronics, " +
            "any lamps, any chandeliers, any rugs, any carpets, any curtains, any blinds, any decor, any artwork, any paintings, any frames, any mirrors, " +
            "any plants, any flowers, any accessories, any personal belongings, any toys, any books, any clutter, any occupied room appearance. " +
            "The result must remain a completely empty room, vacant room, unfurnished room, bare architectural space only. " +
            "No traces of removed furniture. No furniture shadows. No furniture outlines. No furniture ghosts. " +
            "No staged interior. No styled room. No decorative elements. " +
            "Distorted architecture, warped walls, broken windows, unrealistic reconstruction, unfinished surfaces, text, watermark, people.";
        return $"{prompt.Trim()} {removalNegatives}";
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
