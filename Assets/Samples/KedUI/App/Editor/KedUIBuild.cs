using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// Tools/Ked UI/Build Sample UI — 화면 빌드 명세(Screens/*/Editor/*Spec)에서 UIPF 화면 프리팹을 만든다.
    ///
    /// 이것은 앱 빌드다(KedUI/App) — 화면 목록과 순서, 재생 런타임에 매인 것(선택지 항목 · 대사창 배치 DB), 캔버스를 안다.
    /// 화면 묶음(KedUI의 Common · Screens)은 재생 런타임을 모르고, 그 명세는 묶음의 Editor에 있다.
    ///
    /// 원본과 산출물(docs/PLAN_uipf-ui.md "제작 원칙"):
    ///   원본     화면 스크립트(Screens/*/Scripts) · 빌드 명세(Screens/*/Editor) · 그림 · 부품 키트(Common)
    ///   산출물   화면 프리팹(Screens/*/Prefabs) · 선택지 항목(App/Prefabs) · 캔버스(App/Resources/KedUI) · 글꼴 SDF — 빌드마다 덮어쓴다.
    ///            손으로 고치지 않는다(다음 빌드에서 사라진다). 고칠 것은 명세 · 키트에서 고친다.
    ///   처음 한 번 만드는 데이터   Presentation · 대사창 배치 DB — 있으면 빌드가 건드리지 않는다(에셋에서 고쳐도 된다).
    ///            글꼴 묶음은 글꼴 칸만 빌드마다 다시 건다.
    ///   산출물도 커밋한다 — 빌드하지 않고 클론만 해도 실행되게.
    ///
    ///   1. 그림 임포트 설정(KedUIArtImporter, 패키지의 도구 — 그림 자리를 이름으로 찾게만 고쳤다)
    ///   2. TMP 글꼴(Noto Sans KR Medium · Bold, 동적 SDF)
    ///   3. 화면 프리팹 12개(Root 2 · Panel 6 · Page 4) — 자식 이름 = 화면의 Refs
    ///   4. 선택지 항목 프리팹 · 연출 프리셋 DB · UIPF 글꼴 묶음 · 화면별 Presentation
    ///   5. 캔버스 프리팹(App/Resources/KedUI/KedUICanvas) — 층 셋에 화면을 중첩 프리팹으로 놓고 KedUIHost에 잇는다
    ///   6. 검사(KedUIValidate — Refs · Presentation 이름 · 묶음). 메뉴 Validate Sample UI로 빌드 없이 따로도 돈다.
    ///
    /// 생성물 자리는 묶음 규칙(KedUIUnits): 화면 스크립트가 Screens/{묶음}/Scripts/에 있으면 그 묶음 안,
    /// 아니면 Generated/(지금은 그런 화면이 없다).
    /// 다시 실행하면 프리팹은 같은 경로에 덮어쓴다(GUID 유지). 같은 명세면 내용도 같다 — 다만 이름이 겹치는 자식
    /// (저장/불러오기의 슬롯 카드 여섯 장)이 있는 프리팹은 파일 안 fileID가 빌드마다 바뀐다(UIPF UIPrefabs).
    /// 플레이 중에는 빌드하지 않는다 — 실행 중에만 붙는 컴포넌트가 섞여 저장된다.
    /// 씬은 건드리지 않는다 — SampleBootstrap이 Resources에서 캔버스를 읽는다.
    /// 자리 계산 · 저장 · Presentation · 글꼴 · 검사의 도구는 UIPF(Editor/UI/Authoring)의 것이다.
    ///
    /// 배치 모드: -executeMethod Ked.Samples.UI.Editor.KedUIBuild.BuildBatch (문제가 있으면 종료 코드 1).
    /// Unity가 이 프로젝트를 열고 있으면 배치 모드는 프로젝트를 열지 못한다.
    /// </summary>
    public static class KedUIBuild
    {
        public static string AppDir => KedUIUnits.Root + "/App";
        public static string ScreensDir => KedUIUnits.Root + "/Generated/Screens";
        public static string CanvasDir => AppDir + "/Resources/KedUI";
        public static string CanvasPath => CanvasDir + "/KedUICanvas.prefab";

        // 선택지 항목은 본체의 VNOptionItem이라 앱 쪽에 둔다(재생 쪽 선택지 표시자가 이 프리팹으로 항목을 만든다).
        public static string OptionItemPath => AppDir + "/Prefabs/KedOptionItem.prefab";

        [MenuItem("Tools/Ked UI/Build Sample UI")]
        public static void BuildFromMenu()
        {
            bool ok = Build(out string report);

            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);

            EditorUtility.DisplayDialog(
                "Ked UI",
                ok ? "샘플 UI를 만들었습니다. 콘솔에 보고가 있습니다." : "문제가 있습니다. 콘솔의 보고를 확인하세요.",
                "확인");
        }

        /// <summary>배치 모드용: -executeMethod Ked.Samples.UI.Editor.KedUIBuild.BuildBatch</summary>
        public static void BuildBatch()
        {
            bool ok = Build(out string report);
            Debug.Log(report);

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build(out string report)
        {
            var problems = new List<string>();
            var built = new List<string>();

            if (!UIPrefabs.CanBuild(out string reason))
            {
                report = "[Ked UI] 샘플 UI를 빌드하지 않았다 — " + reason;
                return false;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Ked UI", "그림 임포트 설정", 0.05f);
                KedUIArtImporter.Apply();

                EditorUtility.DisplayProgressBar("Ked UI", "TMP 글꼴", 0.15f);
                var font = KedUIFonts.Ensure(problems);

                var kit = new KedUIKit(font);

                UIPrefabs.EnsureFolder(CanvasDir);

                EditorUtility.DisplayProgressBar("Ked UI", "화면", 0.3f);

                GameObject optionItem = Save(OptionItemSpec.Build(kit), OptionItemPath, built);

                GameObject[] pages =
                {
                    SaveScreen(SettingsSpec.AudioPage(kit), built),
                    SaveScreen(SettingsSpec.TextPage(kit), built),
                    SaveScreen(SettingsSpec.PlaybackPage(kit), built),
                    SaveScreen(SettingsSpec.DisplayPage(kit), built),
                };

                GameObject[] roots =
                {
                    SaveScreen(TitleSpec.Build(kit), built),
                    SaveScreen(PlaySpec.Build(kit), built),
                };

                GameObject[] panels =
                {
                    SaveScreen(MenuSpec.Build(kit), built),
                    SaveScreen(SaveLoadSpec.Build(kit), built),
                    SaveScreen(BacklogSpec.Build(kit), built),
                    SaveScreen(SettingsSpec.Build(kit, pages), built),
                    SaveScreen(ModalSpec.Confirm(kit), built),
                    SaveScreen(ModalSpec.Notice(kit), built),
                };

                EditorUtility.DisplayProgressBar("Ked UI", "데이터 자산", 0.75f);

                UIFontSet fontSet = KedUIAssets.FontSet(font);
                UIPresentationSpec[] presentations = KedUIAssets.Presentations();
                DialogueSurfaceLayoutPresetDBSO surfaceDb = KedUIAssets.SurfaceLayoutDb(problems);
                TextAsset texts = AssetDatabase.LoadAssetAtPath<TextAsset>(KedUIAssets.TextsPath);
                if (texts == null)
                    problems.Add($"UI 글자 표 없음: {KedUIAssets.TextsPath}");

                EditorUtility.DisplayProgressBar("Ked UI", "캔버스", 0.85f);

                GameObject canvas = BuildCanvas(roots, panels, fontSet, texts, presentations, optionItem, surfaceDb);
                Save(canvas, CanvasPath, built);

                problems.AddRange(kit.Problems);

                // 화면을 세우며 채운 글자를 비운 채로 저장한다 — 커밋되는 글꼴 자산을 작게 둔다.
                EditorUtility.DisplayProgressBar("Ked UI", "글꼴 아틀라스 비우기", 0.9f);
                KedUIFonts.ClearDynamicData(problems);

                AssetDatabase.SaveAssets();

                // 저장한 결과를 검사한다 — 메뉴 Validate Sample UI와 같은 검사.
                EditorUtility.DisplayProgressBar("Ked UI", "검사", 0.95f);
                built.Add($"(검사: {KedUIValidate.Run(problems)})");
            }
            catch (Exception error)
            {
                problems.Add("빌드 중단: " + error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // 그림 이름 겹침은 키트와 검사가 같은 것을 두 번 올릴 수 있다.
            problems = problems.Distinct().ToList();

            var sb = new StringBuilder();
            sb.AppendLine(problems.Count == 0 ? "[Ked UI] 샘플 UI 빌드 완료" : $"[Ked UI] 샘플 UI 빌드 — 문제 {problems.Count}개");
            sb.AppendLine("만든 것:");
            foreach (string path in built)
                sb.AppendLine("  " + path);

            if (problems.Count > 0)
            {
                sb.AppendLine("문제:");
                foreach (string problem in problems)
                    sb.AppendLine("  - " + problem);
            }

            report = sb.ToString();
            return problems.Count == 0;
        }

        #region 캔버스

        private static GameObject BuildCanvas(
            GameObject[] roots,
            GameObject[] panels,
            UIFontSet fontSet,
            TextAsset texts,
            UIPresentationSpec[] presentations,
            GameObject optionItem,
            DialogueSurfaceLayoutPresetDBSO surfaceDb)
        {
            var go = new GameObject("KedUICanvas", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            // 옛 UI 캔버스(정렬 0) 위, 디버그 캔버스(34 · 999) 아래.
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            // 시안 1600×900. Expand — 어떤 화면비에서도 1600×900 판 전체가 들어온다(남는 쪽은 가장자리로 늘어난다).
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;

            go.AddComponent<GraphicRaycaster>();

            KedUIHost host = go.AddComponent<KedUIHost>();
            go.AddComponent<UIDisplayRefreshDriver>();

            RectTransform rootLayer = Layer(go.transform, "RootLayer");
            RectTransform panelLayer = Layer(go.transform, "PanelLayer");
            RectTransform overlayLayer = Layer(go.transform, "OverlayLayer");

            foreach (GameObject prefab in roots)
                Place(prefab, rootLayer);

            foreach (GameObject prefab in panels)
                Place(prefab, panelLayer);

            var so = new SerializedObject(host);
            so.FindProperty("rootLayer").objectReferenceValue = rootLayer;
            so.FindProperty("panelLayer").objectReferenceValue = panelLayer;
            so.FindProperty("overlayLayer").objectReferenceValue = overlayLayer;
            so.FindProperty("fontSet").objectReferenceValue = fontSet;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("optionItemPrefab").objectReferenceValue = optionItem != null ? optionItem.GetComponent<VNOptionItem>() : null;
            so.FindProperty("surfaceLayoutDb").objectReferenceValue = surfaceDb;

            SerializedProperty list = so.FindProperty("presentations");
            list.arraySize = presentations.Length;
            for (int i = 0; i < presentations.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = presentations[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        private static RectTransform Layer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Place(GameObject prefab, RectTransform layer)
        {
            if (prefab == null)
                return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(layer, false);
            instance.SetActive(false);
        }

        #endregion

        #region 저장

        private static GameObject SaveScreen(GameObject go, List<string> built)
        {
            UIBase view = go.GetComponent<UIBase>();
            string path = KedUIUnits.Rules.PrefabPath(view != null ? view.GetType() : null, go.name, ScreensDir);
            return Save(go, path, built);
        }

        private static GameObject Save(GameObject go, string path, List<string> built)
        {
            GameObject prefab = UIPrefabs.Save(go, path);
            built.Add(path);
            return prefab;
        }

        #endregion
    }
}
