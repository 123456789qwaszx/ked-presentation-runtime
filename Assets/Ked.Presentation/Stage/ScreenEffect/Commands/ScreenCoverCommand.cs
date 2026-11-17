using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class ScreenCoverCommandSpec : CommandSpecBase
{
    [Header("Preset")]
    [Tooltip("ScreenCoverPresetDBSO entry key — 덮개의 색. ex) black, white")]
    public string presetKey = ScreenCoverPresetDBSO.DefaultPresetKey;

    [Tooltip("true면 덮개를 걷는다(screen_cover_clear). presetKey는 쓰지 않는다 — 색은 그대로 두고 덮인 정도만 0으로.")]
    public bool clear;

    [Header("Tween")]
    public float duration = 0.5f;
    public Ease ease = Ease.Linear;
}

// 덮개 — 화면 전체(무대 셋 · 무대 틀)를 한 색으로 덮고(screen_cover) 걷는다(screen_cover_clear). docs/PLAN_screen-cover.md §3.1.
// 커맨드는 의도만 넘기고 전환은 컨트롤러가 갖는다 — 스텝 경계에 묶이지 않아, 걷기는 다음 줄이 열려도 제 길이로 간다.
// 덮기는 다 덮일 때까지 기다린다(전경 — 파서가 wait를 세운다) — 뒤의 무대 정리가 늘 덮인 밑에서 일어나게(A5).
// 대사 줄 머리에서는 기다림이 내려져 덮기만 한다(docs/PLAN_line-beat.md Q1). 기다리는 동안 압축(빨리 넘기기 ·
// 스텝 서두르기 · 시크)이 켜지면 그 자리에서 끝값으로 가고 놓는다 — WaitCommand와 같다.
// 압축 중에 온 덮기 · 걷기는 끝값을 바로 세운다(OnSkip). 플래시는 끝값이 0이라 끊지만, 덮개는 끝 상태가 남으니 세운다.
public sealed class ScreenCoverCommand : CommandBase
{
    private readonly ScreenCoverCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenCoverPresetDBSO _presetDb;

    public override bool WaitForCompletion => _spec.wait;

    public ScreenCoverCommand(
        ScreenCoverCommandSpec spec,
        ScreenEffectRig screenEffects,
        ScreenCoverPresetDBSO presetDb)
    {
        _spec = spec;
        _screenEffects = screenEffects;
        _presetDb = presetDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ScreenCoverEffectController controller = _screenEffects.Cover;

        if (controller == null)
        {
            Debug.LogWarning("[ScreenCoverCommand] 화면 효과 리그에 덮개(Cover_Image)가 없다. 건너뛴다.", _screenEffects);
            yield break;
        }

        ScreenCoverEffectController.State target = BuildTarget(controller);

        // 걷혀 있던 덮개는 새 색에서 덮기 시작한다 — 지난 색에서 옮겨 가며 덮이지 않게.
        if (!_spec.clear && controller.Presented.Amount <= 0f)
            controller.Snap(new ScreenCoverEffectController.State(target.Color, 0f));

        controller.Follow(target, _spec.duration, _spec.ease);

        if (!_spec.wait)
            yield break;

        while (controller.IsTransitioning)
        {
            if (scope.Token.IsCancellationRequested)
                yield break;

            if (scope.ShouldCompressCommandExecution)
            {
                controller.Snap(target);
                yield break;
            }

            yield return null;
        }
    }

    protected override void OnSkip()
    {
        ScreenCoverEffectController controller = _screenEffects.Cover;

        if (controller == null)
            return;

        controller.Snap(BuildTarget(controller));
    }

    private ScreenCoverEffectController.State BuildTarget(ScreenCoverEffectController controller)
    {
        if (_spec.clear)
            return new ScreenCoverEffectController.State(controller.Color, 0f);

        if (_presetDb.TryGet(_spec.presetKey, out ScreenCoverPresetDBSO.Entry e))
            return new ScreenCoverEffectController.State(e.color, 1f);

        Debug.LogWarning(
            $"[ScreenCoverCommand] Cover preset not found. presetKey='{_spec.presetKey}'. " +
            $"'{ScreenCoverPresetDBSO.DefaultPresetKey}'로 덮는다.",
            controller);

        return _presetDb.TryGet(ScreenCoverPresetDBSO.DefaultPresetKey, out e)
            ? new ScreenCoverEffectController.State(e.color, 1f)
            : new ScreenCoverEffectController.State(Color.black, 1f);
    }
}
