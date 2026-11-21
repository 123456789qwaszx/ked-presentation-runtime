using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // shot 계열도 size와 같은 모양이다: 마지막 위치 인자가 이징이고,
    // 미지정이면 스펙 기본값(OutCubic). 해석은 YarnEaseParser 한 자리를 함께 쓴다.
    //
    // 어휘: shot_<축>은 그 축 하나를 절대값으로 놓는다(줌 · 팬 · 초점 · 롤 · 달리 · 트랙). 묶음은 없다 — 여러 축을 함께 놓으려면
    // 나란히 적는다(전환이 축마다 따로라 서로 덮지 않는다). 되돌리기는 shot_reset(렌즈만) · shot_reset_all(통째로)이다.
    // 인물의 초점을 화면 지점에 오게 하는 렌즈는 도구가 풀어서 shot_focus · shot_zoom · shot_pan으로 적는다(코어 ShotIntents.FocusTo).
    // shot_shake는 축이 아니다 — 구도 위에 얹혔다가 0으로 돌아오는 흔들림 클립을 튼다.
    //
    // 값 인자에는 생략값이 없다. 빠졌거나 읽지 못하면 소리 내고 아무것도 내지 않는다 — 대신 갈 뜻 있는 값이 없다
    // (motion의 모르는 이름과 같다). 코어 폴드도 같은 줄을 Unhandled로 남긴다. 생략할 수 있는 것은 길이 · 이징뿐이다.

    private void EnqueueShotZoomSpec(StageCommand c, string zoomToken, string durationToken, string easeToken)
    {
        if (!TryShotNumber(c, zoomToken, "zoom", out float zoom))
            return;

        Emit(new ShotZoomCommandSpec
        {
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    private void EnqueueShotPanSpec(StageCommand c, string xToken, string yToken, string durationToken, string easeToken)
    {
        if (!TryShotUnits(c, xToken, yToken, out Vector2 pan))
            return;

        Emit(new ShotPanCommandSpec
        {
            pan = pan,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    // shot_focus(x, y, dur, ease) — 렌즈의 초점(무대 좌표). 줌할 때 앞 · 뒤 층이 벌어지는 쪽의 기준이다.
    private void EnqueueShotFocusSpec(StageCommand c, string xToken, string yToken, string durationToken, string easeToken)
    {
        if (!TryShotUnits(c, xToken, yToken, out Vector2 focus))
            return;

        Emit(new ShotFocusCommandSpec
        {
            focus = focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    private void EnqueueShotTrackSpec(StageCommand c, string xToken, string yToken, string durationToken, string easeToken)
    {
        if (!TryShotUnits(c, xToken, yToken, out Vector2 track))
            return;

        Emit(new ShotTrackCommandSpec
        {
            track = track,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    private void EnqueueShotDollySpec(StageCommand c, string dollyToken, string durationToken, string easeToken)
    {
        if (!TryShotNumber(c, dollyToken, "dolly", out float dolly))
            return;

        Emit(new ShotDollyCommandSpec
        {
            dolly = dolly,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    private void EnqueueShotRollSpec(StageCommand c, string rollToken, string durationToken, string easeToken)
    {
        if (!TryShotNumber(c, rollToken, "roll", out float roll))
            return;

        Emit(new ShotRollCommandSpec
        {
            roll = roll,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    private void EnqueueShotResetSpec(bool all, string durationToken, string easeToken)
    {
        Emit(new ShotResetCommandSpec
        {
            all = all,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken),
        });
    }

    // shot_shake(이름, 세기) — motions.json의 카메라 리그 클립을 그대로 싣는다. 길이는 클립이 안다.
    // 모르는 이름 · 다른 리그의 클립은 소리 내고 아무것도 내지 않는다 — 대신 틀 기본 흔들림이 없다(motion과 같다).
    private void EnqueueShotShakeSpec(string shakeName, float intensity)
    {
        if (!_motions.TryGet(shakeName, out MotionClip clip))
        {
            Debug.LogError(
                $"[StageCommandParser] Unknown shake '{shakeName}' — {MotionLibrary.BundleFileName}에 없다. 이 커맨드는 재생하지 않는다.");
            return;
        }

        if (clip.Rig != MotionRules.CameraRig)
        {
            Debug.LogError(
                $"[StageCommandParser] shot_shake '{shakeName}'은 '{clip.Rig}' 리그의 모션이다 — 화면 흔들림은 '{MotionRules.CameraRig}' 리그의 클립이어야 한다. 이 커맨드는 재생하지 않는다.");
            return;
        }

        Emit(new ShotShakeCommandSpec { clip = clip, intensity = intensity });
    }

    // 값 인자 하나(숫자). 생략값이 없다 — 못 읽으면 소리 내고 false.
    private static bool TryShotNumber(StageCommand c, string token, string what, out float value)
    {
        if (token != null && YarnNumberParser.TryParseFloat(token, out value))
            return true;

        value = 0f;

        Debug.LogError(token == null
            ? $"[StageCommandParser] <<{c.Name}>>에 {what} 값이 없다 — 값 인자는 꼭 적는다. 이 커맨드는 실행하지 않는다."
            : $"[StageCommandParser] <<{c.Name}>>의 {what}이 숫자가 아니다 ('{token}'). 이 커맨드는 실행하지 않는다.");

        return false;
    }

    // 값 인자 둘(x · y, 부호 있는 단위 토큰). 코어 폴드와 같은 해석(UnitToken)이다.
    private static bool TryShotUnits(StageCommand c, string xToken, string yToken, out Vector2 value)
    {
        value = Vector2.zero;

        if (xToken == null || yToken == null)
        {
            Debug.LogError(
                $"[StageCommandParser] <<{c.Name}>>에 x · y 값이 없다 — 값 인자는 꼭 적는다. 이 커맨드는 실행하지 않는다.");
            return false;
        }

        if (!UnitToken.TryParseSignedPixels(xToken, UnitToken.DefaultReferenceStageWidth, out float x) ||
            !UnitToken.TryParseSignedPixels(yToken, UnitToken.DefaultReferenceStageWidth, out float y))
        {
            Debug.LogError(
                $"[StageCommandParser] <<{c.Name}>>의 x · y를 읽지 못했다 ('{xToken}' '{yToken}'). 이 커맨드는 실행하지 않는다.");
            return false;
        }

        value = new Vector2(x, y);
        return true;
    }
}
