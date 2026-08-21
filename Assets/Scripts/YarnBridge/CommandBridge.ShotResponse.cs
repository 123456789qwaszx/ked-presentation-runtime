using UnityEngine;

public sealed partial class YarnCommandBridge
{
    // shot 계열도 place·size와 같은 모양이다: 마지막 위치 인자가 이징이고,
    // 미지정이면 스펙 기본값(OutCubic), "@이름"이면 curves.json의 커스텀 곡선.
    // 해석은 ResolveEase 한 자리를 함께 쓴다.

    private void EnqueueShotZoomFocusSpec(
        string roleKey,
        string focusName = "body",
        string screenPointName = "center",
        float zoom = 2.5f,
        string durationToken = "1.2s",
        string easeToken = "")
    {
        CharacterFocusPresetParser.TryParse(focusName, out CharacterFocusPreset focusPreset);

        if (!ScreenFocusPointParser.TryParse(screenPointName, out ScreenFocusPoint screenPoint))
            screenPoint = ScreenFocusPoint.Center;

        EaseSelection ease = ResolveEase(easeToken);

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screenPoint,
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        };

        Collect(spec);
    }

    private void EnqueueShotToSpec(
        float zoom = 1f,
        string xToken = "2.5u",
        string yToken = "0u",
        string durationToken = "0.45s",
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new ShotToCommandSpec
        {
            zoom = zoom,
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }

    private void EnqueueShotZoomSpec(
        float zoom = 1f,
        string durationToken = "0.45s",
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new ShotZoomCommandSpec
        {
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }

    private void EnqueueShotTrackSpec(
        string xToken = "2.5u",
        string yToken = "0u",
        string durationToken = "0.35s",
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new ShotTrackCommandSpec
        {
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }

    private void EnqueueShotResetSpec(
        string durationToken = "0.3s",
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new ShotResetCommandSpec
        {
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }
}
