using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// Tools/Ked UI/Validate Sample UI — 빌드하지 않고 검사만 한다.
    /// 빌더가 만든 프리팹이든, 손이나 다른 에디터 스크립트로 만든 특수 화면의 프리팹이든 같은 그물을 지난다.
    /// 어느 화면도 이름으로 알지 않는다 — 묶음(Screens/*) 아래에 있는 것을 모두 본다.
    ///
    ///   1. 그림 이름 — KedUI 아래 같은 이름의 PNG가 둘 이상인가(그림은 이름으로 찾는다)
    ///   2. Refs — 묶음의 프리팹마다, 그 안의 화면 · 위젯(UIBase&lt;TRefs&gt;)의 Refs 이름이 제 아래에 정확히 하나인가
    ///   3. Presentation — 오브젝트 이름이 파일 이름과 같은가(Unity 경고), presentationId가 비지 않았는가
    ///   4. 묶음 — 묶음 안 에셋이 제 묶음 · Common · UIPF · 본체 · 패키지만 가리키는가(KedUIUnits.Audit)
    ///
    /// 빌드(KedUIBuild)도 저장한 뒤 이것을 돌린다.
    /// 배치 모드: -executeMethod Ked.Samples.UI.Editor.KedUIValidate.ValidateBatch (문제가 있으면 종료 코드 1).
    /// </summary>
    public static class KedUIValidate
    {
        [MenuItem("Tools/Ked UI/Validate Sample UI")]
        public static void ValidateFromMenu()
        {
            bool ok = Validate(out string report);

            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);

            EditorUtility.DisplayDialog(
                "Ked UI",
                ok ? "검사에서 문제가 없습니다. 콘솔에 보고가 있습니다." : "문제가 있습니다. 콘솔의 보고를 확인하세요.",
                "확인");
        }

        /// <summary>배치 모드용. Unity가 이 프로젝트를 열고 있으면 배치 모드는 프로젝트를 열지 못한다.</summary>
        public static void ValidateBatch()
        {
            bool ok = Validate(out string report);
            Debug.Log(report);

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Validate(out string report)
        {
            var problems = new List<string>();
            string checkedLine = Run(problems);

            var sb = new StringBuilder();
            sb.AppendLine(problems.Count == 0 ? "[Ked UI] 검사 통과" : $"[Ked UI] 검사 — 문제 {problems.Count}개");
            sb.AppendLine(checkedLine);

            foreach (string problem in problems)
                sb.AppendLine("  - " + problem);

            report = sb.ToString();
            return problems.Count == 0;
        }

        /// <summary>검사를 돌려 problems에 더하고, 무엇을 몇 개 봤는지 한 줄로 돌려준다.</summary>
        public static string Run(List<string> problems)
        {
            KedUIUnits.ArtIndex(problems);

            int prefabs = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { KedUIUnits.UnitsDir }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null)
                    continue;

                prefabs++;
                AuditRefs(prefab, problems);
            }

            int presentations = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:UIPresentationSpec", new[] { KedUIUnits.UnitsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var spec = AssetDatabase.LoadAssetAtPath<UIPresentationSpec>(path);
                if (spec == null)
                    continue;

                presentations++;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (spec.name != fileName)
                    problems.Add($"Presentation 오브젝트 이름이 파일 이름과 다르다: {path} (이름 '{spec.name}')");

                if (string.IsNullOrWhiteSpace(spec.presentationId))
                    problems.Add($"Presentation의 presentationId가 비었다: {path}");
            }

            int units = KedUIUnits.Audit(problems);

            return $"검사: 화면 묶음 {units} · 프리팹 {prefabs} · Presentation {presentations}";
        }

        /// <summary>화면 · 위젯(UIBase&lt;TRefs&gt;)마다: Refs의 이름이 제 아래에 정확히 하나 있는가(UIBase는 이름으로 첫 번째를 잡는다).</summary>
        public static void AuditRefs(GameObject prefab, List<string> problems)
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
    }
}
