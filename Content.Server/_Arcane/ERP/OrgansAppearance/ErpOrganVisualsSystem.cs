using Content.Server._Arcane.ERP.Preferences;
using Content.Server.Preferences.Managers;
using Content.Shared._Arcane.ERP.Organs;
using Content.Shared._Arcane.ERP.OrgansAppearance;
using Content.Shared._Arcane.ERP.Preferences;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Humanoid;
using Robust.Shared.Player;

namespace Content.Server._Arcane.ERP.OrgansAppearance;

public sealed class ErpOrganVisualsSystem : EntitySystem
{
    [Dependency] private readonly ErpOrganPreferencesManager _erpPrefs = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnProfileLoaded);
        SubscribeLocalEvent<EroticOrganComponent, OrganRemovedFromBodyEvent>(OnOrganRemoved);
    }

    private void OnProfileLoaded(Entity<HumanoidAppearanceComponent> ent, ref ProfileLoadFinishedEvent args)
    {
        if (!HasComp<EroticOrgansComponent>(ent))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var userId = actor.PlayerSession.UserId;
        var slot = _prefs.GetPreferences(userId).SelectedCharacterIndex;
        var organPrefs = _erpPrefs.GetCached(userId, slot) ?? ErpOrganPreferences.Default();

        var visuals = EnsureComp<ErpOrganVisualsComponent>(ent);
        visuals.Organs = new Dictionary<string, ErpOrganConfig>(organPrefs.Organs);
        Dirty(ent, visuals);
    }

    private void OnOrganRemoved(Entity<EroticOrganComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ))
            return;

        if (!TryComp<ErpOrganVisualsComponent>(args.OldBody, out var visuals))
            return;

        visuals.Organs.Remove(organ.SlotId);
        Dirty(args.OldBody, visuals);
    }
}
