using System;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace NoQuestIcons;

internal sealed class SettingsWindow : Window
{
    // Sorted alphabetically by display name for the UI - the enum's own declaration order is
    // left untouched, since each member's underlying numeric value is what's actually saved
    // in the player's config. Reordering the enum itself would silently reassign those
    // numbers and scramble everyone's already-categorized icons.
    private static readonly IconCategory[] AssignableCategories = Enum.GetValues<IconCategory>()
        .OrderBy(category => category.GetDisplayName(), StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private readonly PluginConfig config;
    private readonly IKeyState keyState;
    private readonly ITextureProvider textureProvider;

    private readonly VirtualKey[] bindableKeys;

    public SettingsWindow(PluginConfig config, IKeyState keyState, ITextureProvider textureProvider)
        : base("NoQuestIcons Settings")
    {
        // The Advanced tab's content scrolls within its own child region (see Draw()), so
        // the outer window never needs a scrollbar of its own - without this, a tiny mismatch
        // between the outer window's fixed height and its total content can make the outer
        // window grow a second, mostly-nonfunctional scrollbar right next to the real one.
        this.Flags = ImGuiWindowFlags.NoScrollbar;

        this.config = config;
        this.keyState = keyState;
        this.textureProvider = textureProvider;

        this.bindableKeys = this.keyState.GetValidVirtualKeys()
            .Where(key => key != VirtualKey.NO_KEY)
            .OrderBy(key => key.GetFancyName())
            .ToArray();

        this.Size = new Vector2(760, 560);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 450),
            MaximumSize = new Vector2(1600, 1000),
        };
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("NoQuestIconsTabs");
        if (tabBar)
        {
            // Each tab below uses an explicit using (...) { } block, not a plain "using var"
            // line - these three tabs are siblings in the same scope, so a plain "using var"
            // wouldn't close one until the whole block ends, leaving all three open at once
            // instead of each closing before the next begins.
            using (var generalTab = ImRaii.TabItem("General"))
            {
                if (generalTab)
                {
                    var enabled = this.config.Enabled;
                    if (ImGui.Checkbox(" Hide quest icons on all nameplates", ref enabled))
                    {
                        this.config.Enabled = enabled;
                        this.config.Save();
                    }

                    ImGui.TextWrapped("This master switch controls every quest icon at once, overriding whatever's set per category. Use the Advanced tab to fine-tune which quest types are hidden.");
                }
            }

            using (var advancedTab = ImRaii.TabItem("Advanced"))
            {
                if (advancedTab)
                {
                    // One scroll region for the whole tab's content, rather than a separate one
                    // just for the icon table below - avoids two nested scrollbars fighting each
                    // other. The footer is drawn after this child region closes, outside it, so
                    // it stays fixed in place instead of scrolling with everything else.
                    var footerReserve = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;
                    var childHeight = MathF.Max(150f, ImGui.GetContentRegionAvail().Y - footerReserve);

                    using var scrollRegion = ImRaii.Child("NoQuestIconsAdvancedScroll", new Vector2(0, childHeight));
                    if (scrollRegion)
                    {
                        this.DrawPeekKeybind();
                        ImGui.Separator();
                        this.DrawToggleKeybind();
                        ImGui.Separator();
                        this.DrawQuickActions();
                        ImGui.Separator();
                        this.DrawCategorySummary();
                        ImGui.Separator();
                        this.DrawIconRules();
                    }
                }
            }

            using (var aboutTab = ImRaii.TabItem("About"))
            {
                if (aboutTab)
                {
                    ImGui.Text("NoQuestIcons");
                    ImGui.Text($"Version {typeof(Plugin).Assembly.GetName().Version?.ToString(3)}");
                    ImGui.Separator();

                    ImGui.TextWrapped(
                        "Hides every quest icon above NPC nameplates. Icons are sortable and " +
                        "hideable by quest type (Main Scenario, Side Quest, Guildleve, Class/Job " +
                        "Quest, and more), with common types pre-categorized. Includes a peek key " +
                        "to reveal hidden icons on demand and a separate toggle key to switch the " +
                        "whole plugin on/off, both from the Advanced tab.");

                    ImGui.Spacing();
                    ImGui.TextDisabled("Built for Dalamud API 15.");
                }
            }
        }

        this.DrawFooter();
    }

    private void DrawPeekKeybind()
    {
        ImGui.TextWrapped("Hold this key to reveal icons whose \"Peek\" option is checked below, without changing any hide setting.");

        var currentLabel = this.config.PeekKey == VirtualKey.NO_KEY ? "Unbound" : this.config.PeekKey.GetFancyName();
        ImGui.SetNextItemWidth(220);

        using var combo = ImRaii.Combo("Peek key", currentLabel);
        if (combo)
        {
            if (ImGui.Selectable("Unbound", this.config.PeekKey == VirtualKey.NO_KEY))
            {
                this.config.PeekKey = VirtualKey.NO_KEY;
                this.config.Save();
            }

            foreach (var key in this.bindableKeys)
            {
                var selected = key == this.config.PeekKey;
                if (ImGui.Selectable(key.GetFancyName(), selected))
                {
                    this.config.PeekKey = key;
                    this.config.Save();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
        }
    }

    private void DrawToggleKeybind()
    {
        ImGui.TextWrapped("Press this key once to turn the whole plugin on or off, without opening this window.");

        var currentLabel = this.config.ToggleKey == VirtualKey.NO_KEY ? "Unbound" : this.config.ToggleKey.GetFancyName();
        ImGui.SetNextItemWidth(220);

        using var combo = ImRaii.Combo("Toggle plugin key", currentLabel);
        if (combo)
        {
            if (ImGui.Selectable("Unbound", this.config.ToggleKey == VirtualKey.NO_KEY))
            {
                this.config.ToggleKey = VirtualKey.NO_KEY;
                this.config.Save();
            }

            foreach (var key in this.bindableKeys)
            {
                var selected = key == this.config.ToggleKey;
                if (ImGui.Selectable(key.GetFancyName(), selected))
                {
                    this.config.ToggleKey = key;
                    this.config.Save();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
        }
    }

    private void DrawQuickActions()
    {
        if (ImGui.Button("Hide all discovered icons"))
        {
            foreach (var rule in this.config.IconRules.Values)
                rule.Hidden = true;
            this.config.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Show all discovered icons"))
        {
            foreach (var rule in this.config.IconRules.Values)
                rule.Hidden = false;
            this.config.Save();
        }

        if (ImGui.Button("Enable peek for all"))
        {
            foreach (var rule in this.config.IconRules.Values)
                rule.PeekReveals = true;
            this.config.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Disable peek for all"))
        {
            foreach (var rule in this.config.IconRules.Values)
                rule.PeekReveals = false;
            this.config.Save();
        }
    }

    private void DrawCategorySummary()
    {
        ImGui.TextWrapped(
            "Toggle a whole quest type at once here. Common quest types are pre-categorized " +
            "automatically as you play; \"Peek\" controls whether holding the peek key reveals " +
            "icons in that category while they're hidden.");

        // Measuring the actual rendered text/checkbox size (rather than a hardcoded pixel
        // number) means these columns stay correctly sized regardless of the UI scale the
        // player has Dalamud set to - a fixed "70px" can be too narrow on a scaled-up 4K setup.
        var iconsFoundWidth = MathF.Max(ImGui.CalcTextSize("Icons found").X, ImGui.CalcTextSize("999").X) + 20f;
        var hiddenWidth = MathF.Max(ImGui.CalcTextSize("Hidden").X, ImGui.GetFrameHeight()) + 20f;
        var peekWidth = MathF.Max(ImGui.CalcTextSize("Peek").X, ImGui.GetFrameHeight()) + 20f;

        using var table = ImRaii.Table("NoQuestIconsCategorySummary", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (table)
        {
            ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Icons found", ImGuiTableColumnFlags.WidthFixed, iconsFoundWidth);
            ImGui.TableSetupColumn("Hidden", ImGuiTableColumnFlags.WidthFixed, hiddenWidth);
            ImGui.TableSetupColumn("Peek", ImGuiTableColumnFlags.WidthFixed, peekWidth);
            ImGui.TableHeadersRow();

            foreach (var category in AssignableCategories)
            {
                if (category == IconCategory.Uncategorized)
                    continue;

                var rulesInCategory = this.config.IconRules.Values.Where(r => r.Category == category).ToArray();

                ImGui.TableNextRow();

                // Disposes at the end of this loop iteration, same as id below - each row
                // gets its own unique tag and its own disabled-state, then both release
                // automatically before the next row starts.
                using var id = ImRaii.PushId((int)category);

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(category.GetDisplayName());

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(rulesInCategory.Length.ToString());

                var hasIcons = rulesInCategory.Length > 0;
                using var disabled = ImRaii.Disabled(!hasIcons);

                ImGui.TableSetColumnIndex(2);
                var allHidden = hasIcons && rulesInCategory.All(r => r.Hidden);
                if (ImGui.Checkbox("##Hidden", ref allHidden))
                {
                    foreach (var rule in rulesInCategory)
                        rule.Hidden = allHidden;
                    this.config.Save();
                }

                ImGui.TableSetColumnIndex(3);
                var allPeek = hasIcons && rulesInCategory.All(r => r.PeekReveals);
                if (ImGui.Checkbox("##Peek", ref allPeek))
                {
                    foreach (var rule in rulesInCategory)
                        rule.PeekReveals = allPeek;
                    this.config.Save();
                }
            }
        }
    }

    private void DrawIconRules()
    {
        ImGui.TextWrapped(
            "Every quest-marker icon this plugin has seen above a nameplate is listed below. " +
            "Known icons are categorized automatically; anything new starts uncategorized and " +
            "hidden until you assign it a category here, or use the category toggles above.");

        if (this.config.IconRules.Count == 0)
        {
            ImGui.TextDisabled("No quest icons encountered yet - walk past a few quest givers to populate this list.");
            return;
        }

        var iconSize = ImGui.GetFrameHeight();
        var iconIdWidth = MathF.Max(ImGui.CalcTextSize("Icon ID").X, ImGui.CalcTextSize("999999").X) + 20f;
        var hiddenWidth = MathF.Max(ImGui.CalcTextSize("Hidden").X, ImGui.GetFrameHeight()) + 20f;
        var peekWidth = MathF.Max(ImGui.CalcTextSize("Peek").X, ImGui.GetFrameHeight()) + 20f;

        using var table = ImRaii.Table("NoQuestIconsIconRules", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (table)
        {
            ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, iconSize + 16f);
            ImGui.TableSetupColumn("Icon ID", ImGuiTableColumnFlags.WidthFixed, iconIdWidth);
            ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Hidden", ImGuiTableColumnFlags.WidthFixed, hiddenWidth);
            ImGui.TableSetupColumn("Peek", ImGuiTableColumnFlags.WidthFixed, peekWidth);
            ImGui.TableHeadersRow();

            foreach (var (iconId, rule) in this.config.IconRules.OrderBy(entry => entry.Key))
            {
                ImGui.TableNextRow();
                using var id = ImRaii.PushId(iconId);

                ImGui.TableSetColumnIndex(0);
                var texture = this.textureProvider.GetFromGameIcon(new GameIconLookup((uint)iconId)).GetWrapOrEmpty();
                ImGui.Image(texture.Handle, new Vector2(iconSize, iconSize));

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(iconId.ToString());

                ImGui.TableSetColumnIndex(2);
                ImGui.SetNextItemWidth(-1);

                using var combo = ImRaii.Combo("##Category", rule.Category.GetDisplayName());
                if (combo)
                {
                    foreach (var category in AssignableCategories)
                    {
                        var selected = category == rule.Category;
                        if (ImGui.Selectable(category.GetDisplayName(), selected))
                        {
                            rule.Category = category;
                            this.config.Save();
                        }

                        if (selected)
                            ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.TableSetColumnIndex(3);
                var hidden = rule.Hidden;
                if (ImGui.Checkbox("##Hidden", ref hidden))
                {
                    rule.Hidden = hidden;
                    this.config.Save();
                }

                ImGui.TableSetColumnIndex(4);
                var peekReveals = rule.PeekReveals;
                if (ImGui.Checkbox("##Peek", ref peekReveals))
                {
                    rule.PeekReveals = peekReveals;
                    this.config.Save();
                }
            }
        }
    }

    private void DrawFooter()
    {
        const string label = "Support on Ko-fi";
        var buttonSize = new Vector2(
            ImGui.CalcTextSize(label).X + (ImGui.GetStyle().FramePadding.X * 2f),
            ImGui.GetFrameHeight());

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Right-aligned using remaining content width (which already accounts for any
        // active scrollbar or padding), rather than raw window width - avoids the button
        // landing past the actual visible area regardless of how wide the window is.
        var avail = ImGui.GetContentRegionAvail().X;
        if (avail > buttonSize.X)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - buttonSize.X);

        // Ko-fi's own brand color, so the button reads as "support link" at a glance. All
        // three colors release automatically once this method returns.
        using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 0.369f, 0.357f, 1.0f));
        using var hoveredColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.45f, 0.44f, 1.0f));
        using var activeColor = ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.85f, 0.28f, 0.27f, 1.0f));

        if (ImGui.Button(label, buttonSize))
            Util.OpenLink("https://ko-fi.com/grimmortaldread");
    }
}
