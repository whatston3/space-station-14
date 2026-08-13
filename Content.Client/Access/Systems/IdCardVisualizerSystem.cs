using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Access.Systems;

/// <summary>
/// A system that initializes and updates ID card visuals.
/// Allows users to get a new looking ID card when they get assigned a new job.
/// </summary>
public sealed partial class IdCardVisualizerSystem : VisualizerSystem<IdCardVisualsComponent>
{
    [Dependency] private SharedJobSystem _job = default!;

    [Dependency] private EntityQuery<IdCardComponent> _idCardQuery = default!;
    [Dependency] private EntityQuery<PresetIdCardComponent> _presetIdCardQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    /// <inheritdoc />
    protected override void OnAppearanceChange(EntityUid uid, IdCardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!component.UpdateVisuals)
            return;

        var prototype = GetPrototypeToDraw(uid);

        if (prototype == component.LastPrototype)
            return;

        component.LastPrototype = prototype;
        var data = ConstructData((uid, component));

        SetIdVisuals((uid, args.Sprite), data);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<IdCardVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var prototype = GetPrototypeToDraw(ent);

        ent.Comp.LastPrototype = prototype;

        // Construct data, respecting our job icon if one exists!
        var data = ConstructData(ent);

        SetIdVisuals((ent, sprite), data);
    }

    private string? GetPrototypeToDraw(EntityUid uid)
    {
        if (AppearanceSystem.TryGetData<string>(uid, IdCardVisuals.JobProto, out var jobProto))
            return jobProto;

        if (_idCardQuery.TryComp(uid, out var idCard)
            && idCard.JobPrototype != null)
            return idCard.JobPrototype;

        if (_presetIdCardQuery.TryComp(uid, out var presetIdCard))
            return presetIdCard.JobName;

        return null;
    }

    private IdCardVisualData ConstructData(Entity<IdCardVisualsComponent> ent)
    {
        IdCardVisualData data = new()
        {
            JobIconState = ent.Comp.JobIconState
        };

        // Try to get job icon, first from appearance data (implying a rewrite),
        // falling back to the ID card itself, then the ID card preset if that isn't set.
        // Note: currently doesn't handle RSI path!
        if (AppearanceSystem.TryGetData(ent, IdCardVisuals.JobProto, out string? jobProto)
            && TryGetJobIconState(jobProto, out var jobIconState))
        {
            data.JobIconState ??= jobIconState;
        }
        else if (ent.Comp.JobIconState != null)
        {
            data.JobIconState = ent.Comp.JobIconState;
            jobProto = null;
        }
        else if (_idCardQuery.TryComp(ent, out var idCard)
                && idCard.JobPrototype != null
                && TryGetJobIconState(idCard.JobPrototype, out jobIconState))
        {
            jobProto = idCard.JobPrototype;
            data.JobIconState ??= jobIconState;
        }
        else if (_presetIdCardQuery.TryComp(ent, out var presetIdCard)
            && presetIdCard.JobName != null
            && TryGetJobIconState(presetIdCard.JobName, out jobIconState))
        {
            jobProto = presetIdCard.JobName;
            data.JobIconState ??= jobIconState;
        }
        else
        {
            jobProto = null;
        }

        // Finally, look for stripe data in IdCardVisualsPrototype
        if (jobProto != null && ProtoMan.TryIndex<IdCardVisualsPrototype>(jobProto, out var idCardVisuals)
            || ProtoMan.TryIndex(ent.Comp.StartingVisuals, out idCardVisuals)
            || GetFirstPrimaryDepartmentWithIdVisuals(jobProto, out idCardVisuals))
        {
            SetStripeDataFromVisuals(idCardVisuals, ref data);
        }

        return data;
    }

    /// <summary>
    /// Writes the first department with ID visuals for a given job prototype into <paramref name="prototype"/>, if one exists.
    /// </summary>
    private bool GetFirstPrimaryDepartmentWithIdVisuals(string? jobProto, [NotNullWhen(true)] out IdCardVisualsPrototype? prototype)
    {
        prototype = null;

        if (jobProto is null
            || !_job.TryGetAllDepartments(jobProto, out var departments))
            return false;

        foreach (var department in departments)
        {
            if (!department.Primary)
                continue;

            if (ProtoMan.TryIndex(department.ID, out prototype))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Uses an IdCardVisualsPrototype to set the stripes in <paramref name="data"/> before it can be applied to the ID card sprite.
    /// </summary>
    private void SetStripeDataFromVisuals(IdCardVisualsPrototype visuals, ref IdCardVisualData data, bool changeBase)
    {
        data.TopStripeColor ??= visuals.TopStripeColor;
        data.BottomStripeColor ??= visuals.BottomStripeColor;
        data.TopStripeState ??= visuals.TopStripeState;
        data.BottomStripeState ??= visuals.BottomStripeState;

        if (changeBase)
        {
            data.BaseState ??=
            data.
        }
    }

    /// <summary>
    /// Tries to find a job icon's state from the JobPrototype at <paramref name="jobProto"/>.
    /// Writes this out into <paramref name="jobIconState"/>.
    /// </summary>
    private bool TryGetJobIconState(string jobProto, out string jobIconState)
    {
        if (ProtoMan.TryIndex<JobPrototype>(jobProto, out var job)
            && ProtoMan.TryIndex(job.Icon, out var jobIcon)
            && jobIcon.Icon is SpriteSpecifier.Rsi { } rsi)
        {
            jobIconState = rsi.RsiState;
            return true;
        }

        jobIconState = string.Empty;
        return false;
    }

    /// <summary>
    /// Applies the state of <paramref name="data"/> into our ID card's sprite.
    /// </summary>
    private void SetIdVisuals(Entity<SpriteComponent?> sprite, IdCardVisualData data)
    {
        if (SpriteSystem.LayerMapTryGet(sprite, IdCardVisualLayers.Base, out var layer, logMissing: false))
            SpriteSystem.LayerSetRsiState(sprite, layer, data.BaseState);

        if (SpriteSystem.LayerMapTryGet(sprite, IdCardVisualLayers.TopStripe, out layer, logMissing: false))
        {
            SpriteSystem.LayerSetRsiState(sprite, layer, data.TopStripeState);
            SpriteSystem.LayerSetColor(sprite, layer, data.TopStripeColor ?? Color.White);
        }

        if (SpriteSystem.LayerMapTryGet(sprite, IdCardVisualLayers.BottomStripe, out layer, logMissing: false))
        {
            SpriteSystem.LayerSetRsiState(sprite, layer, data.BottomStripeState);
            SpriteSystem.LayerSetColor(sprite, layer, data.BottomStripeColor ?? Color.White);
        }

        if (SpriteSystem.LayerMapTryGet(sprite, IdCardVisualLayers.JobIcon, out layer, logMissing: false))
            SpriteSystem.LayerSetRsiState(sprite, layer, data.JobIconState);
    }

    /// <summary>
    /// A collection of all relevant ID card states.
    /// </summary>
    public struct IdCardVisualData()
    {
        public string? BaseState;
        public string? TopStripeState;
        public Color? TopStripeColor;
        public string? BottomStripeState;
        public Color? BottomStripeColor;
        public string? JobIconState;
    }
}
