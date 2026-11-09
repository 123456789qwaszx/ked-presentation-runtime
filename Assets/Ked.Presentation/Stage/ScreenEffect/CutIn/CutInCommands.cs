using System;
using System.Collections;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class CutInOpenCommandSpec : CommandSpecBase
{
    [Tooltip("덮는 무대(stage01 · stage02). 본 무대에는 열 수 없다.")]
    public string stage = "stage01";

    [Tooltip("판 그림 — Resources/CutInPanels/{키}")]
    public string panelKey;

    [Tooltip("판 중심. 무대 중심 기준 px, y 위.")]
    public Vector2 center;

    [Tooltip("판 기본 크기(프리셋)에 곱한다.")]
    public float scale = 1f;
}

[Serializable]
public sealed class CutInCloseCommandSpec : CommandSpecBase
{
    public string stage = "stage01";
}

/// <summary>
/// cutin_open · cutin_close — 창에 모션 클립을 건다(docs/PLAN_cutin.md). 값은 캐릭터 motion과 같은 식(MotionMath.Compose, relative)이다.
/// 시작값은 창의 지금 값(열기는 Layout이 1 · 0으로 세운 뒤), 끝값은 클립의 마지막 키.
/// 끝 · Skip · 스텝 경계 어디서 확정돼도 같은 끝값을 쓴다. 같은 창의 앞 모션은 DOKill(complete)로 먼저 확정한다.
/// </summary>
public abstract class CutInCommandBase : CommandBase
{
    private readonly StageRigRegistry _stages;
    protected readonly CutInPresetDBSO Preset;

    private StageCutInSlot _slot;
    private MotionClip _clip;
    private Vector3 _startScale;
    private Vector3 _startEuler;
    private Tween _tween;

    private bool _resolved;
    private bool _usable;
    private bool _claimed;

    protected CutInCommandBase(StageRigRegistry stages, CutInPresetDBSO preset)
    {
        _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        Preset = preset ? preset : throw new ArgumentNullException(nameof(preset));
    }

    protected abstract string StageKey { get; }
    protected abstract bool Wait { get; }
    protected abstract MotionClip ClipOf(CutInPresetDBSO preset);

    /// <summary>모션 전에 — false면 이 커맨드는 아무것도 하지 않는다(이유는 경고로 남긴다).</summary>
    protected abstract bool Begin(StageCutInSlot slot);

    /// <summary>모션의 끝값을 쓴 뒤.</summary>
    protected abstract void End(StageCutInSlot slot);

    public override bool WaitForCompletion => Wait;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!Resolve())
            yield break;

        Claim();

        if (!_claimed)
            yield break;

        if (_clip.Duration <= 0f)
        {
            Finish();
            yield break;
        }

        _tween = DOTween
            .To(() => 0f, Apply, _clip.Duration, _clip.Duration)
            .SetEase(Ease.Linear)
            .SetTarget(_slot.Window)
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
            Debug.LogWarning($"[CutIn] 본 무대({StageKey})에는 컷인을 열고 닫을 수 없다 — 덮는 무대(stage01 · stage02)에 쓴다. 건너뛴다.");
            return _usable = false;
        }

        _slot = _stages.Get(StageKey).CutIn;
        _clip = ClipOf(Preset);

        string why = "클립이 없다";

        if (_clip == null || !MotionRules.TryValidate(_clip, null, out why))
        {
            Debug.LogWarning($"[CutIn] 모션 클립이 유효하지 않다 ({Preset.name}): {why}. 끝값만 쓴다.");
            _clip = null;
        }

        return _usable = true;
    }

    // 앞 모션 확정 → 시작 상태(Begin) → 시작값.
    private void Claim()
    {
        _slot.CompleteActive();

        if (!Begin(_slot))
        {
            _usable = false;
            return;
        }

        _claimed = true;
        _startScale = _slot.Window.localScale;
        _startEuler = SignedEuler(_slot.Window.localEulerAngles);

        if (_clip == null)
            _clip = new MotionClip { Name = "none", Rig = CutInPresetDBSO.WindowRig, Duration = 0f, End = MotionEnd.Commit, Channels = Array.Empty<MotionChannel>() };
    }

    private void Apply(float seconds)
    {
        if (!_claimed)
            return;

        foreach (MotionChannel channel in _clip.Channels)
            Write(channel, MotionMath.Compose(channel, MotionChannelMode.Relative, StartOf(channel), seconds));
    }

    private void Finish()
    {
        if (!_claimed)
            return;

        _claimed = false;
        _slot.ClearActive(_tween);
        _tween?.Kill(false);
        _tween = null;

        foreach (MotionChannel channel in _clip.Channels)
            Write(channel, MotionMath.EndValue(channel, MotionChannelMode.Relative, StartOf(channel)));

        End(_slot);
    }

    // 확정 없이 끊겼다(tx · 마스크 지우기 · 장면 경계가 창을 중립으로 돌렸다) — 끝값을 쓰지 않고 손을 뗀다.
    // 자연 종료 · 확정 뒤에도 불리지만 그때는 이미 _claimed가 false다.
    private void OnKilled()
    {
        _claimed = false;
        _tween = null;
    }

    private Vec3 StartOf(in MotionChannel channel)
        => channel.Kind == MotionChannelKind.Scale ? _startScale.ToCore() : _startEuler.ToCore();

    private void Write(in MotionChannel channel, Vec3 value)
    {
        switch (channel.Kind)
        {
            case MotionChannelKind.Scale:
                _slot.Window.localScale = new Vector3(value.X, value.Y, 1f);
                break;

            case MotionChannelKind.Euler:
                _slot.Window.localEulerAngles = value.ToUnity();
                break;

            // 창은 자리를 움직이지 않는다 — 자리는 Layout(대본의 x y)이 정한다.
            case MotionChannelKind.Position:
                break;
        }
    }

    private static Vector3 SignedEuler(Vector3 e)
        => new(Signed(e.x), Signed(e.y), Signed(e.z));

    private static float Signed(float degrees)
        => degrees > 180f ? degrees - 360f : degrees;
}

public sealed class CutInOpenCommand : CutInCommandBase
{
    private readonly CutInOpenCommandSpec _spec;

    public CutInOpenCommand(CutInOpenCommandSpec spec, StageRigRegistry stages, CutInPresetDBSO preset)
        : base(stages, preset)
    {
        _spec = spec;
    }

    protected override string StageKey => _spec.stage;
    protected override bool Wait => _spec.wait;
    protected override MotionClip ClipOf(CutInPresetDBSO preset) => preset.PopIn;

    protected override bool Begin(StageCutInSlot slot)
    {
        // 못 찾으면 해석기가 경고한다 — 판 없이 열린다(마스크 · 모션은 그대로).
        Sprite panel = ResourcesSpriteResolver.Resolve(CutInPresetDBSO.PanelResourcesRoot, _spec.panelKey);
        slot.Layout(Preset, panel, _spec.center, _spec.scale);
        return true;
    }

    protected override void End(StageCutInSlot slot) { }
}

public sealed class CutInCloseCommand : CutInCommandBase
{
    private readonly CutInCloseCommandSpec _spec;

    public CutInCloseCommand(CutInCloseCommandSpec spec, StageRigRegistry stages, CutInPresetDBSO preset)
        : base(stages, preset)
    {
        _spec = spec;
    }

    protected override string StageKey => _spec.stage;
    protected override bool Wait => _spec.wait;
    protected override MotionClip ClipOf(CutInPresetDBSO preset) => preset.PopOut;

    protected override bool Begin(StageCutInSlot slot)
    {
        if (slot.IsOpen)
            return true;

        Debug.LogWarning($"[CutIn] {_spec.stage}에 열린 컷인이 없다 — cutin_close를 건너뛴다.");
        return false;
    }

    protected override void End(StageCutInSlot slot) => slot.SetClosed();
}
