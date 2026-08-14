using System.Collections.Generic;

namespace NoQuestIcons;

/// <summary>
/// A starter set of MarkerIconId values collected and verified through actual in-game testing,
/// not guessed. New icons are checked against this data the moment they're first discovered,
/// so common quest types (and known non-quest icons, like shop restock notifications) come
/// pre-sorted instead of requiring the player to manually categorize every single icon by hand.
/// </summary>
/// <remarks>
/// This is necessarily incomplete - Square Enix doesn't publish these IDs anywhere, and they
/// aren't guaranteed to stay the same across patches, so icon IDs not listed here (a category
/// never tested, like Seasonal Event, or an ID that changes later) will still show up as
/// Uncategorized. When that happens the icon works exactly as it always has: hidden by
/// default, ready to be sorted manually from the Advanced tab.
/// </remarks>
public static class DefaultIconCategories
{
    /// <summary>
    /// Known quest-marker icon IDs and the category each one was confirmed to belong to.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, IconCategory> Categories = new Dictionary<int, IconCategory>
    {
        [71203] = IconCategory.MainScenario,   // Aeglyffe
        [70995] = IconCategory.SideQuest,      // Thon Sul (Il Mheg) - confirmed by testing, not MSQ
        [71221] = IconCategory.SideQuest,      // Eral
        [71241] = IconCategory.Guildleve,      // Gontrant
        [71255] = IconCategory.Guildleve,      // Roarich
        [71321] = IconCategory.FeatureQuest,   // The Smith
        [71341] = IconCategory.SideStory,      // Unquiet Trader
        [71355] = IconCategory.SideStory,      // Royse
        [70991] = IconCategory.ClassJobQuest,  // Gods' Quiver Bow
        [71351] = IconCategory.ClassJobQuest,  // Kupopo
        [71353] = IconCategory.ClassJobQuest,  // Radovan
        [71222] = IconCategory.AlliedSociety,  // Tonaxia
        [71301] = IconCategory.TripleTriad,    // Gyuf Uin
        [60093] = IconCategory.Fate,           // Vexed Researcher (Sheaves on the Wind FATE, Labyrinthos)
    };

    /// <summary>
    /// Icon IDs confirmed to be something other than a quest marker - e.g. "this shop has new
    /// stock for your scrips/gemstones" - so they default to visible instead of hidden the way
    /// actual quest icons do.
    /// </summary>
    public static readonly IReadOnlySet<int> KnownNonQuestIcons = new HashSet<int>
    {
        60091, // Aisara - Bicolor Gemstone vendor
        61391, // Rowena's Representative - gear vendor
    };
}
