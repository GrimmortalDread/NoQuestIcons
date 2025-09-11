using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace NoQuestIcons
{
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
            // Config
            config = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            config.Initialize(PluginInterface);
            Log.Info("[NoQuestIcons] Config loaded. Enabled = {0}", config.Enabled);

            // UI
            windowSystem = new WindowSystem("NoQuestIcons");
            settings = new SettingsWindow(config);
            windowSystem.AddWindow(settings);

            PluginInterface.UiBuilder.Draw += windowSystem.Draw;
            openConfigHandler = () => settings.IsOpen = true;
            PluginInterface.UiBuilder.OpenConfigUi += openConfigHandler;

            // Hook nameplate events
            TryAttachAllCompatibleNamePlateEvents();
        }

        public void Dispose()
        {
            TryDetachAll();

            PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= openConfigHandler;
            windowSystem.RemoveAllWindows();

            try { PluginInterface.SavePluginConfig(config); }
            catch (Exception ex) { Log.Error(ex, "[NoQuestIcons] Failed to save config on dispose."); }
        }

        // ------------------------------------------------------------
        // Reflection Event Hook
        // ------------------------------------------------------------
        private void TryAttachAllCompatibleNamePlateEvents()
        {
            var t = NamePlateGui.GetType();
            var events = t.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var evt in events)
            {
                try
                {
                    var delType = evt.EventHandlerType;
                    if (delType == null) continue;

                    var invoke = delType.GetMethod("Invoke");
                    if (invoke == null || invoke.ReturnType != typeof(void)) continue;

                    var parms = invoke.GetParameters();
                    if (parms.Length < 2 || parms.Length > 3) continue;

                    var shimMethod = parms.Length == 2
                        ? typeof(Plugin).GetMethod(nameof(PlateUpdateShim2), BindingFlags.NonPublic | BindingFlags.Instance)
                        : typeof(Plugin).GetMethod(nameof(PlateUpdateShim3), BindingFlags.NonPublic | BindingFlags.Instance);

                    if (shimMethod == null) continue;

                    var del = Delegate.CreateDelegate(delType, this, shimMethod, false);
                    if (del == null) continue;

                    evt.AddEventHandler(NamePlateGui, del);
                    attachments.Add((evt, del));
                    Log.Info("[NoQuestIcons] Attached to event {0}.", evt.Name);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[NoQuestIcons] Failed to attach to event {0}.", evt.Name);
                }
            }

            if (attachments.Count == 0)
            {
                Log.Error("[NoQuestIcons] No compatible NamePlate events found. Quest icons may still render.");
            }
        }

        private void TryDetachAll()
        {
            foreach (var (evt, del) in attachments)
            {
                try { evt.RemoveEventHandler(NamePlateGui, del); }
                catch (Exception ex) { Log.Warning(ex, "[NoQuestIcons] Failed to detach from {0}.", evt.Name); }
            }
            attachments.Clear();
        }

        private void PlateUpdateShim2(object? arg1, object? handlersObj) => ZeroIcons(handlersObj);
        private void PlateUpdateShim3(object? arg1, object? arg2, object? handlersObj) => ZeroIcons(handlersObj);

        private void ZeroIcons(object? handlersObj)
        {
            if (!config.Enabled || handlersObj is not IEnumerable enumerable) return;

            foreach (var item in enumerable)
            {
                if (item == null) continue;
                try
                {
                    var t = item.GetType();

                    var marker = t.GetProperty("MarkerIconId");
                    if (marker != null && marker.CanWrite) marker.SetValue(item, 0);

                    var nameIcon = t.GetProperty("NameIconId");
                    if (nameIcon != null && nameIcon.CanWrite) nameIcon.SetValue(item, 0);

                    var questIcon = t.GetProperty("QuestIconId");
                    if (questIcon != null && questIcon.CanWrite) questIcon.SetValue(item, 0);

                    var visible = t.GetProperty("MarkerVisible");
                    if (visible != null && visible.CanWrite) visible.SetValue(item, false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[NoQuestIcons] Error while clearing icon properties.");
                }
            }
        }
    }

    // ------------------------------------------------------------
    // Config
    // ------------------------------------------------------------
    [Serializable]
    public sealed class PluginConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        [NonSerialized] private IDalamudPluginInterface? pi;

        public void Initialize(IDalamudPluginInterface pluginInterface) => pi = pluginInterface;
        public void Save() => pi?.SavePluginConfig(this);
    }

    // ------------------------------------------------------------
    // Settings Window
    // ------------------------------------------------------------
    public sealed class SettingsWindow : Window
    {
        private readonly PluginConfig config;

        public SettingsWindow(PluginConfig config)
            : base("NoQuestIcons Settings", ImGuiWindowFlags.None)
        {
            this.config = config;
            Size = new Vector2(500, 300);
            SizeConstraints = new WindowSizeConstraints
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
                    var enabled = config.Enabled;
                    if (ImGui.Checkbox(" Hide quest icons on all nameplates", ref enabled))
                    {
                        config.Enabled = enabled;
                        config.Save();
                    }

                    ImGui.TextWrapped("Toggle whether quest icons (diamonds, exclamation/question marks) "
                                    + "should be hidden above NPC nameplates.");
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
                    ImGui.Text("Version 1.0.0");
                    ImGui.Separator();
                    ImGui.TextWrapped("This plugin removes quest icons from NPC nameplates.\n"
                                    + "Purely client-side, safe to use, no game files modified.");
                    ImGui.TextDisabled("Created for Dalamud API 13.1.0");
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
    }
}
