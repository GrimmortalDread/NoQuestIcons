using System;
using System.Collections.Generic;

using Dalamud.Game.Gui.NamePlate;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace NoQuestIcons;

public sealed class Plugin : IDalamudPlugin, IDisposable
{
    public string Name => "NoQuestIcons";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static INamePlateGui NamePlateGui { get; private set; } = null!;

    private readonly WindowSystem windowSystem;
    private readonly SettingsWindow settings;
    private readonly PluginConfig config;
    private readonly Action openConfigHandler;

    public Plugin()
    {
        this.config = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
        this.config.Initialize(PluginInterface);
        Log.Info("[NoQuestIcons] Config loaded. Enabled = {0}", this.config.Enabled);

        this.windowSystem = new WindowSystem("NoQuestIcons");
        this.settings = new SettingsWindow(this.config);
        this.windowSystem.AddWindow(this.settings);

        PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        this.openConfigHandler = () => this.settings.IsOpen = true;
        PluginInterface.UiBuilder.OpenConfigUi += this.openConfigHandler;

        // Subscribed to all four NamePlate update events (not just OnDataUpdate) because
        // MarkerIconId is reset by the game at multiple points in the update cycle, and
        // zeroing it from a single event was observed to still flicker. Hitting it on every
        // available update/post-update pass closes that gap. This is intentionally explicit
        // and typed rather than reflection-based, so it's easy to audit and doesn't depend on
        // discovering event members at runtime.
        NamePlateGui.OnDataUpdate += this.OnIconUpdate;
        NamePlateGui.OnPostDataUpdate += this.OnIconUpdate;
        NamePlateGui.OnNamePlateUpdate += this.OnIconUpdate;
        NamePlateGui.OnPostNamePlateUpdate += this.OnIconUpdate;
    }

    public void Dispose()
    {
        NamePlateGui.OnDataUpdate -= this.OnIconUpdate;
        NamePlateGui.OnPostDataUpdate -= this.OnIconUpdate;
        NamePlateGui.OnNamePlateUpdate -= this.OnIconUpdate;
        NamePlateGui.OnPostNamePlateUpdate -= this.OnIconUpdate;

        PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= this.openConfigHandler;
        this.windowSystem.RemoveAllWindows();

        try
        {
            PluginInterface.SavePluginConfig(this.config);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[NoQuestIcons] Failed to save config on dispose.");
        }
    }

    private void OnIconUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!this.config.Enabled)
            return;

        foreach (var handler in handlers)
        {
            // MarkerIconId is the large icon above NPC heads used for quest availability
            // (and a few other markers, like hunt/FATE indicators). Setting it to 0 disables it.
            handler.MarkerIconId = 0;
        }
    }
}
