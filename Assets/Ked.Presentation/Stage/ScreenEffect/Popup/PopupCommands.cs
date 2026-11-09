using System;
using System.Collections;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class PopupOpenCommandSpec : CommandSpecBase
{
    [Tooltip("덮는 무대(stage01 · stage02). 본 무대에는 열 수 없다.")]
    public string stage = "stage01";

    [Tooltip("그림 — Resources/Popups/{키}")]
    public string pictureKey;

    [Tooltip("그림 중심. 무대 중심 기준 px, y 위.")]
    public Vector2 center;

    [Tooltip("프리셋 배율에 곱한다.")]
    public float scale = 1f;
}

[Serializable]
public sealed class PopupCloseCommandSpec : CommandSpecBase
{
    public string stage = "stage01";
}

/// <summary>
/// popup · popup_close — 팝업 그룹의 불투명도 하나를 프리셋 곡선으로 움직인다(docs/PLAN_popup.md).
/// 곡선은 페이드의 진행(0 → 1)으로 읽는다: 불투명도 = 시작값 + (끝값 − 시작값) × 진행. 그래서 이미 열린 팝업에
/// 다시 popup을 쓰면 불투명도 1에서 1로 — 그림만 바로 바뀐다(원본에서 본 적 없는 경우, 번쩍이지 않게).
/// 끝 · Skip · 스텝 경계 어디서 확정돼도 같은 끝값을 쓴다. 같은 팝업의 앞 페이드는 먼저 확정한다.
/// </summary>
public abstract class PopupCommandBase : CommandBase
{
    private readonly StageRigRegistry _stages;
    protected readonly PopupPresetDBSO Preset;

    private StagePopupSlot _slot;
    private CurveKey[] _keys;
    private float _duration;
    private float _from;
    private float _to;
    private Tween _tween;

    private bool _resolved;
    private bool _usable;
    private bool _claimed;

    protected PopupCommandBase(StageRigRegistry stages, PopupPresetDBSO preset)
    {
        _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        Preset = preset ? preset : throw new ArgumentNullException(nameof(preset));
    }

    protected abstract string StageKey { get; }
    protected abstract bool Wait { get; }

    /// <summary>페이드 끝의 불투명도 — 열기 1, 닫기 0.</summary>
    protected abstract float EndAlpha { get; }

    protected abstract CurveKey[] KeysOf(PopupPresetDBSO preset, out string why);

    /// <summary>페이드 전에 — false면 이 커맨드는 아무것도 하지 않는다(이유는 경고로 남긴다).</summary>
    protected abstract bool Begin(StagePopupSlot slot);

    /// <summary>페이드의 끝값을 쓴 뒤.</summary>
    protected abstract void End(StagePopupSlot slot);

    public override bool WaitForCompletion => Wait;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!Resolve())
            yield break;

        Claim();

        if (!_claimed)
            yield break;

        if (_keys == null || _duration <= 0f)
        {
            Finish();
            yield break;
        }

        _tween = DOTween
            .To(() => 0f, Apply, _duration, _duration)
            .SetEase(Ease.Linear)
            .SetTarget(_slot.Group)
            .OnComplete(Finish)
            .OnKill(OnKilled);

        _slot.SetActive(_tween, Finish);

        if (!Wait)
            yield break;

        while (_claimed)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!Resolve())
            return;

        if (!_claimed)
            Claim();

        Finish();   // Begin이 거절했으면 _claimed가 false라 아무것도 쓰지 않는다
    }

    protected override void OnStepLifetimeFinished()
    {
        if (_claimed)
            Finish();
    }

    private bool Resolve()
    {
        if (_resolved)
            return _usable;

        _resolved = true;

        if (StageRoles.Of(StageKey) == StageRole.Main)
        {
            Debug.LogWarning($"[Popup] 본 무대({StageKey})에는 그림 팝업을 열고 닫을 수 없다 — 덮는 무대(stage01 · stage02)에 쓴다. 건너뛴다.");
            return _usable = false;
        }

        _slot = _stages.Get(StageKey).Popup;
        _keys = KeysOf(Preset, out string why);

        if (_keys == null)
            Debug.LogWarning($"[Popup] 페이드 곡선이 유효하지 않다 ({Preset.name}): {why}. 끝값만 쓴다.");
        else
            _duration = _keys[_keys.Length - 1].Time;

        return _usable = true;
    }

    // 앞 페이드 확정 → 시작 상태(Begin) → 시작값.
    private void Claim()
    {
        _slot.CompleteActive();

        if (!Begin(_slot))
        {
            _usable = false;
            return;
        }

        _claimed = true;
        _from = _slot.Alpha;
        _to = EndAlpha;
    }

    private void Apply(float seconds)
    {
        if (!_claimed)
            return;

        _slot.SetAlpha(_from + (_to - _from) * Progress(seconds));
    }

    // 곡선 값 → 진행(0 → 1). 등장 곡선은 0 → 1, 퇴장 곡선은 1 → 0이라 양끝으로 정규화한다.
    private float Progress(float seconds)
    {
        float a = _keys[0].Value;
        float b = _keys[_keys.Length - 1].Value;
        float v = CurveFunctions.Evaluate(_keys, seconds);
        return Mathf.Approximately(a, b) ? 1f : Mathf.Clamp01((v - a) / (b - a));
    }

    private void Finish()
    {
        if (!_claimed)
            return;

        _claimed = false;
        _slot.ClearActive(_tween);
        _tween?.Kill(false);
        _tween = null;

        _slot.SetAlpha(_to);
        End(_slot);
    }

    // 확정 없이 끊겼다(tx · 마스크 지우기 · 장면 경계가 팝업을 닫았다) — 끝값을 쓰지 않고 손을 뗀다.
    private void OnKilled()
    {
        _claimed = false;
        _tween = null;
    }
}

public sealed class PopupOpenCommand : PopupCommandBase
{
    private readonly PopupOpenCommandSpec _spec;

    public PopupOpenCommand(PopupOpenCommandSpec spec, StageRigRegistry stages, PopupPresetDBSO preset)
        : base(stages, preset)
    {
        _spec = spec;
    }

    protected override string StageKey => _spec.stage;
    protected override bool Wait => _spec.wait;
    protected override float EndAlpha => 1f;
    protected override CurveKey[] KeysOf(PopupPresetDBSO preset, out string why) => preset.OpenKeys(out why);

    protected override bool Begin(StagePopupSlot slot)
    {
        // 못 찾으면 해석기가 경고한다 — 뒷막만 뜬다(폴드는 열림).
        Sprite picture = ResourcesSpriteResolver.Resolve(PopupPresetDBSO.PictureResourcesRoot, _spec.pictureKey);
        slot.Layout(Preset, picture, _spec.center, _spec.scale);
        return true;
    }

    protected override void End(StagePopupSlot slot) { }
}

public sealed class PopupCloseCommand : PopupCommandBase
{
    private readonly PopupCloseCommandSpec _spec;

    public PopupCloseCommand(PopupCloseCommandSpec spec, StageRigRegistry stages, PopupPresetDBSO preset)
        : base(stages, preset)
    {
        _spec = spec;
    }

    protected override string StageKey => _spec.stage;
    protected override bool Wait => _spec.wait;
    protected override float EndAlpha => 0f;
    protected override CurveKey[] KeysOf(PopupPresetDBSO preset, out string why) => preset.CloseKeys(out why);

    protected override bool Begin(StagePopupSlot slot)
    {
        if (slot.IsOpen)
            return true;

        Debug.LogWarning($"[Popup] {_spec.stage}에 열린 그림 팝업이 없다 — popup_close를 건너뛴다.");
        return false;
    }

    protected override void End(StagePopupSlot slot) => slot.SetClosed();
}
