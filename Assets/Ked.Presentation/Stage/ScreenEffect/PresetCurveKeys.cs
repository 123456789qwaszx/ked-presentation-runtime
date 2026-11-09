using System;
using Ked.Presentation.Core;

/// <summary>
/// 잰 점을 지나는 매끈한 곡선 — 원본 GIF · 영상에서 잰 값(시각, 값)을 키로 만든다.
/// 접선은 이웃 두 점의 기울기(Catmull-Rom), 양끝은 0. 컷인 창 모션과 그림 팝업 페이드가 같이 쓴다.
/// </summary>
public static class PresetCurveKeys
{
    public static CurveKey[] Smooth(float[] t, float[] v)
    {
        if (t == null || v == null)
            throw new ArgumentNullException(t == null ? nameof(t) : nameof(v));

        if (t.Length != v.Length)
            throw new ArgumentException("시각과 값의 개수가 다르다.");

        CurveKey[] keys = new CurveKey[t.Length];

        for (int i = 0; i < t.Length; i++)
        {
            float tangent = i == 0 || i == t.Length - 1
                ? 0f
                : (v[i + 1] - v[i - 1]) / (t[i + 1] - t[i - 1]);

            keys[i] = new CurveKey(t[i], v[i], tangent, tangent);
        }

        return keys;
    }
}
