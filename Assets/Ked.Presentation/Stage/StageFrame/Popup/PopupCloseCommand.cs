using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class PopupCloseCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = "stage01";
}

// popup_close — 무대 슬롯에 퇴장 페이드를 걸기만 한다. 끝에서 슬롯이 닫힘으로 둔다(StagePopupSlot.PlayClose).
public sealed class PopupCloseCommand : CommandBase
{
    private readonly PopupCloseCommandSpec _spec;
    private readonly PopupPresetDBSO _preset;
    private readonly StageFrameRegistry _frames;

    private StagePopupSlot _slot;
    private CurveKey[] _keys;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public PopupCloseCommand(
        PopupCloseCommandSpec spec,
        PopupPresetDBSO preset,
        StageFrameRegistry frames)
    {
        _spec = spec;
        _preset = preset;
        _frames = frames;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasPopupToClose())
            yield break;

        _slot.PlayClose(_keys);

        if (!_spec.wait)
            yield break;

        while (_slot.IsFading)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasPopupToClose())
            return;

        _slot.SnapClose();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _slot = _frames.Popup(_spec.stage);
        _keys = _preset.CloseKeys(out string why);

        if (_keys == null)
            Debug.LogWarning($"[PopupCloseCommand] 퇴장 곡선이 유효하지 않다 ({_preset.name}): {why}. 끝값만 쓴다.");
    }

    // 열린 팝업은 실행 시점에 본다 — 앞 줄의 popup이 아직 페이드 중이어도 열림이다.
    private bool HasPopupToClose()
    {
        if (_slot.IsOpen)
            return true;

        Debug.LogWarning($"[PopupCloseCommand] {_spec.stage}에 열린 그림 팝업이 없다 — 건너뛴다.");
        return false;
    }
}
