using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 화면 밖의 UIPF·연출 데이터 자산. 한 번 만들면 다시 만들지 않는다(사람이 고친 값을 빌드가 덮지 않게) —
    /// 글꼴 묶음만 매번 글꼴을 다시 건다.
    /// 자리: 글꼴 묶음은 Common/Fonts, Presentation과 대사창 배치 DB는 그 화면의 묶음(KedUIUnits.Rules).
    /// Generated/는 묶음에 들지 않은 화면이 있을 때만 쓴다(지금은 없다).
    /// </summary>
    internal static class KedUIAssets
    {
        public const string OutputDir = KedUIKit.Root + "/Generated";
        public const string PresentationDir = OutputDir + "/Presentations";
        public const string FontSetPath = KedUIFonts.FontDir + "/KedUIFontSet.asset";
        public const string TextsPath = KedUIKit.Root + "/Texts/KedUITexts.csv";

        public static readonly Type[] ViewTypes =
        {
            typeof(TitleScreen),
            typeof(PlayScreen),
            typeof(SystemMenuPanel),
            typeof(SaveLoadPanel),
            typeof(BacklogPanel),
            typeof(SettingsPanel),
            typeof(AudioSettingsPage),
            typeof(TextSettingsPage),
            typeof(PlaybackSettingsPage),
            typeof(DisplaySettingsPage),
            typeof(ConfirmPanel),
            typeof(NoticePanel),
        };

        /// <summary>UIPF 공용 글꼴 묶음(UIThemeAssets.Fonts). 언어 줄마다 같은 글꼴 — Noto Sans KR은 라틴 글자도 갖는다.</summary>
        public static UIFontSet FontSet(TMP_FontAsset font)
        {
            string path = FontSetPath;
            UIFontSet set = AssetDatabase.LoadAssetAtPath<UIFontSet>(path);

            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UIFontSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.locales = new[]
            {
                new UIFontSet.LocaleFonts { localeId = "ko-KR", body = font },
                new UIFontSet.LocaleFonts { localeId = "en-US", body = font },
            };

            EditorUtility.SetDirty(set);
            return set;
        }

        /// <summary>화면마다 Presentation 하나(presentationId = 화면 키). 지금은 비어 있다 — 테마·변형을 더할 자리.</summary>
        public static UIPresentationSpec[] Presentations()
        {
            var specs = new List<UIPresentationSpec>(ViewTypes.Length);

            foreach (Type type in ViewTypes)
            {
                string key = UIViewKey.Of(type);
                // 화면이 묶음에 있으면 Screens/{묶음}/Presentation/{key}Presentation.asset, 아니면 Generated/Presentations/{key}.asset.
                // 없을 때만 만든다(presentationId = 화면 키, 오브젝트 이름 = 파일 이름) — UIPF UIPresentationAssets.
                string path = KedUIUnits.Rules.PresentationPath(type, key, PresentationDir);
                specs.Add(UIPresentationAssets.Ensure(type, path));
            }

            return specs.ToArray();
        }

        /// <summary>
        /// 새 대사창에 맞춘 연출 프리셋 DB. 키·종류(kind)는 옛 DB와 같고, 자리만 새 박스(1400×210 · 이름표)에 맞췄다.
        /// 비율은 대사창 판(1600×900)에 대한 것 — 시안 좌표를 그대로 나눈 값이다.
        /// </summary>
        public static DialogueSurfaceLayoutPresetDBSO SurfaceLayoutDb(List<string> problems)
        {
            // 재생 화면의 대사창에 맞춘 데이터라 재생 묶음에 둔다(Screens/Play/Data).
            string path = KedUIUnits.Rules.DataPath(typeof(PlayScreen), "KedSurfaceLayoutPresetDB.asset", OutputDir);
            var db = AssetDatabase.LoadAssetAtPath<DialogueSurfaceLayoutPresetDBSO>(path);
            if (db != null)
                return db;

            db = ScriptableObject.CreateInstance<DialogueSurfaceLayoutPresetDBSO>();

            DialogueSurfaceLayoutPresetDBSO.Entry bottom = DialogueSurfaceLayoutPresetDBSO.Entry.CreateBottom();
            ApplyBox(ref bottom, line: new Rect(156, 693, 1230, 106), name: new Rect(156, 610, 250, 40), lineSize: 27f, nameSize: 26f);

            DialogueSurfaceLayoutPresetDBSO.Entry speaker = DialogueSurfaceLayoutPresetDBSO.Entry.CreateSpeakerBottom();
            ApplyBox(ref speaker, line: new Rect(156, 693, 1230, 106), name: new Rect(156, 610, 250, 40), lineSize: 27f, nameSize: 26f);

            // 초상 컷인: 왼쪽을 비운 좁은 글 자리(옛 프리셋처럼 이름표 없음).
            DialogueSurfaceLayoutPresetDBSO.Entry portrait = DialogueSurfaceLayoutPresetDBSO.Entry.CreatePortraitNarrow();
            ApplyBox(ref portrait, line: new Rect(512, 693, 874, 106), name: new Rect(512, 610, 250, 40), lineSize: 26f, nameSize: 24f);

            // 레터박스: 박스 안 한 줄, 세로 가운데.
            DialogueSurfaceLayoutPresetDBSO.Entry letterbox = DialogueSurfaceLayoutPresetDBSO.Entry.CreateLetterboxBottom();
            ApplyBox(ref letterbox, line: new Rect(156, 693, 1230, 106), name: new Rect(156, 610, 250, 40), lineSize: 24f, nameSize: 24f);
            letterbox.lineAlignment = TextAlignmentOptions.Left;

            // 화면 전체 글·책 쪽: 옛 비율 그대로, 글자 크기만 새 기준(27/36)으로.
            DialogueSurfaceLayoutPresetDBSO.Entry full = DialogueSurfaceLayoutPresetDBSO.Entry.CreateFullTopLeft();
            full.lineFontSize = 24f;
            full.nameFontSize = 24f;

            DialogueSurfaceLayoutPresetDBSO.Entry book = DialogueSurfaceLayoutPresetDBSO.Entry.CreateBlackBookPage();
            book.lineFontSize = 26f;
            book.nameFontSize = 24f;

            DialogueSurfaceLayoutPresetDBSO.Entry[] entries = { bottom, speaker, portrait, letterbox, full, book };

            FieldInfo field = typeof(DialogueSurfaceLayoutPresetDBSO).GetField(
                "entries",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                problems.Add("DialogueSurfaceLayoutPresetDBSO.entries 칸을 찾지 못했다 — 기본 프리셋(옛 자리)으로 만든다.");
            }
            else
            {
                field.SetValue(db, entries);
            }

            UIPrefabs.EnsureFolder(UIPrefabs.DirectoryOf(path));
            AssetDatabase.CreateAsset(db, path);
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void ApplyBox(
            ref DialogueSurfaceLayoutPresetDBSO.Entry entry,
            Rect line,
            Rect name,
            float lineSize,
            float nameSize)
        {
            entry.overrideImage = false;

            entry.lineAnchorMin = AnchorMin(line);
            entry.lineAnchorMax = AnchorMax(line);
            entry.linePivot = new Vector2(0.5f, 0.5f);
            entry.lineAnchoredPosition = Vector2.zero;
            entry.lineSizeDelta = Vector2.zero;
            entry.lineAlignment = TextAlignmentOptions.TopLeft;
            entry.lineFontSize = lineSize;
            entry.lineSpacing = 18f;
            entry.paragraphSpacing = 0f;
            entry.lineMargin = Vector4.zero;

            entry.nameAnchorMin = AnchorMin(name);
            entry.nameAnchorMax = AnchorMax(name);
            entry.namePivot = new Vector2(0.5f, 0.5f);
            entry.nameAnchoredPosition = Vector2.zero;
            entry.nameSizeDelta = Vector2.zero;
            entry.nameAlignment = TextAlignmentOptions.Center;
            entry.nameFontSize = nameSize;
            entry.nameMargin = Vector4.zero;
        }

        // 시안 좌표(위 원점) → 판(1600×900)에 대한 앵커 비율(아래 원점).
        private static Vector2 AnchorMin(Rect r) => new(r.x / 1600f, (900f - r.y - r.height) / 900f);
        private static Vector2 AnchorMax(Rect r) => new((r.x + r.width) / 1600f, (900f - r.y) / 900f);
    }
}
