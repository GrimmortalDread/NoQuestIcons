using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
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

    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;

    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private readonly WindowSystem windowSystem;
    private readonly SettingsWindow settings;
    private readonly PluginConfig config;
    private readonly Action openConfigHandler;

    // Updated once per game tick in OnFrameworkUpdate, then just read (not recomputed) inside
    // OnIconUpdate - see the comments on Framework.Update below for why hotkey polling lives
    // there instead of in the nameplate update handler.
    private bool wasPeeking;
    private bool wasToggleKeyHeld;

    public Plugin()
    {
        this.config = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
        this.config.Initialize(PluginInterface);

        // Icons discovered in an earlier session, before their ID was added to
        // DefaultIconCategories, would otherwise be stuck as Uncategorized forever - the
        // per-discovery pre-fill in OnIconUpdate only runs the first time an icon is ever
        // seen. This catches existing rules up once on load, without touching anything the
        // player has already categorized or hidden/unhidden on purpose.
        ApplyKnownDefaultsToExistingRules(this.config);

        Log.Info("[NoQuestIcons] Config loaded. Enabled = {0}", this.config.Enabled);

        this.windowSystem = new WindowSystem("NoQuestIcons");
        this.settings = new SettingsWindow(this.config, KeyState, TextureProvider);
        this.windowSystem.AddWindow(this.settings);

        PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        this.openConfigHandler = () => this.settings.IsOpen = true;
        PluginInterface.UiBuilder.OpenConfigUi += this.openConfigHandler;

        CommandManager.AddHandler("/noquesticons", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the NoQuestIcons settings window (or use the shorter /nqi). Use 'on' or 'off' to enable/disable without opening the window.",
        });
        CommandManager.AddHandler("/nqi", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Alias for /noquesticons.",
            ShowInHelp = false,
        });

        // Framework.Update fires every single game tick, unconditionally - unlike
        // NamePlateGui.OnDataUpdate, which despite its own doc comment isn't reliably called
        // every frame when nothing nameplate-related is happening (e.g. standing still). A
        // held key (peek) tolerates that fine, but a quick tap (the toggle key) can land
        // entirely between two sparse OnDataUpdate calls and simply never register. Hotkey
        // polling lives here specifically so it can't be affected by that.
        Framework.Update += this.OnFrameworkUpdate;

        NamePlateGui.OnDataUpdate += this.OnIconUpdate;
    }

    public void Dispose()
    {
        NamePlateGui.OnDataUpdate -= this.OnIconUpdate;
        Framework.Update -= this.OnFrameworkUpdate;

        CommandManager.RemoveHandler("/noquesticons");
        CommandManager.RemoveHandler("/nqi");

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

    private static void ApplyKnownDefaultsToExistingRules(PluginConfig config)
    {
        var changed = false;

        foreach (var (iconId, rule) in config.IconRules)
        {
            if (rule.Category != IconCategory.Uncategorized)
                continue; // already sorted, either by us or by the player - leave it alone

            if (DefaultIconCategories.Categories.TryGetValue(iconId, out var defaultCategory))
            {
                rule.Category = defaultCategory;
                changed = true;
            }
            else if (DefaultIconCategories.KnownNonQuestIcons.Contains(iconId))
            {
                // Not a quest type at all, so it doesn't belong in "Uncategorized" (which
                // should only mean "this one genuinely needs sorting") - Other fits better.
                rule.Category = IconCategory.Other;
                if (rule.Hidden)
                    rule.Hidden = false;
                changed = true;
            }
        }

        if (changed)
            config.Save();
    }

    private void OnCommand(string command, string arguments)
    {
        var trimmed = arguments.Trim();

        if (string.Equals(trimmed, "on", StringComparison.OrdinalIgnoreCase))
        {
            this.config.Enabled = true;
            this.config.Save();
            NamePlateGui.RequestRedraw();
            return;
        }

        if (string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase))
        {
            this.config.Enabled = false;
            this.config.Save();
            NamePlateGui.RequestRedraw();
            return;
        }

        this.settings.IsOpen = !this.settings.IsOpen;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Checked before the Enabled guard in OnIconUpdate ever runs, so the toggle key can
        // turn the plugin back ON, not just off. Edge-triggered (true this tick, wasn't last
        // tick) so holding it down doesn't rapid-toggle.
        var toggleKeyHeld = this.config.ToggleKey != VirtualKey.NO_KEY && KeyState[this.config.ToggleKey];
        if (toggleKeyHeld && !this.wasToggleKeyHeld)
        {
            this.config.Enabled = !this.config.Enabled;
            this.config.Save();
            NamePlateGui.RequestRedraw();
            Log.Info("[NoQuestIcons] Toggle key pressed, Enabled = {0}, requested redraw.", this.config.Enabled);
        }

        this.wasToggleKeyHeld = toggleKeyHeld;

        var peeking = this.config.PeekKey != VirtualKey.NO_KEY && KeyState[this.config.PeekKey];
        if (peeking != this.wasPeeking)
        {
            this.wasPeeking = peeking;
            NamePlateGui.RequestRedraw();
            Log.Info("[NoQuestIcons] Peek state changed to {0}, requested redraw.", peeking);
        }
    }

    private void OnIconUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!this.config.Enabled)
            return;

        var rulesChanged = false;

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

            var iconId = handler.MarkerIconId;
            if (iconId == 0)
                continue;

            // Every distinct icon ID gets its own rule, discovered the first time it's seen.
            // Known icon IDs are pre-filled from DefaultIconCategories so common quest types
            // don't require manual sorting; anything not in that list still falls back to
            // uncategorized/hidden, ready to be sorted from the Advanced tab.
            if (!this.config.IconRules.TryGetValue(iconId, out var rule))
            {
                rule = new IconRule();

                if (DefaultIconCategories.Categories.TryGetValue(iconId, out var defaultCategory))
                {
                    rule.Category = defaultCategory;
                }
                else if (DefaultIconCategories.KnownNonQuestIcons.Contains(iconId))
                {
                    rule.Category = IconCategory.Other;
                    rule.Hidden = false;
                }

                this.config.IconRules[iconId] = rule;
                rulesChanged = true;

                // Logged so new, not-yet-known icon IDs can be matched to quest types by
                // testing in-game and reading back through the log.
                Log.Info("[NoQuestIcons] Discovered new icon {0} on '{1}'", iconId, handler.Name);
            }

            // Hidden icons stay hidden even while peeking unless this specific rule opted
            // into being revealed by peek (PeekReveals, true by default). this.wasPeeking is
            // kept current by OnFrameworkUpdate, which runs every tick regardless of whether
            // this handler does.
            var shouldHide = rule.Hidden && !(this.wasPeeking && rule.PeekReveals);
            if (shouldHide)
                handler.MarkerIconId = 0;
        }

        // Saving every frame a new icon shows up would be wasteful, but new icon IDs should
        // be rare after the first few minutes of play, so this only fires when the rule set
        // actually grew.
        if (rulesChanged)
            this.config.Save();
    }
}
