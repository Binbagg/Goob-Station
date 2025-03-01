using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Rest;

public sealed class XenoRestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoRestActionEvent>(OnXenoRestAction);

        SubscribeLocalEvent<XenoRestingComponent, UpdateCanMoveEvent>(OnXenoRestingCanMove);
        SubscribeLocalEvent<XenoRestingComponent, AttackAttemptEvent>(OnXenoRestingMeleeHit);
        SubscribeLocalEvent<XenoRestingComponent, XenoSecreteStructureAttemptEvent>(OnXenoSecreteStructureAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoLeapAttemptEvent>(OnXenoRestingLeapAttempt);
        SubscribeLocalEvent<ActionBlockIfRestingComponent, RMCActionUseAttemptEvent>(OnXenoRestingActionUseAttempt);
    }

    private void OnXenoRestingActionUseAttempt(Entity<ActionBlockIfRestingComponent> ent, ref RMCActionUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;
        if (HasComp<XenoRestingComponent>(user))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString(ent.Comp.Popup), user, user, PopupType.SmallCaution);
        }
    }

    private void OnXenoRestingCanMove(Entity<XenoRestingComponent> xeno, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnXenoRestAction(Entity<XenoComponent> xeno, ref XenoRestActionEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var attempt = new XenoRestAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        if (HasComp<XenoRestingComponent>(xeno))
        {
            RemComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.NotResting);
            _actions.SetToggled(args.Action, false);
        }
        else
        {
            AddComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.Resting);
            _actions.SetToggled(args.Action, true);
        }

        _actionBlocker.UpdateCanMove(xeno);

        var ev = new XenoRestEvent(HasComp<XenoRestingComponent>(xeno));
        RaiseLocalEvent(xeno, ref ev);
    }

    private void OnXenoRestingMeleeHit(Entity<XenoRestingComponent> xeno, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnXenoSecreteStructureAttempt(Entity<XenoRestingComponent> xeno, ref XenoSecreteStructureAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-secrete"), xeno, xeno);
        args.Cancelled = true;
    }


    private void OnXenoRestingLeapAttempt(Entity<XenoRestingComponent> xeno, ref XenoLeapAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-leap"), xeno, xeno);
        args.Cancelled = true;
    }

}
