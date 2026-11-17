using UnityEngine;

/// <summary>
/// 그림 팝업 프리셋 — 게임마다 하나. 그림 배율과 페이드 길이(docs/PLAN_stage-visibility.md §3.3).
/// 그림 · 자리는 대본이 정한다(<<popup 무대 그림 x y [배율]>>). 파서가 popup · popup_close를 그림 리그로 풀 때 읽는다 —
/// 런타임에서 편집하면 다음 popup부터 반영된다.
///
/// 팝업은 그림 하나다. 뒤를 어둡게 까는 것은 대본(연출 카드)이 screen_dim으로 한다 — 프리셋에 뒷막은 없다.
/// 원본을 똑같이 재현하지 않는다(소유자, 2026-11-17) — 페이드는 곡선이 아니라 길이 하나다.
/// 그림 배율 0.91은 원본에서 잰 값이다(배경을 자로 — docs/PLAN_popup.md §1).
/// </summary>
[CreateAssetMenu(menuName = "Ked/Presentation/Popup Preset", fileName = "PopupPreset")]
public sealed class PopupPresetDBSO : ScriptableObject
{
    /// <summary>그림의 Resources 뿌리. 대본의 그림 키 = 이 아래 스프라이트 이름.</summary>
    public const string PictureResourcesRoot = "Popups";

    public const float DefaultPictureScale = 0.91f;
    public const float DefaultOpenSeconds = 0.45f;
    public const float DefaultCloseSeconds = 0.3f;

    [Header("Picture")]
    [Tooltip("그림 원본 픽셀에 곱하는 배율. 대본의 배율이 또 곱해진다. 원본 실측 0.91.")]
    public float pictureScale = DefaultPictureScale;

    [Header("Fade (초)")]
    [Tooltip("popup — 그림이 0에서 1로 나타나는 길이.")]
    public float openSeconds = DefaultOpenSeconds;

    [Tooltip("popup_close — 그림이 1에서 0으로 사라지는 길이.")]
    public float closeSeconds = DefaultCloseSeconds;
}
