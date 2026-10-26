using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// Tools/Ked UI/Build Sample UI — 화면 빌드 명세(Specs/*)에서 UIPF 화면 프리팹을 만든다.
    ///
    ///   1. 그림 임포트 설정(KedUIArtImporter, 패키지의 도구 그대로)
    ///   2. TMP 글꼴(Noto Sans KR Medium · Bold, 동적 SDF)
    ///   3. 화면 프리팹 12개(Root 2 · Panel 6 · Page 4) — 자식 이름 = 화면의 Refs
    ///   4. 선택지 항목 프리팹 · 연출 프리셋 DB · UIPF 글꼴 묶음 · 화면별 Presentation
    ///   5. 캔버스 프리팹(Resources/KedUI/KedUICanvas) — 층 셋에 화면을 중첩 프리팹으로 놓고 KedUIHost에 잇는다
    ///   6. Refs 감사 — 화면 타입의 Refs마다 자식이 정확히 하나 있는지
    ///
    /// 다시 실행하면 프리팹은 같은 경로에 덮어쓴다(GUID 유지). 데이터 자산은 이미 있으면 그대로 둔다.
    /// 씬은 건드리지 않는다 — SampleBootstrap이 Resources에서 캔버스를 읽는다.
    /// </summary>
    public static class KedUIBuild
    {
        public const string ScreensDir = KedUIKit.Root + "/Generated/Screens";
        public const string CanvasDir = KedUIKit.Root + "/Resources/KedUI";
        public const string CanvasPath = CanvasDir + "/KedUICanvas.prefab";
        public const string OptionItemPath = KedUIKit.Root + "/Generated/KedOptionItem.prefab";

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

            try
            {
                EditorUtility.DisplayProgressBar("Ked UI", "그림 임포트 설정", 0.05f);
                KedUIArtImporter.Apply();

                EditorUtility.DisplayProgressBar("Ked UI", "TMP 글꼴", 0.15f);
                var font = KedUIFonts.Ensure(problems);

                var kit = new KedUIKit(font);

                EnsureFolder(ScreensDir);
                EnsureFolder(CanvasDir);

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

                EditorUtility.DisplayProgressBar("Ked UI", "Refs 감사", 0.95f);
                foreach (GameObject prefab in roots) Audit(prefab, problems);
                foreach (GameObject prefab in panels) Audit(prefab, problems);
                foreach (GameObject prefab in pages) Audit(prefab, problems);

                problems.AddRange(kit.Problems);
                AssetDatabase.SaveAssets();
            }
            catch (Exception error)
            {
                problems.Add("빌드 중단: " + error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

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

        #region 저장 · 감사

        private static GameObject SaveScreen(GameObject go, List<string> built)
        {
            return Save(go, $"{ScreensDir}/{go.name}.prefab", built);
        }

        private static GameObject Save(GameObject go, string path, List<string> built)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out bool success);
            Object.DestroyImmediate(go);

            if (!success || prefab == null)
                throw new InvalidOperationException("프리팹을 저장하지 못했다: " + path);

            built.Add(path);
            return prefab;
        }

        // 화면·위젯(UIBase<TRefs>)마다: Refs의 이름이 제 아래에 정확히 하나 있는가(UIBase는 이름으로 첫 번째를 잡는다).
        private static void Audit(GameObject prefab, List<string> problems)
        {
            if (prefab == null)
                return;

            foreach (UIBase view in prefab.GetComponentsInChildren<UIBase>(true))
            {
                Type refs = UIRefCheck.RefsTypeOf(view.GetType());
                if (refs == null)
                    continue;

                var counts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (Transform t in view.GetComponentsInChildren<Transform>(true))
                    counts[t.name] = counts.TryGetValue(t.name, out int n) ? n + 1 : 1;

                foreach (string name in Enum.GetNames(refs))
                {
                    counts.TryGetValue(name, out int count);

                    if (count == 0)
                        problems.Add($"{prefab.name}/{view.GetType().Name}: Ref '{name}' 자식이 없다");
                    else if (count > 1)
                        problems.Add($"{prefab.name}/{view.GetType().Name}: Ref '{name}' 자식이 {count}개다(첫 번째만 잡힌다)");
                }
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        #endregion
    }
}
