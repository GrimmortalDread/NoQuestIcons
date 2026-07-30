using Dalamud.Configuration;
using Dalamud.Plugin;

namespace NoQuestIcons;

public sealed class PluginConfig : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

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
