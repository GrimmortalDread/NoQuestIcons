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

        // OnDataUpdate fires every frame for every visible nameplate, so it's the only
        // subscription needed. OnNamePlateUpdate fires at the same point but only when a
        // conditional internal flag is set, making it a subset of OnDataUpdate, and the
        // "Post" variants fire after the nameplate has already been updated, which is too
        // late to be useful here. The earlier flicker this plugin saw wasn't caused by
        // missing update passes; it was MarkerIconId being cleared unconditionally for
        // nameplate kinds it shouldn't have touched (see OnIconUpdate below).
        NamePlateGui.OnDataUpdate += this.OnIconUpdate;
    }

    public void Dispose()
    {
        NamePlateGui.OnDataUpdate -= this.OnIconUpdate;

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
            // MarkerIconId is the large icon above a nameplate and isn't exclusive to quest
            // availability - it's also used for target markers (1, 2, 3, ...), hunt marks,
            // and other indicators, so clearing it unconditionally stomps on those too.
            // NamePlateKind.EventNpcCompanion covers EventNpc (quest givers, vendors, etc.)
            // and Companion objects, which is the group quest icons actually render on, so
            // gating on it here leaves markers on players, enemies, friendly battle NPCs,
            // retainers, treasure, and gathering points untouched.
            if (handler.NamePlateKind != NamePlateKind.EventNpcCompanion)
                continue;

            handler.MarkerIconId = 0;
        }
    }
}
