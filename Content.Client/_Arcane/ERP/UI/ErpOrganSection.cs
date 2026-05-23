using Content.Shared._Arcane.ERP.Preferences;
using Content.Shared.Humanoid;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client._Arcane.ERP.UI;

public sealed class ErpOrganSection : BoxContainer
{
    // Per-organ available variants. Expand as RSI states are added.
    private static readonly Dictionary<string, string[]> OrganVariants = new()
    {
        [ErpOrganSlots.Penis]     = ["human"],
        [ErpOrganSlots.Vagina]    = ["human"],
        [ErpOrganSlots.Breasts]   = ["human"],
        [ErpOrganSlots.Testicles] = ["human"],
        [ErpOrganSlots.Anus]      = ["human"],
    };

    // Slots shown only for specific sexes (null = all sexes)
    private static readonly Dictionary<string, Sex[]> SlotSexFilter = new()
    {
        [ErpOrganSlots.Penis]     = [Sex.Male],
        [ErpOrganSlots.Testicles] = [Sex.Male],
        [ErpOrganSlots.Vagina]    = [Sex.Female],
        [ErpOrganSlots.Breasts]   = [Sex.Female],
    };

    private ErpOrganPreferences _prefs = ErpOrganPreferences.Default();
    private bool _settingPreferences;

    private readonly Dictionary<string, (BoxContainer Row, OptionButton Variant)> _organControls = new();

    public event Action<ErpOrganPreferences>? OnPreferencesChanged;

    public ErpOrganSection()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        Margin = new Thickness(0, 8, 0, 0);
        IoCManager.InjectDependencies(this);
        Build();
    }

    private void Build()
    {
        var header = new Label
        {
            Text = Loc.GetString("erp-organ-section-title"),
            Margin = new Thickness(0, 0, 0, 2),
        };
        AddChild(header);

        foreach (var slotId in ErpOrganSlots.All)
        {
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                SeparationOverride = 8,
                Margin = new Thickness(0, 2),
            };

            row.AddChild(new Label
            {
                Text = Loc.GetString($"erp-preferences-tab-organ-{slotId}"),
                MinWidth = 90,
                VAlign = Label.VAlignMode.Center,
            });

            var variants = OrganVariants.TryGetValue(slotId, out var v) ? v : ["human"];
            var variantBtn = new OptionButton { MinWidth = 120 };
            foreach (var variant in variants)
                variantBtn.AddItem(Loc.GetString($"erp-preferences-tab-variant-{variant}"), variantBtn.ItemCount);

            variantBtn.OnItemSelected += args =>
            {
                variantBtn.SelectId(args.Id);
                NotifyChange(slotId);
            };

            row.AddChild(variantBtn);
            AddChild(row);
            _organControls[slotId] = (row, variantBtn);
        }
    }

    public void SetSex(Sex sex)
    {
        foreach (var (slotId, (row, _)) in _organControls)
        {
            row.Visible = !SlotSexFilter.TryGetValue(slotId, out var allowed) || Array.IndexOf(allowed, sex) >= 0;
        }
    }

    public void SetPreferences(ErpOrganPreferences prefs)
    {
        _settingPreferences = true;
        try
        {
            _prefs = prefs;

            foreach (var slotId in ErpOrganSlots.All)
            {
                if (!_organControls.TryGetValue(slotId, out var controls))
                    continue;

                var cfg = prefs.GetOrgan(slotId);
                var variants = OrganVariants.TryGetValue(slotId, out var v) ? v : ["human"];
                var idx = Array.IndexOf(variants, cfg.Variant);
                controls.Variant.SelectId(idx >= 0 ? idx : 0);
            }
        }
        finally
        {
            _settingPreferences = false;
        }
    }

    private void NotifyChange(string slotId)
    {
        if (_settingPreferences)
            return;

        if (!_organControls.TryGetValue(slotId, out var controls))
            return;

        var variants = OrganVariants.TryGetValue(slotId, out var v) ? v : ["human"];
        var idx = controls.Variant.SelectedId;
        var variant = idx < variants.Length ? variants[idx] : "human";

        var existing = _prefs.GetOrgan(slotId);
        _prefs.SetOrgan(slotId, new ErpOrganConfig { Variant = variant, Size = existing.Size });

        OnPreferencesChanged?.Invoke(_prefs);
    }
}
