using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.IgnitionSource;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.NightVision;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnNightVisionStartup);
        SubscribeLocalEvent<NightVisionComponent, MapInitEvent>(OnNightVisionMapInit);
        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnNightVisionAfterHandle);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnNightVisionRemove);
        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionAlertEvent>(OnNightVisionToggle);


        SubscribeLocalEvent<RMCNightVisionVisibleOnIgniteComponent, IgnitionEvent>(OnNightVisionVisibleIgnition);
    }

    private void OnNightVisionStartup(Entity<NightVisionComponent> ent, ref ComponentStartup args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionAfterHandle(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionMapInit(Entity<NightVisionComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnNightVisionRemove(Entity<NightVisionComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Alert is { } alert)
            _alerts.ClearAlert(ent, alert);

        NightVisionRemoved(ent);
    }

    private void OnNightVisionToggle(Entity<NightVisionComponent> ent, ref ToggleNightVisionAlertEvent args)
    {
        Toggle((ent, ent));
    }







    private void OnNightVisionVisibleIgnition(Entity<RMCNightVisionVisibleOnIgniteComponent> ent, ref IgnitionEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Ignite)
            EnsureComp<RMCNightVisionVisibleComponent>(ent);
        else
            RemCompDeferred<RMCNightVisionVisibleComponent>(ent);
    }

    public NightVisionState Toggle(Entity<NightVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return NightVisionState.Off;

        ent.Comp.State = ent.Comp.State switch
        {
            NightVisionState.Off => NightVisionState.Half,
            NightVisionState.Half => NightVisionState.Full,
            NightVisionState.Full => NightVisionState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAlert((ent, ent.Comp));
        return ent.Comp.State;
    }

    private void UpdateAlert(Entity<NightVisionComponent> ent)
    {
        if (ent.Comp.Alert is { } alert)
        {
            var level = MathF.Max((int) NightVisionState.Off, (int) ent.Comp.State);
            var max = _alerts.GetMaxSeverity(alert);
            var severity = max - ContentHelpers.RoundToLevels(level, (int) NightVisionState.Full, max + 1);
            _alerts.ShowAlert(ent, alert, (short) severity);
        }

        NightVisionChanged(ent);
    }



    protected virtual void NightVisionChanged(Entity<NightVisionComponent> ent)
    {
    }

    protected virtual void NightVisionRemoved(Entity<NightVisionComponent> ent)
    {
    }


    public void SetSeeThroughContainers(Entity<NightVisionComponent?> ent, bool see)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.SeeThroughContainers = see;
        Dirty(ent);
    }
}
