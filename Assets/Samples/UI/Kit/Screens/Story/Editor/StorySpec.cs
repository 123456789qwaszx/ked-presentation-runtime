using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Ked.Samples.UI.StoryChapterCard.Refs;
using R = Ked.Samples.UI.StoryPanel.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 스토리 — 챕터 카드가 가로로 늘어선 화면. 틀은 감상과 같다(타이틀 배경 · 뒤로 · 제목).
    /// 카드는 가로 ScrollRect 안의 HorizontalLayoutGroup에 선다. 카드 견본(ChapterCardTemplate)은 숨겨 두고 화면이 복제한다.
    /// 카드는 고정된 네모 상자다(300×480) — 번호 · 그림 자리(지금은 번호만) · 이름 · 한 줄 정보.
    /// </summary>
    public static class StorySpec
    {
        private static readonly Rect ScrollArea = new(64, 170, 1472, 600);
        private static readonly Rect Card = new(100, 230, 300, 480);

        private static readonly Color Cream = KedColors.Hex(0xFAF6EC);
        private static readonly Color CardLine = KedColors.Hex(0x10243C);

        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<StoryPanel>(nameof(StoryPanel));
            kit.Backdrop(view, nameof(R.Background), "background_title");

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, nameof(R.Frame));

            kit.IconButton(frame, nameof(R.Back), "back", new Rect(64, 38, 58, 58), padding: 15f);
            kit.Label(frame, nameof(R.StoryTitle), "스토리", 144, 37, 700, 56, 36, KedColors.Navy, bold: true);
            kit.Label(frame, nameof(R.StorySubtitle), "", 146, 96, 1100, 32, 17, KedColors.Muted);

            UINode scroll = kit.Node(frame, nameof(R.ChapterScroll), ScrollArea);
            kit.Clear(scroll);
            ScrollRect scrollRect = scroll.Go.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 60f;
            scrollRect.inertia = true;

            UINode viewport = kit.Fill(scroll, nameof(R.ChapterViewport));
            viewport.Go.AddComponent<RectMask2D>();

            // 내용: 왼쪽에 붙고 세로는 보이는 칸을 채운다. 너비는 카드 수에 맞춰 자란다.
            UINode content = kit.Node(viewport, nameof(R.ChapterContent),
                new Rect(ScrollArea.x, ScrollArea.y, 0, ScrollArea.height), UIAnchor.TopLeft);
            content.Rect.anchorMin = new Vector2(0f, 0f);
            content.Rect.anchorMax = new Vector2(0f, 1f);
            content.Rect.pivot = new Vector2(0f, 0.5f);
            content.Rect.sizeDelta = Vector2.zero;
            content.Rect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = content.Go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 0, 0);
            layout.spacing = 36f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.Go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.Rect;
            scrollRect.content = content.Rect;

            BuildCard(kit, content);

            kit.Label(frame, nameof(R.StoryEmpty), "스토리 폴더에 챕터(*.progression.json)가 없습니다.",
                100, 420, 1400, 40, 21, KedColors.Muted, TextAlignmentOptions.Center);

            return view.Go;
        }

        // 카드: 상아색 둥근 상자 전체가 버튼. 위에서부터 번호 · 그림 자리 · 이름(두 줄까지) · 한 줄 정보.
        private static void BuildCard(KedUIKit kit, UINode content)
        {
            float x = Card.x, y = Card.y, w = Card.width, h = Card.height;

            UINode card = kit.Node(content, nameof(R.ChapterCardTemplate), Card);
            card.Go.AddComponent<StoryChapterCard>();

            // 안의 내용은 버튼 아래에 둔다 — 누름 연출이 함께 움직인다.
            UINode button = kit.RowButton(card, nameof(C.ChapterButton), Card, radius: 18f);

            kit.Surface(kit.Fill(button, nameof(C.ChapterSurface)), Cream, radius: 18f);
            kit.Outline(kit.Fill(button, nameof(C.ChapterLine)), CardLine, 3f);

            kit.Label(button, nameof(C.ChapterIndex), "00", x + 20, y + 14, w - 40, 34, 20, KedColors.Muted,
                TextAlignmentOptions.Center, bold: true);

            UINode art = kit.Node(button, nameof(C.ChapterArt), x + 20, y + 56, w - 40, 300);
            kit.Surface(art, KedColors.Divider, radius: 12f);
            kit.Text(kit.Fill(art, nameof(C.ChapterArtNumber)), "00", 96, KedColors.WithAlpha(KedColors.Navy, 0.35f),
                TextAlignmentOptions.Center, bold: true);

            kit.Label(button, nameof(C.ChapterName), "", x + 20, y + 368, w - 40, 62, 20, KedColors.Navy,
                TextAlignmentOptions.Center, bold: true, wrap: true, overflow: TextOverflowModes.Ellipsis);
            kit.Label(button, nameof(C.ChapterInfo), "", x + 20, y + 438, w - 40, 26, 15, KedColors.Muted,
                TextAlignmentOptions.Center);

            card.Go.SetActive(false);
        }
    }
}
