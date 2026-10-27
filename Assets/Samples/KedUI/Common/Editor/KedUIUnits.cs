using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 화면 묶음(시험 중 — docs/PLAN_uipf-ui.md "화면 묶음 시험")의 규칙을 한곳에 둔다.
    ///
    ///   Screens/{묶음}/Scripts/       화면 · 위젯 스크립트
    ///   Screens/{묶음}/Prefabs/       빌더가 만든 화면 프리팹
    ///   Screens/{묶음}/Presentation/  화면의 UIPF Presentation({화면}Presentation.asset)
    ///   Screens/{묶음}/Sprites/       그 화면만 쓰는 그림
    ///   Screens/{묶음}/Data/          그 화면에 맞춘 연출 데이터(재생 묶음의 대사창 배치 DB)
    ///   Screens/{묶음}/Editor/        빌드 명세(.asmref로 에디터 어셈블리에 붙는다)
    ///   Common/                       여러 화면이 같이 쓰는 키트 스크립트 · 그림 · 글꼴
    ///
    /// 화면 스크립트가 묶음에 있으면 생성물은 그 묶음에, 없으면 예전처럼 Generated/에 만든다.
    /// 묶음은 제 묶음 · Common · UIPF · 본체(게임 쪽 프레임워크) · Unity 패키지만 가리킬 수 있다(Audit).
    /// </summary>
    internal static class KedUIUnits
    {
        public const string UnitsDir = KedUIKit.Root + "/Screens";
        public const string CommonDir = KedUIKit.Root + "/Common";

        private const string UipfDir = "Assets/UIPresentationFlow";
        private const string CoreDir = "Assets/Ked.Presentation"; // 선택지 항목(VNOptionItem) · 대사창 배치 DB 같은 본체 계약

        #region 그림 — 이름으로 찾기

        /// <summary>
        /// KedUI 아래 PNG를 이름(확장자 뺀 파일 이름)으로 모은다. 그림은 Art/ · Common/Sprites/ · 묶음의 Sprites/ 어디에 있어도 된다.
        /// 같은 이름이 둘 이상이면 그 이름은 problems에 올리고 찾지 않는다.
        /// </summary>
        public static Dictionary<string, string> ArtIndex(List<string> problems)
        {
            var found = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { KedUIKit.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                if (!found.TryGetValue(name, out List<string> paths))
                    found[name] = paths = new List<string>();

                paths.Add(path);
            }

            var index = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, List<string>> pair in found)
            {
                if (pair.Value.Count == 1)
                    index[pair.Key] = pair.Value[0];
                else
                    problems?.Add($"그림 이름이 겹친다 '{pair.Key}': {string.Join(", ", pair.Value)}");
            }

            return index;
        }

        #endregion

        #region 생성물 위치

        /// <summary>화면 스크립트가 Screens/{묶음}/Scripts/에 있으면 그 묶음 폴더, 아니면 null.</summary>
        public static string UnitOf(Type viewType)
        {
            string script = ScriptPath(viewType);
            if (script == null || !script.StartsWith(UnitsDir + "/", StringComparison.Ordinal))
                return null;

            string dir = Path.GetDirectoryName(script)?.Replace('\\', '/');
            if (dir == null || !dir.EndsWith("/Scripts", StringComparison.Ordinal))
                return null;

            return dir.Substring(0, dir.Length - "/Scripts".Length);
        }

        public static string PrefabPath(Type viewType, string name, string fallbackDir)
        {
            string unit = UnitOf(viewType);
            return unit != null ? $"{unit}/Prefabs/{name}.prefab" : $"{fallbackDir}/{name}.prefab";
        }

        public static string DataPath(Type viewType, string fileName, string fallbackDir)
        {
            string unit = UnitOf(viewType);
            return unit != null ? $"{unit}/Data/{fileName}" : $"{fallbackDir}/{fileName}";
        }

        public static string PresentationPath(Type viewType, string key, string fallbackDir)
        {
            string unit = UnitOf(viewType);
            return unit != null ? $"{unit}/Presentation/{key}Presentation.asset" : $"{fallbackDir}/{key}.asset";
        }

        private static string ScriptPath(Type type)
        {
            if (type == null)
                return null;

            foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return path;
            }

            return null;
        }

        #endregion

        #region 폴더

        /// <summary>폴더가 없으면 부모부터 만든다.</summary>
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

        #region 묶음 감사

        /// <summary>
        /// 묶음마다: 안의 에셋이 직접 가리키는 것이 제 묶음 · Common · UIPF · 본체 · Unity 패키지 · 내장 리소스뿐인가.
        /// 다른 묶음이나 KedUI의 다른 폴더(Art · Generated …)를 가리키면 problems에 올린다.
        /// 돌려주는 값은 감사한 묶음 수.
        /// </summary>
        public static int Audit(List<string> problems)
        {
            if (!AssetDatabase.IsValidFolder(UnitsDir))
                return 0;

            string[] units = AssetDatabase.GetSubFolders(UnitsDir);

            foreach (string unit in units)
            {
                foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { unit }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(path))
                        continue;

                    foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                    {
                        if (!IsAllowed(unit, dependency))
                            problems.Add($"묶음 밖을 가리킨다: {path} → {dependency}");
                    }
                }
            }

            return units.Length;
        }

        private static bool IsAllowed(string unit, string dependency)
        {
            if (!dependency.StartsWith("Assets/", StringComparison.Ordinal))
                return true; // Packages/ · 내장 리소스

            return dependency.StartsWith(unit + "/", StringComparison.Ordinal)
                   || dependency.StartsWith(CommonDir + "/", StringComparison.Ordinal)
                   || dependency.StartsWith(UipfDir + "/", StringComparison.Ordinal)
                   || dependency.StartsWith(CoreDir + "/", StringComparison.Ordinal);
        }

        #endregion
    }
}
