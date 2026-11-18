using UnityEngine;

/// <summary>
/// 대사창 초상의 값 — 카메라가 찍는 곳 · 범위와 초상 칸의 자리 · 크기(PortraitFrame). 값의 집은 이 에셋 하나다.
/// 에셋이라 재생 중에 고친 값이 그대로 남고, 촬영소 · 초상 칸이 프레임마다 다시 읽어 고치는 즉시 화면에 보인다.
/// 조립에 연결하지 않으면 코드 기본값으로 열고 경고한다(그때는 고친 값이 남지 않는다).
/// </summary>
[CreateAssetMenu(menuName = "Ked/Presentation/Portrait Frame Preset", fileName = "PortraitFramePreset")]
public sealed class PortraitFramePresetDBSO : ScriptableObject
{
    public PortraitFrame frame = PortraitFrame.Default;

    /// <summary>쓸 수 있는 값으로 메운 구도 — 인스펙터에서 고치는 도중의 값(범위 0 등)을 그대로 내지 않는다.</summary>
    public PortraitFrame Frame => frame.Sanitized();
}
