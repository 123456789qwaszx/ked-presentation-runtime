using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using A = Ked.Samples.UI.AlbumPage.Refs;
using G = Ked.Samples.UI.CGPage.Refs;
using R = Ked.Samples.UI.ExtraPanel.Refs;

namespace Ked.Samples.UI.Editor
{
    // 감상 — 바깥(Panel)과 탭 안(Page 둘). 틀은 기록과 같다(옛 저장 화면의 틀, 탭 줄).
    // 해금 판정은 하지 않는다 — 앨범 · CG의 내용과 잠김 · 열림은 여기 상수로 고정한다. 그림은 예제 배경을 줄인 자리 그림.
    public static class ExtraSpec
    {
        public static readonly Rect PageArea = new(64, 140, 1472, 740);

        private static readonly string[] TabLabels = { "앨범", "CG" };

        // 앨범 — 지나온 장소. 그림은 album_{이름}.
        private static readonly (string Picture, string Title, string Chapter, bool Open)[] Album =
        {
            ("shop", "골동품 상점", "1장", true),
            ("hanok", "한옥", "1장", true),
            ("class_day", "교실 · 낮", "2장", true),
            ("class_lunch", "교실 · 점심", "2장", true),
            ("festival", "작년 축제", "3장", true),
            ("staffroom", "교무실 · 저녁", "3장", false),
            ("studio", "작업실 · 저녁", "3장", false),
            ("studio", "작업실 · 밤", "4장", false),
        };

        // CG — 배경(album_{이름}) 위 인물(portrait_{id}). 자리 그림이다.
        private static readonly (string Picture, string Character, bool Open)[] CG =
        {
            ("shop", "harin", true), ("hanok", "sigure", true), ("class_day", "harin", true),
            ("class_lunch", "sigure", true), ("festival", "kasmi", true), ("staffroom", "harin", true),
            ("studio", "kasmi", false), ("studio", "sigure", true), ("hanok", "harin", false),
            ("festival", "sigure", false), ("shop", "kasmi", true), ("class_day", "kasmi", false),
            ("staffroom", "sigure", false), ("hanok", "kasmi", false), ("festival", "harin", false),
        };

        private static readonly Color Cream = KedColors.Hex(0xFAF6EC);
        private static readonly Color CardLine = KedColors.Hex(0x10243C);

        public static GameObject Build(KedUIKit kit, IReadOnlyList<GameObject> pagePrefabs)
        {
            UINode view = kit.NewView<ExtraPanel>(nameof(ExtraPanel));
            kit.Backdrop(view, nameof(R.Background), "background_title");

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, nameof(R.Frame));

            kit.IconButton(frame, nameof(R.Back), "back", new Rect(64, 38, 58, 58), padding: 15f);
            kit.Label(frame, nameof(R.ExtraTitle), "감상", 144, 37, 700, 56, 36, KedColors.Navy, bold: true);
            kit.Label(frame, nameof(R.ExtraSubtitle), "", 146, 96, 1100, 32, 17, KedColors.Muted);

            for (int i = 0; i < TabLabels.Length; i++)
            {
                float x = 100 + i * 262;
                kit.Tab(frame, "ExtraTab_" + i, "ExtraTabText_" + i, TabLabels[i],
                    new Rect(x, 149, 250, 54),
                    new Rect(x + 15, 157, 210, 35));
            }

            UINode pageRoot = kit.Node(frame, nameof(R.PageRoot), PageArea, UIAnchor.TopLeft);

            foreach (GameObject prefab in pagePrefabs)
            {
                if (prefab == null)
                    continue;

                var page = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                page.transform.SetParent(pageRoot.Rect, false);
                page.SetActive(false);
            }

            return view.Go;
        }

        #region Pages

        // 4열 × 2줄 카드. 열린 카드는 그림 · 이름 · 장, 잠긴 카드는 자물쇠 · "???".
        public static GameObject AlbumTab(KedUIKit kit)
        {
            UINode page = kit.NewView<AlbumPage>(nameof(AlbumPage), PageArea);

            kit.Label(page, nameof(A.AlbumInfo), OpenCount(Album.Count(a => a.Open), Album.Length),
                1160, 161, 340, 34, 18, KedColors.Muted, TextAlignmentOptions.Right);

            UINode grid = kit.Node(page, nameof(A.AlbumGrid), 100, 236, 1400, 502);

            for (int i = 0; i < Album.Length; i++)
            {
                var a = Album[i];
                float w = 326, h = 240;
                float x = 100 + (i % 4) * 358, y = 236 + (i / 4) * 262;

                UINode card = kit.Node(grid, "AlbumCard_" + i, x, y, w, h);
                kit.Surface(card, Cream, radius: 18f);
                kit.Outline(kit.Fill(card, "AlbumCardLine_" + i), CardLine, 3f);

                UINode window = kit.Node(card, "AlbumWindow_" + i, x + 13, y + 13, w - 26, 166);
                kit.Surface(window, KedColors.Divider, radius: 10f);
                window.Go.AddComponent<Mask>().showMaskGraphic = true;

                if (a.Open)
                {
                    Picture(kit, window, "AlbumPicture_" + i, "album_" + a.Picture);
                    kit.Label(card, "AlbumTitle_" + i, a.Title, x + 20, y + 186, 220, 40, 20, KedColors.Navy, bold: true);
                    kit.Label(card, "AlbumChapter_" + i, a.Chapter, x + w - 90, y + 186, 70, 40, 15, KedColors.Muted,
                        TextAlignmentOptions.Right, bold: true);
                }
                else
                {
                    Lock(kit, window, "AlbumLock_" + i, new Rect(x + w / 2 - 24, y + 66, 48, 48));
                    kit.Label(window, "AlbumLockedNote_" + i, "아직 열리지 않았습니다", x + 13, y + 120, w - 26, 26, 15,
                        KedColors.Muted, TextAlignmentOptions.Center);
                    kit.Label(card, "AlbumTitle_" + i, "???", x + 20, y + 186, 220, 40, 20, KedColors.Muted, bold: true);
                }
            }

            return page.Go;
        }

        // 5열 × 3줄 16:9 칸. 번호는 늘, 잠긴 칸은 자물쇠.
        public static GameObject CGTab(KedUIKit kit)
        {
            UINode page = kit.NewView<CGPage>(nameof(CGPage), PageArea);

            kit.Label(page, nameof(G.CGInfo), OpenCount(CG.Count(c => c.Open), CG.Length),
                1160, 161, 340, 34, 18, KedColors.Muted, TextAlignmentOptions.Right);

            UINode grid = kit.Node(page, nameof(G.CGGrid), 100, 236, 1400, 482);

            for (int i = 0; i < CG.Length; i++)
            {
                var c = CG[i];
                float w = 260, h = 146;
                float x = 100 + (i % 5) * 285, y = 236 + (i / 5) * 168;

                UINode tile = kit.Node(grid, "CGTile_" + i, x, y, w, h);
                kit.Surface(tile, KedColors.Divider, radius: 14f);
                tile.Go.AddComponent<Mask>().showMaskGraphic = true;

                if (c.Open)
                {
                    Picture(kit, tile, "CGPicture_" + i, "album_" + c.Picture);
                    kit.Image(kit.Node(tile, "CGCharacter_" + i, x + 80, y + 8, 191, 146), "portrait_" + c.Character);
                }
                else
                {
                    Lock(kit, tile, "CGLock_" + i, new Rect(x + w / 2 - 20, y + h / 2 - 22, 40, 40));
                }

                kit.Outline(kit.Fill(tile, "CGTileLine_" + i), CardLine, 3f);

                UINode number = kit.Node(tile, "CGNumber_" + i, x + 12, y + h - 40, 52, 26);
                kit.Surface(number, KedColors.Navy, radius: 13f);
                kit.Text(kit.Fill(number, "CGNumberText_" + i), KedUIUtil.TwoDigits(i + 1), 13, KedColors.White,
                    TextAlignmentOptions.Center, bold: true);
            }

            return page.Go;
        }

        // 창을 비율 유지로 덮는 그림(넘치는 쪽은 창의 Mask가 자른다).
        private static void Picture(KedUIKit kit, UINode window, string name, string sprite)
        {
            UINode picture = kit.Fill(window, name);
            kit.Image(picture, sprite);
            AspectRatioFitter fitter = picture.Go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
        }

        private static void Lock(KedUIKit kit, UINode parent, string name, Rect rect)
        {
            kit.Image(kit.Node(parent, name, rect), "icon_lock", KedColors.WithAlpha(KedColors.White, 0.75f), preserveAspect: true);
        }

        // "열림  5 / 8" — 고정 내용에서 센다.
        private static string OpenCount(int open, int total) => $"열림  {open} / {total}";

        #endregion
    }
}
