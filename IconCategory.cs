namespace NoQuestIcons;

/// <summary>
/// The quest/marker categories a discovered <see cref="IconRule"/> can be grouped under.
/// This list deliberately does not include Custom Deliveries, since those are shown as
/// map/minimap icons rather than nameplate markers, so this plugin can't affect them. FATEs
/// themselves work the same way (the "available" indicator is map-only) - but NPCs involved
/// in a FATE (an escort target, someone to protect, etc.) do render their own nameplate
/// marker, which is what the Fate category below covers.
/// </summary>
public enum IconCategory
{
    Uncategorized,
    MainScenario,
    FeatureQuest,
    SideStory,
    SideQuest,
    ClassJobQuest,
    AlliedSociety,
    Guildleve,
    TripleTriad,
    Fate,
    SeasonalEvent,
    Other,
}

public static class IconCategoryExtensions
{
    /// <summary>
    /// A human-readable label for display in the settings window. Kept separate from the
    /// enum member name so the UI can use spacing/casing the C# identifier rules don't allow.
    /// </summary>
    public static string GetDisplayName(this IconCategory category) => category switch
    {
        IconCategory.Uncategorized => "Uncategorized",
        IconCategory.MainScenario => "Main Scenario Quest",
        IconCategory.FeatureQuest => "Feature Quest",
        IconCategory.SideStory => "Side Story Quest",
        IconCategory.SideQuest => "Side Quest",
        IconCategory.ClassJobQuest => "Class/Job Quest",
        IconCategory.AlliedSociety => "Allied Society (Beast Tribe)",
        IconCategory.Guildleve => "Guildleve",
        IconCategory.TripleTriad => "Triple Triad",
        IconCategory.Fate => "FATE",
        IconCategory.SeasonalEvent => "Seasonal Event",
        IconCategory.Other => "Other",
        _ => category.ToString(),
    };
}