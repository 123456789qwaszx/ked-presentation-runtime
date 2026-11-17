using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

// 화면 흔들림 — 카메라 리그의 Camera_Shake 노드에 모션 클립을 튼다.
//
// 흔들림은 의도가 아니다. 구도(카메라의 축) 위에 얹혔다가 0으로 돌아오는 연기이고, 끝값이 없다:
//   - 클립은 camera 리그의 restore 모션이다(MotionRules가 보장한다 — Camera_Shake에만 키, end는 restore).
//   - 노드는 언제나 항등에서 시작해 항등으로 끝난다. 그래서 정지 프레임(폴드)이 접을 것이 없고,
//     하네스는 정착한 프레임에서 이 노드가 항등인지를 본다.
//   - 샘플링은 코어 MotionMath 그대로다. 랜덤이 없어 툴 프리뷰와 런타임이 같은 흔들림을 낸다.
//
// 한 번에 하나만 돈다. 새 흔들림이 오면 앞의 것을 0으로 끊고 시작한다(섞인 흔들림을 남기지 않는다).
public sealed class CameraShakePlayer
{
    private readonly RectTransform _node;

    private Tween _tween;

    public CameraShakePlayer(RectTransform shakeNode)
    {
        _node = shakeNode;
    }

    public bool IsPlaying => _tween != null;

    /// <summary>클립을 처음부터 한 번 튼다. 세기는 키 값(중립에서 벗어난 만큼)에 곱한다.</summary>
    public void Play(MotionClip clip, float intensity)
    {
        Stop();

        if (clip == null || !(clip.Duration > 0f) || clip.Channels == null || clip.Channels.Length == 0)
            return;

        _tween = DOTween
            .To(() => 0f, seconds => Apply(clip, intensity, seconds), clip.Duration, clip.Duration)
            .SetEase(Ease.Linear)
            .SetTarget(_node)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _tween = null;
                ResetNode();
            });
    }

    /// <summary>지금 흔들림을 끊고 노드를 항등으로 돌린다. 압축 · 스텝 경계 · 장면 경계.</summary>
    public void Stop()
    {
        _tween?.Kill(false);
        _tween = null;

        ResetNode();
    }

    // 노드가 항등에서 시작하므로 세기를 곱한 원시 값이 곧 노드에 쓰는 값이다(MotionMath.SampleScaled).
    private void Apply(MotionClip clip, float intensity, float seconds)
    {
        for (int i = 0; i < clip.Channels.Length; i++)
        {
            Vector3 value = MotionMath.SampleScaled(clip.Channels[i], seconds, intensity).ToUnity();

            switch (clip.Channels[i].Kind)
            {
                case MotionChannelKind.Position: _node.anchoredPosition = value; break;
                case MotionChannelKind.Scale: _node.localScale = new Vector3(value.x, value.y, 1f); break;
                case MotionChannelKind.Euler: _node.localEulerAngles = value; break;
            }
        }
    }

    private void ResetNode()
    {
        _node.anchoredPosition = Vector2.zero;
        _node.localScale = Vector3.one;
        _node.localEulerAngles = Vector3.zero;
    }
}
