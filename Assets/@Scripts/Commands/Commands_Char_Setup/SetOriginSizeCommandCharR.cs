using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Origin Size",
    Order = -929,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -929)]
public sealed class SetOriginSizeCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_CastTransform;

    [Header("Scale Preset")]
    public CharScalePreset preset = CharScalePreset.Normal;

    [Header("Command Multiplier")]
    [Tooltip("최종 스케일에 곱해지는 커맨드 단위 배율. 1이면 그대로.")]
    public float multiplier = 1f;

    [Header("Override")]
    [Tooltip("체크하면 Preset/DB 계산을 무시하고 overrideScale을 직접 적용합니다.")]
    public bool overrideScale = false;

    public Vector3 scaleOverride = Vector3.one;

    [Header("Options")]
    [Tooltip("true면 X/Y/Z 모두 같은 값으로 적용합니다.")]
    public bool uniformScale = true;

    [Tooltip("uniformScale=false일 때 Y에 곱할 배율입니다.")]
    public float yMultiplier = 1f;

    [Tooltip("uniformScale=false일 때 Z에 곱할 배율입니다. UI RectTransform이면 보통 1을 유지합니다.")]
    public float zMultiplier = 1f;
}

public sealed class SetOriginSizeCommandCharR : CommandBase
{
    private readonly SetOriginSizeCommandSpecCharR _spec;
    private readonly CharStageTuningSO _globalTuning;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;

    private RectTransform _rect;
    private bool _resolveAttempted;

    public SetOriginSizeCommandCharR(
        SetOriginSizeCommandSpecCharR spec,
        CharStageTuningSO globalTuning,
        RoleAnchorTuningDBSO roleTuningDb)
    {
        _spec = spec;
        _globalTuning = globalTuning;
        _roleTuningDb = roleTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply(scope);
    }
    
    private void Apply(CommandRunScope scope)
    {
        if (_spec.overrideScale)
        {
            _rect.localScale = _spec.scaleOverride;
            return;
        }

        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, _spec.slotKey);

        float scale = CharScaleResolver.ResolveScale(
            _spec.preset,
            _globalTuning,
            _roleTuningDb,
            tuningKey,
            _spec.multiplier);

        if (_spec.uniformScale)
        {
            _rect.localScale = new Vector3(scale, scale, scale);
            return;
        }

        _rect.localScale = new Vector3(
            scale,
            scale * SafeMultiplier(_spec.yMultiplier),
            scale * SafeMultiplier(_spec.zMultiplier));
    }

    private static float SafeMultiplier(float value) => value == 0f ? 1f : value;

    private void ResolveRefs(CommandRunScope scope)
    {
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rigRefs.GetRect(_spec.target);
        
        _resolveAttempted = true;
    }
}