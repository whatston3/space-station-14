using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Systems;

public sealed partial class RegenerativeStasisSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MobStateSystem _mobs = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDeathgaspSystem _deathgasp = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;
    [Dependency] private EntityQuery<InjurableComponent> _injurableQuery = default!;
    [Dependency] private EntityQuery<MobThresholdsComponent> _mobThresholdsQuery = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = new("Brute");
    private static readonly ProtoId<DamageTypePrototype> BluntType = new("Blunt");
    private static readonly ProtoId<DamageTypePrototype> SlashType = new("Slash");
    private static readonly ProtoId<DamageTypePrototype> PierceType = new("Piercing");

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<RegenerativeStasisActionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.InitialName = MetaData(ent).EntityName;
        ent.Comp.InitialDescription = MetaData(ent).EntityDescription;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<RegenerativeStasisActionComponent> ent, ref ActionRelayedEvent<MobStateChangedEvent> args)
    {
        // If we are revived cancel the stasis.
        if (args.Args.NewMobState == MobState.Alive && ent.Comp.IsInStasis)
            CancelStasis(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnMoveGhost(Entity<RegenerativeStasisActionComponent> ent, ref ActionRelayedEvent<GhostAttemptEvent> args)
    {
        if (ent.Comp.AllowGhosting || !ent.Comp.IsInStasis)
            return;

        args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStasisUse(Entity<RegenerativeStasisActionComponent> ent, ref ChangelingStasisActionEvent args)
    {
        if (ent.Comp.IsInStasis)
        {
            ExitStasis((ent, ent.Comp), args.Performer);
            args.Handled = true; //Only handle when exiting, as we don't need the useDelay otherwise.
            return;
        }

        EnterStasis((ent, ent.Comp), args.Performer);
    }

    /// <summary>
    /// Enter the stasis and set the action cooldown depending on the damage you have taken.
    /// </summary>
    public void EnterStasis(Entity<RegenerativeStasisActionComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.IsInStasis)
            return;

        // Damage ourselves to the point of death.
        if (!_mobs.IsDead(target)
            && _mobThresholdsQuery.TryComp(target, out var thresholdComp)
            && _damageableQuery.TryComp(target, out var damageableComp)
            && _mobThresholds.TryGetDeadThreshold(target, out var deadThreshold, thresholdComp))
        {
            var damage = _damage.GetPositiveDamage((target, damageableComp));
            var totalDamage = damage.GetTotal();
            var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent), GetNetEntity(target));
            var targetDamage = deadThreshold * (1.0f + random.NextFloat(ent.Comp.MinAdditionalDamage, ent.Comp.MaxAdditionalDamage));
            if (totalDamage <= 0)
            {
                // Create damage from scratch from the known damageable types.
                if (_injurableQuery.TryComp(target, out var injurableComp)
                    && ProtoMan.TryIndex(injurableComp.DamageContainer, out var damageCont))
                {
                    // Try to deal some amount of brute damage if possible, otherwise deal whatever we can at random.
                    var hasBrute = damageCont.SupportedGroups.Contains(BruteGroup);
                    var bluntSupported = hasBrute || damageCont.SupportedTypes.Contains(BluntType);
                    var slashSupported = hasBrute || damageCont.SupportedTypes.Contains(SlashType);
                    var pierceSupported = hasBrute || damageCont.SupportedTypes.Contains(PierceType);

                    if (!bluntSupported && !slashSupported && !pierceSupported)
                    {
                        if (damageCont.SupportedGroups.Count > 0)
                        {
                            DamageSpecifier damageToAdd = new(ProtoMan.Index(_random.Pick(damageCont.SupportedGroups)), targetDamage.Value);
                            _damage.ChangeDamage((target, damageableComp), damageToAdd, ignoreResistances: true, ignoreGlobalModifiers: true);
                        }
                        else if (damageCont.SupportedTypes.Count > 0)
                        {
                            DamageSpecifier damageToAdd = new(ProtoMan.Index(_random.Pick(damageCont.SupportedGroups)), targetDamage.Value);
                            _damage.ChangeDamage((target, damageableComp), damageToAdd, ignoreResistances: true, ignoreGlobalModifiers: true);
                        }
                    }
                    else
                    {
                        var bluntAmount = bluntSupported ? _random.Next() + 0.01f : 0.0f;
                        var slashAmount = slashSupported ? _random.Next() + 0.01f : 0.0f;
                        var pierceAmount = pierceSupported ? _random.Next() + 0.01f : 0.0f;
                        var totalAmount = bluntAmount + slashAmount + pierceAmount;

                        DamageSpecifier damageToAdd = new();
                        if (bluntAmount > 0)
                            damageToAdd += new DamageSpecifier(ProtoMan.Index(BluntType), targetDamage.Value * (bluntAmount / totalAmount));
                        if (slashAmount > 0)
                            damageToAdd += new DamageSpecifier(ProtoMan.Index(SlashType), targetDamage.Value * (slashAmount / totalAmount));
                        if (bluntAmount > 0)
                            damageToAdd += new DamageSpecifier(ProtoMan.Index(PierceType), targetDamage.Value * (pierceAmount / totalAmount));
                        _damage.ChangeDamage((target, damageableComp), damageToAdd, ignoreResistances: true, ignoreGlobalModifiers: true);
                    }
                }
            }
            else if (totalDamage < deadThreshold)
            {
                // Scale our damage up.
                var damageToAdd = damage * (float)(deadThreshold - totalDamage / totalDamage);
                _damage.ChangeDamage((target, damageableComp), damageToAdd);
            }
        }

        // Just in case, this should explicitly disallow revival.
        if (_mobThresholdsQuery.TryComp(target, out var mobThresholds))
        {
            ent.Comp.PreviousAllowRevives = mobThresholds.AllowRevives;
            _mobThresholds.SetAllowRevives(target, false, mobThresholds);
        }
        else
        {
            ent.Comp.PreviousAllowRevives = false;
        }

        // If we didn't die yet, die temporarily until we revive.
        // Ghosting will be blocked while in stasis.
        if (!_mobs.IsDead(target))
            _mobs.ChangeMobState(target, MobState.Dead);

        _popup.PopupEntity(Loc.GetString("changeling-stasis-enter"), target, target, PopupType.MediumCaution);

        ent.Comp.IsInStasis = true;
        Dirty(ent);

        var stasisDuration = ent.Comp.MinStasisCooldown;

        stasisDuration += ent.Comp.BonusCooldownPerDamage * (double)_damage.GetTotalDamage(target);
        stasisDuration = new TimeSpan(Math.Clamp(stasisDuration.Ticks, ent.Comp.MinStasisCooldown.Ticks, ent.Comp.MaxStasisCooldown.Ticks)); // No clamp method for TimeSpans

        _metaData.SetEntityName(ent, Loc.GetString("changeling-stasis-active-name"));
        _metaData.SetEntityDescription(ent, Loc.GetString("changeling-stasis-active-desc"));

        _actions.SetToggled(ent.Owner, ent.Comp.IsInStasis);
        _actions.SetCooldown(ent.Owner, stasisDuration);
    }

    /// <summary>
    /// Exit the stasis and heal all damage and bloodloss.
    /// TODO: Maybe add a some sort of rejuvenate lite so that we can also heal some status effects?
    /// </summary>
    public void ExitStasis(Entity<RegenerativeStasisActionComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.IsInStasis)
            return;

        // Heal all damage.
        _damage.ClearAllDamage(target);

        // Heal bloodloss and stop bleeding.
        if (_bloodstreamQuery.TryComp(target, out var bloodstream))
        {
            _bloodstream.TryRegulateBloodLevel((target, bloodstream), bloodstream.BloodReferenceSolution.MaxVolume);
            _bloodstream.TryModifyBleedAmount((target, bloodstream), -bloodstream.BleedAmount);
        }

        // Restore prior revivability (just in case)
        _mobThresholds.SetAllowRevives(target, ent.Comp.PreviousAllowRevives);

        // Revive.
        _mobs.ChangeMobState(target, MobState.Alive);

        _popup.PopupEntity(Loc.GetString("changeling-stasis-exit"), Loc.GetString("changeling-stasis-exit-others", ("user", Identity.Entity(target, EntityManager))), target, target, PopupType.MediumCaution);
        _audio.PlayPredicted(ent.Comp.ExitSound, target, target);

        ent.Comp.IsInStasis = false;
        Dirty(ent);

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Owner, ent.Comp.IsInStasis);
    }

    /// <summary>
    /// Cancel the stasis without healing.
    /// </summary>
    public void CancelStasis(Entity<RegenerativeStasisActionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.IsInStasis)
            return;

        ent.Comp.IsInStasis = false;
        Dirty(ent);

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Owner, ent.Comp.IsInStasis);
    }
}

/// <summary>
/// Action event for entering/leaving the stasis.
/// </summary>
public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;
