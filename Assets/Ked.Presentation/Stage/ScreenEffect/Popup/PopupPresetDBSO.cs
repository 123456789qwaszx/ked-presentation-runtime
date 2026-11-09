using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 그림 팝업 프리셋 — 게임마다 하나. 그림 배율 · 뒷막 색과 세기 · 등장 · 퇴장 페이드(docs/PLAN_popup.md §4).
/// 그림 · 자리는 대본이 정한다(<<popup 무대 그림 x y [배율]>>). 런타임에서 편집하는 값이다.
///
/// 기본값은 원본 게임 실측이다(2026-11-09, 아미야가 녹화한 GIF 둘 — 등장 · 퇴장, 1240×680, 약 70ms 간격).
/// 자는 배경이다: 그 장면의 원본 배경(BG_uniseed_basement_night)을 GIF에 맞대면 0.637 GIF px = 캔버스 1px.
/// 그림(Story_element_qstory_main_06_003, 404×404)을 같은 자로 재면 원본 픽셀의 0.91배이고 화면 가운데(오차 0.3px)다.
/// 페이드 동안 크기 · 자리는 그대로이고(프레임마다 배율 0.90~1.10을 맞춰 1.00) 불투명도만 움직인다.
/// 뒷막 세기는 프레임마다 정확히 "팝업 불투명도 × 0.47"이다 — 한 그룹의 페이드(StagePopupSlot).
///
/// 페이드 키의 시각 0은 "마지막으로 변하지 않은 프레임과 처음 변한 프레임의 가운데"로 잡았다(±35ms는 모른다).
/// GIF 간격이 거칠다 — 원본 영상(30fps)에서 다시 재면 바꾼다.
/// </summary>
[CreateAssetMenu(menuName = "Ked/Presentation/Popup Preset", fileName = "PopupPreset")]
public sealed class PopupPresetDBSO : ScriptableObject
{
    /// <summary>그림의 Resources 뿌리. 대본의 그림 키 = 이 아래 스프라이트 이름.</summary>
    public const string PictureResourcesRoot = "Popups";

    [Header("Picture")]
    [Tooltip("그림 원본 픽셀에 곱하는 배율. 대본의 배율이 또 곱해진다. 원본 실측 0.91(±0.005).")]
    public float pictureScale = 0.91f;

    [Header("Backdrop")]
    [Tooltip("화면 전체 막의 색. 알파가 세기다 — 그룹 불투명도가 1일 때의 세기. 원본 실측: 거의 검정, 0.47(채널별 0.44~0.49).")]
    public Color backdropColor = new(0.04f, 0.04f, 0.04f, 0.47f);

    [Header("Fade (그룹 불투명도, 초)")]
    [Tooltip("등장 — 시각(초). 첫 값 0, 오름차순.")]
    public float[] openTimes = { 0f, 0.035f, 0.105f, 0.175f, 0.245f, 0.315f, 0.385f, 0.445f };

    [Tooltip("등장 — 불투명도. 0에서 1로.")]
    public float[] openAlpha = { 0f, 0.07f, 0.42f, 0.53f, 0.75f, 0.93f, 0.97f, 1f };

    [Tooltip("퇴장 — 시각(초). 첫 값 0, 오름차순.")]
    public float[] closeTimes = { 0f, 0.035f, 0.095f, 0.165f, 0.235f, 0.305f };

    [Tooltip("퇴장 — 불투명도. 1에서 0으로.")]
    public float[] closeAlpha = { 1f, 0.92f, 0.76f, 0.51f, 0.23f, 0f };

    /// <summary>에셋이 없을 때의 기본값 — 조립이 경고하고 쓴다.</summary>
    public static PopupPresetDBSO CreateDefault()
    {
        PopupPresetDBSO preset = CreateInstance<PopupPresetDBSO>();
        preset.name = "PopupPreset (코드 기본값)";
        preset.hideFlags = HideFlags.DontSave;
        return preset;
    }

    /// <summary>등장 곡선(불투명도 0 → 1). 값이 규칙에 어긋나면 null과 이유.</summary>
    public CurveKey[] OpenKeys(out string why) => Keys(openTimes, openAlpha, 0f, 1f, out why);

    /// <summary>퇴장 곡선(불투명도 1 → 0). 값이 규칙에 어긋나면 null과 이유.</summary>
    public CurveKey[] CloseKeys(out string why) => Keys(closeTimes, closeAlpha, 1f, 0f, out why);

    // 규칙: 시각과 값의 개수가 같고 둘 이상, 첫 시각 0, 시각은 오름차순, 첫 값 · 끝 값이 페이드의 양끝.
    private static CurveKey[] Keys(float[] times, float[] values, float from, float to, out string why)
    {
        if (times == null || values == null || times.Length < 2 || times.Length != values.Length)
        {
            why = "시각과 값은 개수가 같고 둘 이상이어야 한다";
            return null;
        }

        if (times[0] != 0f)
        {
            why = $"첫 시각은 0이어야 한다 ({times[0]})";
            return null;
        }

        for (int i = 1; i < times.Length; i++)
        {
            if (!(times[i] > times[i - 1]))
            {
                why = $"시각은 오름차순이어야 한다 ({times[i - 1]} → {times[i]})";
                return null;
            }
        }

        if (!Mathf.Approximately(values[0], from) || !Mathf.Approximately(values[values.Length - 1], to))
        {
            why = $"불투명도는 {from}에서 시작해 {to}로 끝나야 한다 ({values[0]} → {values[values.Length - 1]})";
            return null;
        }

        why = null;
        return PresetCurveKeys.Smooth(times, values);
    }
}
