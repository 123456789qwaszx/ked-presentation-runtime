using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class PopupOpenCommandSpec : CommandSpecBase
{
    [Header("Target")]
    [Tooltip("덮는 무대(stage01 · stage02).")]
    public string stage = "stage01";

    [Header("Picture")]
    [Tooltip("Resources/Popups/{키}")]
    public string pictureKey;

    [Tooltip("그림 중심. 무대 중심 기준 px, y 위.")]
    public Vector2 center;

    [Tooltip("프리셋 배율에 곱한다.")]
    public float scale = 1f;
}

// popup — 프리셋과 그림을 골라 무대 슬롯에 등장 페이드를 걸기만 한다. 페이드는 슬롯이 소유한다(StagePopupSlot.PlayOpen).
public sealed class PopupOpenCommand : CommandBase
{
    private readonly PopupOpenCommandSpec _spec;
    private readonly PopupPresetDBSO _preset;
    private readonly StageRigRegistry _stages;

    private StagePopupSlot _slot;
    private Sprite _picture;
    private CurveKey[] _keys;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public PopupOpenCommand(
        PopupOpenCommandSpec spec,
        PopupPresetDBSO preset,
        StageRigRegistry stages)
    {
        _spec = spec;
        _preset = preset;
        _stages = stages;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        _slot.PlayOpen(_preset, _picture, _spec.center, _spec.scale, _keys);

        if (!_spec.wait)
            yield break;

        while (_slot.IsFading)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        _slot.SnapOpen(_preset, _picture, _spec.center, _spec.scale);
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _slot = _stages.Get(_spec.stage).Popup;

        // 못 찾으면 해석기가 경고한다 — 뒷막만 뜬다(폴드는 열림).
        _picture = ResourcesSpriteResolver.Resolve(PopupPresetDBSO.PictureResourcesRoot, _spec.pictureKey);

        _keys = _preset.OpenKeys(out string why);

        if (_keys == null)
            Debug.LogWarning($"[PopupOpenCommand] 등장 곡선이 유효하지 않다 ({_preset.name}): {why}. 끝값만 쓴다.");
    }
}
