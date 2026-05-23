using Content.Client._Arcane.ERP.Preferences;
using Content.Client.Lobby;
using Content.Shared._Arcane.ERP.Organs;
using Content.Shared._Arcane.ERP.OrgansAppearance;
using Content.Shared._Arcane.ERP.Preferences;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Arcane.ERP.OrgansAppearance;

public sealed class ErpOrganVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ClientErpOrganPreferencesManager _erpPrefs = default!;
    [Dependency] private readonly IClientPreferencesManager _prefs = default!;

    private enum OrganLayer : byte
    {
        Penis,
        Vagina,
        Breasts,
        Testicles,
        Anus,
    }

    private static readonly Dictionary<string, OrganLayer> SlotToLayer = new()
    {
        [ErpOrganSlots.Penis]     = OrganLayer.Penis,
        [ErpOrganSlots.Vagina]    = OrganLayer.Vagina,
        [ErpOrganSlots.Breasts]   = OrganLayer.Breasts,
        [ErpOrganSlots.Testicles] = OrganLayer.Testicles,
        [ErpOrganSlots.Anus]      = OrganLayer.Anus,
    };

    private static readonly Dictionary<string, HumanoidVisualLayers> OrganCoverageLayer = new()
    {
        [ErpOrganSlots.Penis]     = HumanoidVisualLayers.ErpGroin,
        [ErpOrganSlots.Vagina]    = HumanoidVisualLayers.ErpGroin,
        [ErpOrganSlots.Testicles] = HumanoidVisualLayers.ErpGroin,
        [ErpOrganSlots.Anus]      = HumanoidVisualLayers.ErpGroin,
        [ErpOrganSlots.Breasts]   = HumanoidVisualLayers.ErpChest,
    };

    private static readonly Dictionary<string, string> OrganRsiPath = new()
    {
        [ErpOrganSlots.Penis]     = "/Textures/_Arcane/ERP/Mobs/penis_onmob.rsi",
        [ErpOrganSlots.Vagina]    = "/Textures/_Arcane/ERP/Mobs/vagina_onmob.rsi",
        [ErpOrganSlots.Breasts]   = "/Textures/_Arcane/ERP/Mobs/breasts_onmob.rsi",
        [ErpOrganSlots.Testicles] = "/Textures/_Arcane/ERP/Mobs/testicles_onmob.rsi",
        [ErpOrganSlots.Anus]      = "/Textures/_Arcane/ERP/Mobs/anus_onmob.rsi",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ErpOrganVisualsComponent, AfterAutoHandleStateEvent>(OnOrganState);
        SubscribeLocalEvent<ErpOrganVisualsComponent, ComponentShutdown>(OnOrganShutdown);

        // Clothing equipped/unequipped → HiddenLayers changed → update visibility
        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidVisualStateUpdatedEvent>(OnHumanoidState);

        // Editor preview: client-side dummy entity, no server state
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnPreviewProfileLoaded);
    }

    public void RefreshPreview(EntityUid uid, ErpOrganPreferences prefs)
    {
        if (!IsClientSide(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var visuals = EnsureComp<ErpOrganVisualsComponent>(uid);
        visuals.Organs = new Dictionary<string, ErpOrganConfig>(prefs.Organs);

        ApplyOrganLayers((uid, visuals), CompOrNull<HumanoidAppearanceComponent>(uid), sprite);
    }

    private void OnPreviewProfileLoaded(Entity<HumanoidAppearanceComponent> ent, ref ProfileLoadFinishedEvent args)
    {
        if (!IsClientSide(ent))
            return;

        if (!HasComp<EroticOrgansComponent>(ent))
            return;

        var slot = _prefs.Preferences?.SelectedCharacterIndex ?? 0;
        var organPrefs = _erpPrefs.GetSlot(slot);

        var visuals = EnsureComp<ErpOrganVisualsComponent>(ent);
        visuals.Organs = new Dictionary<string, ErpOrganConfig>(organPrefs.Organs);

        if (TryComp<SpriteComponent>(ent, out var sprite))
            ApplyOrganLayers((ent, visuals), ent.Comp, sprite);
    }

    private void OnOrganState(Entity<ErpOrganVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        ApplyOrganLayers(ent, CompOrNull<HumanoidAppearanceComponent>(ent), sprite);
    }

    private void OnHumanoidState(Entity<HumanoidAppearanceComponent> ent, ref HumanoidVisualStateUpdatedEvent args)
    {
        if (!HasComp<ErpOrganVisualsComponent>(ent))
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        foreach (var slotId in ErpOrganSlots.All)
        {
            if (!SlotToLayer.TryGetValue(slotId, out var layerKey))
                continue;

            if (!_sprite.LayerMapTryGet((ent, sprite), layerKey, out var index, false))
                continue;

            _sprite.LayerSetVisible((ent, sprite), index, IsOrganVisible(slotId, ent.Comp));
        }
    }

    private void OnOrganShutdown(Entity<ErpOrganVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        foreach (var layerKey in Enum.GetValues<OrganLayer>())
            _sprite.RemoveLayer((ent, sprite), layerKey, logMissing: false);
    }

    private void ApplyOrganLayers(Entity<ErpOrganVisualsComponent> ent, HumanoidAppearanceComponent? humanoid, SpriteComponent sprite)
    {
        foreach (var slotId in ErpOrganSlots.All)
        {
            if (!SlotToLayer.TryGetValue(slotId, out var layerKey))
                continue;
            if (!OrganRsiPath.TryGetValue(slotId, out var rsiPath))
                continue;

            ent.Comp.Organs.TryGetValue(slotId, out var cfg);
            cfg ??= ErpOrganConfig.Default();

            var index = _sprite.LayerMapReserve((ent, sprite), layerKey);
            _sprite.LayerSetRsi((ent, sprite), index, new ResPath(rsiPath), BuildStateName(slotId, cfg));
            _sprite.LayerSetColor((ent, sprite), index, cfg.Color ?? humanoid?.SkinColor ?? Color.White);
            _sprite.LayerSetVisible((ent, sprite), index, IsOrganVisible(slotId, humanoid));
        }
    }

    private static bool IsOrganVisible(string slotId, HumanoidAppearanceComponent? humanoid)
    {
        if (humanoid == null)
            return true;

        if (!OrganCoverageLayer.TryGetValue(slotId, out var coverageLayer))
            return true;

        return !humanoid.HiddenLayers.ContainsKey(coverageLayer)
            && !humanoid.PermanentlyHidden.Contains(coverageLayer);
    }

    private static string BuildStateName(string slotId, ErpOrganConfig cfg)
    {
        if (slotId == ErpOrganSlots.Breasts)
            return $"breasts_{cfg.Variant}_0";

        return $"{slotId}_{cfg.Variant}_{cfg.Size}_0";
    }
}
