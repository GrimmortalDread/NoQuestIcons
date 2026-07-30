using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
    private readonly List<(EventInfo evt, Delegate del)> attachments = new();
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

        this.TryAttachAllCompatibleNamePlateEvents();
    }

    public void Dispose()
    {
        this.TryDetachAll();

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

    /// <summary>
    /// Binds to every compatible NamePlate update event on the currently running Dalamud
    /// build via reflection (rather than a single hard-coded event), since the game only
    /// resets the marker icon on the frames some of these events fire, and binding to just
    /// one of them is not sufficient to reliably suppress the icon without flicker.
    /// </summary>
    private void TryAttachAllCompatibleNamePlateEvents()
    {
        var events = NamePlateGui.GetType().GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        for (var i = 0; i < events.Length; i++)
        {
            var evt = events[i];
            var handlerType = evt.EventHandlerType;
            if (handlerType is null)
                continue;

            try
            {
                var invoke = handlerType.GetMethod("Invoke");
                if (invoke is null)
                    continue;

                if (invoke.ReturnType != typeof(void))
                    continue;

                var parameters = invoke.GetParameters();
                if (parameters.Length < 2 || parameters.Length > 3)
                    continue;

                var shim = parameters.Length == 2
                    ? typeof(Plugin).GetMethod(nameof(PlateUpdateShim2), BindingFlags.NonPublic | BindingFlags.Instance)
                    : typeof(Plugin).GetMethod(nameof(PlateUpdateShim3), BindingFlags.NonPublic | BindingFlags.Instance);
                if (shim is null)
                    continue;

                var del = Delegate.CreateDelegate(handlerType, this, shim, false);
                if (del is null)
                    continue;

                evt.AddEventHandler(NamePlateGui, del);
                this.attachments.Add((evt, del));
                Log.Info("[NoQuestIcons] Attached to event {0}.", evt.Name);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[NoQuestIcons] Failed to attach to event {0}.", evt.Name);
            }
        }

        if (this.attachments.Count == 0)
        {
            Log.Error("[NoQuestIcons] No compatible NamePlate events found. Quest icons may still render.");
        }
    }

    private void TryDetachAll()
    {
        foreach (var (evt, del) in this.attachments)
        {
            try
            {
                evt.RemoveEventHandler(NamePlateGui, del);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[NoQuestIcons] Failed to detach from {0}.", evt.Name);
            }
        }

        this.attachments.Clear();
    }

    // Matches Dalamud's current INamePlateGui.OnPlateUpdateDelegate(context, handlers) shape.
    private void PlateUpdateShim2(object arg1, object handlersObj)
    {
        this.ZeroIcons(handlersObj);
    }

    // Fallback for any 3-parameter nameplate update delegate shape.
    private void PlateUpdateShim3(object arg1, object arg2, object handlersObj)
    {
        this.ZeroIcons(handlersObj);
    }

    private void ZeroIcons(object handlersObj)
    {
        if (!this.config.Enabled)
            return;

        if (handlersObj is not IEnumerable handlers)
            return;

        foreach (var handler in handlers)
        {
            if (handler is null)
                continue;

            try
            {
                var type = handler.GetType();

                var markerIconProp = type.GetProperty("MarkerIconId");
                if (markerIconProp is not null && markerIconProp.CanWrite)
                    markerIconProp.SetValue(handler, 0);

                var nameIconProp = type.GetProperty("NameIconId");
                if (nameIconProp is not null && nameIconProp.CanWrite)
                    nameIconProp.SetValue(handler, 0);

                var questIconProp = type.GetProperty("QuestIconId");
                if (questIconProp is not null && questIconProp.CanWrite)
                    questIconProp.SetValue(handler, 0);

                var markerVisibleProp = type.GetProperty("MarkerVisible");
                if (markerVisibleProp is not null && markerVisibleProp.CanWrite)
                    markerVisibleProp.SetValue(handler, false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[NoQuestIcons] Error while clearing icon properties.");
            }
        }
    }
}
