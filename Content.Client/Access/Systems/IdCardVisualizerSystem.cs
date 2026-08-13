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

    protected override void OnAppearanceChange(EntityUid uid, IdCardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!component.UpdateVisuals)
            return;

        var prototype = GetPrototypeToDraw(uid);

        if (prototype == component.LastPrototype)
            return;

        component.LastPrototype = prototype;
        var data = ConstructData((uid, component), initialCheck: false);

        SetIdVisuals((uid, args.Sprite), data);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<IdCardVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var prototype = GetPrototypeToDraw(ent);

        ent.Comp.LastPrototype = prototype;

        // Construct data, respecting our job icon if one exists!
        var data = ConstructData(ent, initialCheck: true);

        SetIdVisuals((ent, sprite), data);
    }

    private string? GetPrototypeToDraw(EntityUid uid)
    {
        if (AppearanceSystem.TryGetData<string>(uid, IdCardVisuals.JobProto, out var jobProto))
        {
            return jobProto;
        }
        else if (TryComp<IdCardComponent>(uid, out var idCard)
                && idCard.JobPrototype != null)
        {
            return idCard.JobPrototype;
        }
        else if (TryComp<PresetIdCardComponent>(uid, out var presetIdCard))
        {
            return presetIdCard.JobName;
        }
        return null;
    }

    private IdCardVisualData ConstructData(Entity<IdCardVisualsComponent> ent, bool initialCheck)
    {
        IdCardVisualData data = new()
        {
            BaseState = ent.Comp.BaseState,
            TopStripeState = ent.Comp.StripeTopState,
            TopStripeColor = ent.Comp.StripeTopColor,
            BottomStripeState = ent.Comp.StripeBottomState,
            BottomStripeColor = ent.Comp.StripeBottomColor,
            JobIconState = ent.Comp.JobIconState
        };

        // Try to get job icon, first from appearance data (implying a rewrite),
        // falling back to the ID card itself, then the ID card preset if that isn't set.
        // Note: currently doesn't handle RSI path!
        string jobProto;
        if (AppearanceSystem.TryGetData<string>(ent, IdCardVisuals.JobProto, out jobProto)
            && TryGetJobIconState(jobProto, out var jobIconState))
        {
            if (!initialCheck || data.JobIconState == null)
                data.JobIconState = jobIconState;
        }
        else if (TryComp<IdCardComponent>(ent, out var idCard)
                && idCard.JobPrototype != null
                && TryGetJobIconState(idCard.JobPrototype, out jobIconState))
        {
            jobProto = idCard.JobPrototype;
            if (!initialCheck || data.JobIconState == null)
                data.JobIconState = jobIconState;
        }
        else if (TryComp<PresetIdCardComponent>(ent, out var presetIdCard)
            && presetIdCard.JobName != null
            && TryGetJobIconState(presetIdCard.JobName, out jobIconState))
        {
            jobProto = presetIdCard.JobName;
            if (!initialCheck || data.JobIconState == null)
                data.JobIconState = jobIconState;
        }
        else
        {
            jobProto = string.Empty;
        }

        // Finally, look for stripe data in IdCardVisualsPrototype

        if (initialCheck && ProtoMan.TryIndex(ent.Comp.StartingVisuals, out var idCardVisuals))
        {
            SetStripeDataFromVisuals(idCardVisuals, ref data);
        }
        else if (ProtoMan.TryIndex(jobProto, out idCardVisuals))
        {
            SetStripeDataFromVisuals(idCardVisuals, ref data);
        }
        else if (GetFirstPrimaryDepartmentWithIdVisuals(jobProto, out idCardVisuals))
        {
            SetStripeDataFromVisuals(idCardVisuals, ref data);
        }
        else
        {
            if (data.TopStripeColor == null && data.TopStripeState != null)
                data.TopStripeVisible = false;
            if (data.BottomStripeColor == null && data.BottomStripeState != null)
                data.BottomStripeVisible = false;
        }

        return data;
    }

    /// <summary>
    /// Writes the first department with ID visuals for a given job prototype into <paramref name="prototype"/>, if one exists.
    /// </summary>
    private bool GetFirstPrimaryDepartmentWithIdVisuals(string jobProto, [NotNullWhen(true)] out IdCardVisualsPrototype? prototype)
    {
        prototype = null;

        if (!_job.TryGetAllDepartments(jobProto, out var departments))
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
    private void SetStripeDataFromVisuals(IdCardVisualsPrototype visuals, ref IdCardVisualData data)
    {
        if (visuals.TopStripeColor != null)
            data.TopStripeColor ??= visuals.TopStripeColor.Value;
        if (visuals.BottomStripeColor != null)
            data.BottomStripeColor ??= visuals.BottomStripeColor.Value;

        // Set states only if not null!
        if (visuals.TopStripeState != null)
            data.TopStripeState = visuals.TopStripeState;
        if (visuals.BottomStripeState != null)
            data.BottomStripeState = visuals.BottomStripeState;
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
            SpriteSystem.LayerSetVisible(sprite, layer, data.TopStripeVisible);
            SpriteSystem.LayerSetColor(sprite, layer, data.TopStripeColor ?? Color.White);
        }

        if (SpriteSystem.LayerMapTryGet(sprite, IdCardVisualLayers.BottomStripe, out layer, logMissing: false))
        {
            SpriteSystem.LayerSetRsiState(sprite, layer, data.BottomStripeState);
            SpriteSystem.LayerSetVisible(sprite, layer, data.BottomStripeVisible);
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
        public bool TopStripeVisible = true;
        public Color? TopStripeColor;
        public string? BottomStripeState;
        public bool BottomStripeVisible = true;
        public Color? BottomStripeColor;
        public string? JobIconState;
    }
}
