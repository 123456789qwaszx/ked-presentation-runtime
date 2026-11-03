using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class DepthFocusCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Mode")]
    [Tooltip("true면 slotKey를 무시하고 모든 캐릭터의 블러를 0으로 되돌린다.")]
    public bool clear;

    [Header("Circle of Confusion")]
    [Tooltip("초점면에서 depthScale이 1만큼 떨어질 때 붙는 블러량. " +
             "depthScale은 Far 0.68 ~ Close 1.38이라 최대 간격이 0.70이다 — " +
             "1.43이면 무대 양 끝이 최대로 흐려진다.")]
    [Min(0f)] public float falloff = 1.43f;

    [Tooltip("착란원이 아무리 커도 이 값을 넘지 않는다.")]
    [Range(0f, 1f)] public float maxBlur = 1f;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// 초점 캐릭터를 지명하고, 나머지를 깊이 차이에 비례해 흐린다.
///
/// v2를 버린 이유가 여기서 해결된다 — 레이어를 통째로 균일하게 흐리면 초점이 나간 게 아니라
/// 렌즈에 자국이 남은 것처럼 보인다. 흐림이 깊이마다 달라야 디포커스로 읽힌다.
///
/// 깊이는 레이어가 아니라 <see cref="CharacterRigRefs.SettledDepthScale"/>에서 온다.
/// 캐릭터는 대부분 mid 레이어에 몰려 있고 깊이는 size 커맨드의 프리셋으로 갈리기 때문에,
/// 레이어를 기준으로 삼으면 전원이 같은 값이 되어 v2의 실패를 반복한다.
///
/// 배경은 대상이 아니다 — screen_blur가 이미 레이어 단위로 처리한다. 배경도 캐릭터 리그로 서지만
/// **캐스팅하지 않은 슬롯**이라, 배역이 있는 슬롯만 훑는다(focus_clear도 같다 — 배경의 screen_blur를 지우지 않는다).
/// 플래그가 아니라 구조로 빠진다.
///
/// 리그마다 "초점 블러" 의도만 넘긴다 — 전환은 각 리그의 RigVisualEffectController가 소유하고,
/// 모양(char_visual)·레이어(screen_blur) 블러와는 가장 큰 값으로 합쳐진다.
/// </summary>
public sealed class DepthFocusCommandCharR : CommandBase
{
    // 화면 전체의 초점이 한꺼번에 옮겨가면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly DepthFocusCommandSpecCharR _spec;
    private readonly PresentationStage _stage;

    private readonly List<(RigVisualEffectController Controller, float Blur)> _targets = new();
    private readonly List<KeyValuePair<string, CharacterRigRefs>> _scratch = new();

    public override bool WaitForCompletion => _spec.wait;

    public DepthFocusCommandCharR(PresentationStage stage, DepthFocusCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        CollectTargets();

        for (int i = 0; i < _targets.Count; i++)
            _targets[i].Controller.FollowFocusBlur(_targets[i].Blur, _spec.duration, _spec.ease);

        if (!_spec.wait)
            yield break;

        while (AnyTransitioning())
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        CollectTargets();

        for (int i = 0; i < _targets.Count; i++)
            _targets[i].Controller.SnapFocusBlur(_targets[i].Blur);
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        for (int i = 0; i < _targets.Count; i++)
            _targets[i].Controller.AccelerateFocusBlur(StepFinishSpeedUpMultiplier);
    }

    private void CollectTargets()
    {
        _targets.Clear();

        CharacterRigRefs focusRig = null;

        if (!_spec.clear)
        {
            focusRig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

            if (focusRig == null)
            {
                Debug.LogWarning(
                    $"[DepthFocusCommandCharR] Focus character not found. " +
                    $"slotKey='{_spec.slotKey}'. 초점면을 정할 수 없어 아무것도 흐리지 않는다.");
                return;
            }
        }

        float focusScale = focusRig != null ? focusRig.SettledDepthScale : 0f;

        _scratch.Clear();
        _stage.characterRigs.CollectAliveRigEntries(_scratch);

        for (int i = 0; i < _scratch.Count; i++)
        {
            CharacterRigRefs refs = _scratch[i].Value;

            if (refs?.VisualEffect == null)
                continue;

            // 배경(캐스팅하지 않은 슬롯)은 초점면 계산에 끼지 않는다 — SettledDepthScale 1로 섞이면
            // 배경이 초점 캐릭터와 같은 깊이에 있는 척 흐려지거나 선명해진다.
            if (!_stage.castRegistry.TryPeekCharacter(_scratch[i].Key, out _))
                continue;

            _targets.Add((
                refs.VisualEffect,
                _spec.clear ? 0f : CircleOfConfusion(refs.SettledDepthScale, focusScale)));
        }
    }

    private float CircleOfConfusion(float depthScale, float focusScale)
        => Mathf.Clamp01(Mathf.Abs(depthScale - focusScale) * _spec.falloff)
           * Mathf.Clamp01(_spec.maxBlur);

    private bool AnyTransitioning()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i].Controller.IsFocusBlurTransitioning)
                return true;
        }

        return false;
    }
}
