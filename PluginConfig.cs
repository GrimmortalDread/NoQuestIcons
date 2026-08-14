using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin;

namespace NoQuestIcons;

public sealed class PluginConfig : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public bool Enabled { get; set; } = true;

    // Keyed by MarkerIconId. Populated automatically as icons are encountered in-game;
    // see IconRule and DefaultIconCategories for how entries are seeded.
    public Dictionary<int, IconRule> IconRules { get; set; } = new();

    // Held to temporarily reveal icons whose rule has PeekReveals = true.
    // NO_KEY means peek is unbound/disabled.
    public VirtualKey PeekKey { get; set; } = VirtualKey.NO_KEY;

    // Pressed once to turn the whole plugin on or off. NO_KEY means unbound.
    public VirtualKey ToggleKey { get; set; } = VirtualKey.NO_KEY;

    private IDalamudPluginInterface? pi;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pi = pluginInterface;
    }

    public void Save()
    {
        this.pi?.SavePluginConfig(this);
    }
}
