namespace NoQuestIcons;

/// <summary>
/// The quest/marker categories a discovered <see cref="IconRule"/> can be grouped under.
/// This list is a starting point based on the quest types that render a nameplate marker -
/// it deliberately does not include FATEs or Custom Deliveries, since both are shown as
/// map/minimap icons rather than nameplate markers, so this plugin can't affect them.
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
        IconCategory.SeasonalEvent => "Seasonal Event",
        IconCategory.Other => "Other",
        _ => category.ToString(),
    };
}
