using TMPro;
using UnityEngine;
using R = Ked.Samples.UI.EpisodeEndPanel.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 14 끝 카드 — 시안 없음(패키지 밖에서 더한 화면). 모달과 같은 딤 위에 상아색 카드 하나.
    /// 그림은 키트의 공용 그림만 쓴다(면 · 단색 · 버튼) — 이 묶음에는 Sprites/가 없다.
    /// </summary>
    public static class EpisodeEndSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<EpisodeEndPanel>(nameof(EpisodeEndPanel));

            UINode dim = kit.Fill(view, nameof(R.EndDim));
            kit.Solid(dim, KedColors.Dim, raycast: true);

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, "EndFrame");

            UINode card = kit.Node(frame, nameof(R.EndCard), 480, 210, 640, 470);
            kit.Surface(card, KedColors.Ivory, radius: 18f, raycast: true);

            TextMeshProUGUI kicker = kit.Label(frame, nameof(R.EndKicker), "에피소드 끝", 520, 250, 560, 30, 20,
                KedColors.Turquoise, TextAlignmentOptions.Center, bold: true, overflow: TextOverflowModes.Ellipsis);
            kicker.characterSpacing = 6f;

            UINode accent = kit.Node(frame, nameof(R.EndAccent), 770, 288, 60, 4);
            kit.Solid(accent, KedColors.Turquoise);

            kit.Label(frame, nameof(R.EndTitle), "—", 520, 304, 560, 56, 36, KedColors.Navy,
                TextAlignmentOptions.Center, bold: true, overflow: TextOverflowModes.Ellipsis);

            UINode divider = kit.Node(frame, nameof(R.EndDivider), 560, 378, 480, 2);
            kit.Solid(divider, KedColors.Divider);

            UINode body = kit.Node(frame, nameof(R.EndBody), 520, 396, 560, 60);
            kit.Text(body, "", 22, KedColors.Muted, TextAlignmentOptions.Center, wrap: true, lineSpacing: 8f);

            kit.Label(frame, nameof(R.EndResultHeading), "결과", 520, 470, 560, 28, 18, KedColors.Navy,
                TextAlignmentOptions.Center, bold: true);

            UINode result = kit.Node(frame, nameof(R.EndResult), 520, 502, 560, 70);
            kit.Text(result, "", 22, KedColors.Navy, TextAlignmentOptions.Center, wrap: true, lineSpacing: 8f);

            kit.TextButton(frame, nameof(R.EndTitleReturn), nameof(R.EndTitleReturnLabel), "타이틀로",
                new Rect(660, 590, 280, 66), KedButtonStyle.Primary);

            return view.Go;
        }
    }
}
