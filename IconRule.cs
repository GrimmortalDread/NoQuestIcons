namespace NoQuestIcons;

/// <summary>
/// A per-icon rule, keyed in <see cref="PluginConfig.IconRules"/> by the game's MarkerIconId.
/// Entries are created automatically the first time an icon is seen above a quest-giver
/// nameplate. Known icon IDs are pre-filled from <see cref="DefaultIconCategories"/>; anything
/// not in that list falls back to <see cref="IconCategory.Uncategorized"/> and hidden, so the
/// player can sort it manually from the Advanced tab.
/// </summary>
public sealed class IconRule
{
    public IconCategory Category { get; set; } = IconCategory.Uncategorized;

    public bool Hidden { get; set; } = true;

    /// <summary>
    /// Whether holding the peek key should reveal this icon while it's hidden. Defaults to
    /// true so peek behaves as "show everything" out of the box; set to false on specific
    /// icons/categories the player wants to stay hidden even while peeking.
    /// </summary>
    public bool PeekReveals { get; set; } = true;
}
