using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using C = Ked.Samples.UI.SaveSlotCard.Refs;
using H = Ked.Samples.UI.CharactersPage.Refs;
using R = Ked.Samples.UI.RecordPanel.Refs;
using S = Ked.Samples.UI.SaveSlotsPage.Refs;
using T = Ked.Samples.UI.TipsPage.Refs;

namespace Ked.Samples.UI.Editor
{
    // 기록 — 바깥(Panel)과 탭 안(Page 넷)을 따로 만든다. 틀은 옛 저장 화면(05)의 것, 탭 한 칸이 넷으로.
    // Page 프리팹은 Panel 프리팹의 PageRoot 아래에 중첩 프리팹으로 들어간다(UIPF: Page는 Owner의 PageRoot 자식이어야 한다).
    // 팁 · 인물은 고정 내용이라 여기 상수로 둔다(글자 표에 넣지 않는다).
    public static class RecordSpec
    {
        // 탭 안 영역(PageRoot). Page 좌표는 모두 이 안의 절대 좌표로 적는다. 탭 줄 오른쪽(쪽 표시)부터 아래 버튼까지.
        public static readonly Rect PageArea = new(64, 140, 1472, 740);

        private static readonly string[] TabLabels = { "저장", "불러오기", "팁", "캐릭터" };

        // 인물(카드 순서). 설정 › 음량의 캐릭터별 음성도 이 목록을 쓴다. 그림은 portrait_{id} · face_{id}.
        public static readonly (string Id, string Name)[] Characters =
        {
            ("harin", "하린"),
            ("sigure", "시구레"),
            ("kasmi", "카스미"),
        };

        private const string CharacterProfile = "인물 소개 문구가 들어갈 자리입니다.\n두 줄까지 보여 줍니다.";

        // 팁(목록 순서). 키는 빈칸으로 나눈다. 없으면 "".
        private static readonly (string Icon, string Title, string Body, string Keys, string Where)[] Tips =
        {
            ("auto", "자동 진행",
                "대사가 모두 표시되고 정한 시간이 지나면 다음 대사로 넘어갑니다.\nA 키나 빠른 메뉴의 자동 버튼으로 켜고 끕니다.",
                "A", "설정 › 재생 ›\n자동 진행 대기"),
            ("fast_forward", "빠르게 재생",
                "S 키로 켜고 끕니다. Ctrl 키는 누르는 동안만 빨리 넘깁니다.\n배속은 다음에 켤 때부터 적용됩니다.",
                "S Ctrl", "설정 › 재생 ›\n빠르게 재생"),
            ("backlog", "대사 기록",
                "지나간 대사를 다시 읽습니다.\n음성이 있는 줄은 다시 듣고, 줄을 골라 그 장면으로 돌아갈 수 있습니다.",
                "", "빠른 메뉴 ›\n대사 기록"),
            ("rollback", "한 줄 되감기",
                "R 키로 바로 앞 대사로 돌아갑니다.",
                "R", "—"),
            ("hide_ui", "UI 숨기기",
                "장면만 보고 싶을 때 대사창과 메뉴를 숨깁니다.\nEsc 키를 누르거나 화면을 누르면 다시 보입니다.",
                "Esc", "빠른 메뉴 ›\nUI 숨기기"),
            ("save", "기록 남기기",
                "메뉴 › 저장에서 지금 장면을 수동 슬롯에 남깁니다.\n불러오면 이어하기 지점이 불러온 기록으로 바뀝니다.",
                "", "메뉴 ›\n저장 · 불러오기"),
            ("settings", "미리 맞춰 두기",
                "음량 · 대사 · 재생 · 화면을 미리 맞춰 둡니다.\n바꾼 설정은 즉시 적용되고 저장됩니다.",
                "", "타이틀 · 메뉴 ›\n설정"),
        };

        private static readonly Color Cream = KedColors.Hex(0xFAF6EC);
        private static readonly Color CardLine = KedColors.Hex(0x10243C);
        private static readonly Color PictureBack = KedColors.Hex(0xBEE0F9);

        public static GameObject Build(KedUIKit kit, IReadOnlyList<GameObject> pagePrefabs)
        {
            UINode view = kit.NewView<RecordPanel>(nameof(RecordPanel));
            kit.Backdrop(view, nameof(R.Background), "background_title");

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, nameof(R.Frame));

            kit.IconButton(frame, nameof(R.Back), "back", new Rect(64, 38, 58, 58), padding: 15f);
            kit.Label(frame, nameof(R.RecordTitle), "기록", 144, 37, 700, 56, 36, KedColors.Navy, bold: true);
            kit.Label(frame, nameof(R.RecordSubtitle), "", 146, 96, 1100, 32, 17, KedColors.Muted);
            kit.IconButton(frame, nameof(R.Close), "close", new Rect(1478, 38, 58, 58), padding: 16f);

            for (int i = 0; i < TabLabels.Length; i++)
            {
                float x = 100 + i * 262;
                kit.Tab(frame, "RecordTab_" + i, "RecordTabText_" + i, TabLabels[i],
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

        public static GameObject SaveTab(KedUIKit kit) =>
            SlotsTab<SavePage>(kit, "선택한 슬롯에 현재 장면을 저장합니다.", "이 슬롯에 저장");

        public static GameObject LoadTab(KedUIKit kit) =>
            SlotsTab<LoadPage>(kit, "선택한 기록에서 이야기를 이어갑니다.", "불러오기");

        // 05 · 06 — 슬롯 카드 여섯은 GridLayoutGroup(3열, 440×190, 간격 40×48)에 놓인 같은 카드(SaveSlotCard)다.
        private static GameObject SlotsTab<TPage>(KedUIKit kit, string hint, string action) where TPage : SaveSlotsPage
        {
            UINode page = kit.NewView<TPage>(typeof(TPage).Name, PageArea);

            kit.Label(page, nameof(S.PageInfo), "PAGE  01 / 01", 1160, 161, 340, 34, 18, KedColors.Muted,
                TextAlignmentOptions.Right);

            UINode grid = kit.Node(page, nameof(S.SlotGrid), 100, 248, 1400, 428);
            GridLayoutGroup layout = grid.Go.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(440, 190);
            layout.spacing = new Vector2(40, 48);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            for (int i = 0; i < SaveSlotsPage.SlotsPerPage; i++)
            {
                int col = i % 3;
                int row = i / 3;
                BuildCard(kit, grid, "Slot_" + (i + 1), new Rect(100 + 480 * col, 248 + 238 * row, 440, 190));
            }

            kit.Label(page, nameof(S.SlotHint), hint, 104, 744, 950, 38, 19, KedColors.Muted);

            kit.IconButton(page, nameof(S.PreviousPage), "back", new Rect(650, 807, 48, 48), padding: 12f);
            kit.Label(page, nameof(S.CurrentPage), "1", 719, 816, 240, 30, 18, KedColors.Navy,
                TextAlignmentOptions.Center, bold: true);
            kit.IconButton(page, nameof(S.NextPage), "next", new Rect(934, 807, 48, 48), padding: 12f);

            kit.TextButton(page, nameof(S.SlotAction), nameof(S.SlotActionLabel), action,
                new Rect(1180, 785, 320, 70), KedButtonStyle.Primary);

            return page.Go;
        }

        // 카드 하나. 안의 이름은 카드(SaveSlotCard)가 제 안에서만 찾는다.
        private static void BuildCard(KedUIKit kit, UINode grid, string name, Rect d)
        {
            UINode card = kit.Node(grid, name, d);
            card.Go.AddComponent<SaveSlotCard>();

            float x = d.x;
            float y = d.y;

            // 카드 전체가 버튼 — save_slot 그림 그대로(밝은 자리 = 그림 창의 알려진 자리 그림).
            UINode surface = kit.ImageButton(card, nameof(C.CardButton), "save_slot");

            UINode thumb = kit.Node(surface, nameof(C.CardThumbnail), x + 13, y + 12, 414, 100);

            UINode number = kit.Node(thumb, nameof(C.CardNumber), x + 26, y + 21, 64, 28);
            kit.Surface(number, KedColors.Navy, radius: 14f);
            UINode numberText = kit.Fill(number, nameof(C.CardNumberText));
            kit.Text(numberText, "01", 14, KedColors.White, TextAlignmentOptions.Center, bold: true);

            kit.Label(thumb, nameof(C.CardChapter), "", x + 102, y + 22, 310, 26, 15, KedColors.Navy, bold: true,
                overflow: TextOverflowModes.Ellipsis);
            kit.Label(thumb, nameof(C.CardPlayTime), "", x + 200, y + 82, 215, 24, 14, KedColors.Navy,
                TextAlignmentOptions.Right);

            kit.Label(surface, nameof(C.CardTitle), "", x + 24, y + 122, 392, 27, 20, KedColors.Navy, bold: true,
                overflow: TextOverflowModes.Ellipsis);
            kit.Label(surface, nameof(C.CardDate), "", x + 24, y + 154, 360, 24, 14, KedColors.Muted);
            kit.Label(surface, nameof(C.CardEmpty), "빈 슬롯", x + 26, y + 60, 380, 42, 24, KedColors.Muted,
                TextAlignmentOptions.Center);
            kit.Label(surface, nameof(C.CardEmptyHint), "", x + 24, y + 145, 380, 25, 15, KedColors.Muted,
                TextAlignmentOptions.Center);

            UINode selected = kit.Fill(surface, nameof(C.CardSelected), -2f, -2f, -2f, -2f);
            kit.Outline(selected, KedColors.Turquoise, 3f);
            selected.Go.SetActive(false);
        }

        // 왼쪽 목록 · 오른쪽 본문. 본문은 팁마다 하나씩 만들어 두고 고른 것만 켠다.
        public static GameObject TipsTab(KedUIKit kit)
        {
            UINode page = kit.NewView<TipsPage>(nameof(TipsPage), PageArea);

            UINode list = kit.Node(page, nameof(T.TipList), 100, 232, 470, 600);
            Panel(kit, list, "TipListLine");

            for (int i = 0; i < Tips.Length; i++)
            {
                var tip = Tips[i];
                float y = 250 + i * 80;

                UINode row = kit.RowButton(list, "TipRow_" + i, new Rect(114, y, 442, 68));

                UINode iconBase = kit.Node(row, "TipRowIconBase_" + i, 136, y + 12, 44, 44);
                kit.Surface(iconBase, KedColors.CurrentRow, radius: 14f);
                UINode icon = kit.Node(row, "TipRowIcon_" + i, 144, y + 20, 28, 28);
                kit.Image(icon, "icon_" + tip.Icon, preserveAspect: true);

                kit.Label(row, "TipRowNumber_" + i, KedUIUtil.TwoDigits(i + 1), 198, y, 40, 68, 15, KedColors.Muted, bold: true);
                kit.Label(row, "TipRowTitle_" + i, tip.Title, 240, y, 300, 68, 21, KedColors.Navy, bold: true);

                if (i < Tips.Length - 1)
                    kit.Solid(kit.Node(list, "TipRowLine_" + i, 136, y + 75, 404, 1), KedColors.Divider);
            }

            UINode detail = kit.Node(page, nameof(T.TipDetail), 600, 232, 900, 600);
            Panel(kit, detail, "TipDetailLine");

            for (int i = 0; i < Tips.Length; i++)
                BuildTipDetail(kit, detail, i);

            return page.Go;
        }

        private static void BuildTipDetail(KedUIKit kit, UINode parent, int i)
        {
            var tip = Tips[i];
            UINode d = kit.Node(parent, "TipDetail_" + i, 600, 232, 900, 600);

            UINode iconBase = kit.Node(d, "TipIconBase_" + i, 640, 272, 104, 104);
            kit.Surface(iconBase, KedColors.CurrentRow, radius: 24f);
            kit.Image(kit.Node(d, "TipIcon_" + i, 662, 294, 60, 60), "icon_" + tip.Icon, preserveAspect: true);

            kit.Label(d, "TipNumber_" + i, "TIP  " + KedUIUtil.TwoDigits(i + 1), 772, 278, 300, 28, 16, KedColors.Turquoise, bold: true);
            kit.Label(d, "TipTitle_" + i, tip.Title, 770, 306, 680, 56, 38, KedColors.Navy, bold: true);
            kit.Solid(kit.Node(d, "TipLine_" + i, 640, 408, 820, 2), KedColors.Divider);
            kit.Label(d, "TipBody_" + i, tip.Body, 640, 428, 820, 110, 21, KedColors.Navy, wrap: true);

            // 그림 자리 — 그림이 오면 이 틀 안에 둔다. 지금은 팁 아이콘을 흐리게.
            UINode picture = kit.Node(d, "TipPicture_" + i, 640, 548, 480, 270);
            kit.Surface(picture, KedColors.Divider, radius: 14f);
            kit.Image(kit.Node(picture, "TipPictureIcon_" + i, 840, 643, 80, 80), "icon_" + tip.Icon,
                KedColors.WithAlpha(KedColors.Navy, 0.25f), preserveAspect: true);

            kit.Label(d, "TipKeysCaption_" + i, "관련 키", 1150, 556, 200, 30, 16, KedColors.Muted, bold: true);

            string[] keys = string.IsNullOrEmpty(tip.Keys) ? new string[0] : tip.Keys.Split(' ');
            if (keys.Length == 0)
                kit.Label(d, "TipKeysNone_" + i, "—", 1150, 592, 64, 36, 19, KedColors.Navy);

            for (int k = 0; k < keys.Length; k++)
            {
                float w = keys[k].Length > 1 ? 88 : 64;
                UINode cap = kit.Node(d, $"TipKey_{i}_{k}", 1150 + k * 100, 592, w, 36);
                kit.Surface(cap, KedColors.Ivory, radius: 10f);
                kit.Outline(kit.Fill(cap, $"TipKeyLine_{i}_{k}"), KedColors.SurfaceLine, 1f);
                kit.Text(kit.Fill(cap, $"TipKeyText_{i}_{k}"), keys[k], 17, KedColors.Navy, TextAlignmentOptions.Center, bold: true);
            }

            kit.Label(d, "TipWhereCaption_" + i, "관련 설정", 1150, 660, 200, 30, 16, KedColors.Muted, bold: true);
            kit.Label(d, "TipWhere_" + i, tip.Where, 1150, 694, 320, 70, 19, KedColors.Navy, wrap: true);

            d.Go.SetActive(i == 0);
        }

        // 인물 카드 셋. 켜기/끄기의 id는 Page에 적어 둔다(카드 순서).
        public static GameObject CharactersTab(KedUIKit kit)
        {
            UINode page = kit.NewView<CharactersPage>(nameof(CharactersPage), PageArea);

            for (int i = 0; i < CharactersPage.CardCount && i < Characters.Length; i++)
            {
                var c = Characters[i];
                float x = 100 + 480 * i, y = 232, w = 440, h = 516;

                UINode card = kit.Node(page, "CharacterCard_" + i, x, y, w, h);
                kit.Surface(card, Cream, radius: 22f);
                kit.Outline(kit.Fill(card, "CharacterCardLine_" + i), CardLine, 4f);

                UINode window = kit.Node(card, "CharacterWindow_" + i, x + 14, y + 14, w - 28, 316);
                kit.Surface(window, PictureBack, radius: 14f);
                window.Go.AddComponent<Mask>().showMaskGraphic = true;
                kit.Image(kit.Fill(window, "CharacterPortrait_" + i), "portrait_" + c.Id);

                kit.Label(card, "CharacterName_" + i, c.Name, x + 28, y + 344, 300, 46, 30, KedColors.Navy, bold: true);
                kit.Label(card, "CharacterProfile_" + i, CharacterProfile, x + 28, y + 392, w - 56, 56, 16, KedColors.Muted, wrap: true);
                kit.Solid(kit.Node(card, "CharacterDivider_" + i, x + 28, y + 456, w - 56, 2), KedColors.Divider);

                kit.Image(kit.Node(card, "CharacterVoiceIcon_" + i, x + 28, y + 470, 30, 30), "icon_voice", preserveAspect: true);
                kit.Label(card, "CharacterVoiceLabel_" + i, "음성", x + 68, y + 466, 120, 38, 20, KedColors.Navy, bold: true);
                kit.Switch(card, "CharacterVoice_" + i, "CharacterVoice" + i, new Rect(x + w - 28 - 90, y + 466, 90, 40));
            }

            kit.Label(page, nameof(H.CharactersHint), "여기서 켜고 끈 음성은 설정 › 음량 › 캐릭터별 음성과 같은 값입니다.",
                104, 768, 1000, 34, 17, KedColors.Muted);

            var so = new SerializedObject(page.Go.GetComponent<CharactersPage>());
            SerializedProperty ids = so.FindProperty("characterIds");
            ids.arraySize = Mathf.Min(CharactersPage.CardCount, Characters.Length);
            for (int i = 0; i < ids.arraySize; i++)
                ids.GetArrayElementAtIndex(i).stringValue = Characters[i].Id;
            so.ApplyModifiedPropertiesWithoutUndo();

            return page.Go;
        }

        // 옅은 상아 판 + 가는 테두리(팁 목록 · 본문).
        private static void Panel(KedUIKit kit, UINode node, string lineName)
        {
            kit.Surface(node, KedColors.WithAlpha(KedColors.Ivory, 0.96f), radius: 18f);
            kit.Outline(kit.Fill(node, lineName), KedColors.SurfaceLine, 1f);
        }

        #endregion
    }
}
