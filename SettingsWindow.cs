using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace NoQuestIcons;

internal sealed class SettingsWindow : Window
{
    private readonly PluginConfig config;

    public SettingsWindow(PluginConfig config)
        : base("NoQuestIcons Settings")
    {
        this.config = config;

        this.Size = new Vector2(500, 300);
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 300),
            MaximumSize = new Vector2(900, 700),
        };
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("NoQuestIconsTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                var enabled = this.config.Enabled;
                if (ImGui.Checkbox(" Hide quest icons on all nameplates", ref enabled))
                {
                    this.config.Enabled = enabled;
                    this.config.Save();
                }

                ImGui.TextWrapped("Toggle whether quest icons (diamonds, exclamation/question marks) should be hidden above NPC nameplates.");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Advanced"))
            {
                ImGui.TextWrapped("Advanced settings can be added here in the future.");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("About"))
            {
                ImGui.Text("NoQuestIcons Plugin");
                ImGui.Text("Version 1.3.0");
                ImGui.Separator();
                ImGui.TextWrapped("This plugin removes quest icons from NPC nameplates.\nPurely client-side, safe to use, no game files modified.");
                ImGui.TextDisabled("Created for Dalamud API 15");
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}
