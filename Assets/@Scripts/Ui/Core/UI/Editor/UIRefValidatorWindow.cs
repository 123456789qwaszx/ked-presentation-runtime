// Assets/Editor/CpsUiRefValidatorWindow.cs
// Unity Editor-only UI ref validator for UITop<TRefs>-style components.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[AttributeUsage(AttributeTargets.Field)]
public sealed class OptionalRefAttribute : Attribute { }

public sealed class CpsUiRefValidatorWindow : EditorWindow
{
    private enum Severity { Error, Warning }

    [Serializable]
    private sealed class Issue
    {
        public Severity severity;
        public string prefabPath;
        public string uiTypeName;
        public string refName;
        public string problem;        // Missing / WrongType / UnknownRule
        public string expected;       // e.g. CanvasGroup, Image, TMP_Text
        public string foundPath;      // hierarchy path if found
        public string note;
    }

    private Vector2 _scroll;
    private readonly List<Issue> _issues = new();

    // Toggle filters
    private bool _showErrors = true;
    private bool _showWarnings = true;
    private bool _includeOptional = false;

    // Perf knobs
    private bool _scanAllPrefabs = true;
    private UnityEngine.Object _scanFolder; // optional folder object

    [MenuItem("Tools/CPS/Validate UI Refs")]
    public static void Open()
    {
        GetWindow<CpsUiRefValidatorWindow>("CPS UI Refs Validator");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("CPS UI Refs Validator", EditorStyles.boldLabel);

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            _showErrors = EditorGUILayout.ToggleLeft("Show Errors", _showErrors);
            _showWarnings = EditorGUILayout.ToggleLeft("Show Warnings", _showWarnings);
            _includeOptional = EditorGUILayout.ToggleLeft("Include Optional Refs", _includeOptional);

            EditorGUILayout.Space(6);
            _scanAllPrefabs = EditorGUILayout.ToggleLeft("Scan All Prefabs in Project", _scanAllPrefabs);

            using (new EditorGUI.DisabledScope(_scanAllPrefabs))
            {
                _scanFolder = EditorGUILayout.ObjectField(
                    new GUIContent("Folder (optional)", "Limit scan to a folder (DefaultAsset)."),
                    _scanFolder,
                    typeof(DefaultAsset),
                    false);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Prefabs", GUILayout.Height(28)))
                    ScanPrefabs();

                if (GUILayout.Button("Clear", GUILayout.Height(28)))
                    _issues.Clear();
            }
        }

        EditorGUILayout.Space(8);

        DrawSummary();
        DrawIssuesList();
    }

    private void DrawSummary()
    {
        int err = _issues.Count(i => i.severity == Severity.Error);
        int warn = _issues.Count(i => i.severity == Severity.Warning);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Issues: { _issues.Count }   Errors: { err }   Warnings: { warn }");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy Report (TSV)", GUILayout.Width(150)))
            {
                EditorGUIUtility.systemCopyBuffer = BuildTsv(_issues);
            }
        }
    }

    private void DrawIssuesList()
    {
        var filtered = _issues.Where(i =>
        {
            if (i.severity == Severity.Error && !_showErrors) return false;
            if (i.severity == Severity.Warning && !_showWarnings) return false;
            return true;
        }).ToList();

        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var issue in filtered)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = issue.severity == Severity.Error ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.75f, 0.35f);

                EditorGUILayout.LabelField($"{issue.severity} | {issue.problem}", style);
                EditorGUILayout.LabelField($"Prefab: {issue.prefabPath}");
                EditorGUILayout.LabelField($"UI: {issue.uiTypeName}");
                EditorGUILayout.LabelField($"Ref: {issue.refName}");
                EditorGUILayout.LabelField($"Expected: {issue.expected}");
                if (!string.IsNullOrEmpty(issue.foundPath))
                    EditorGUILayout.LabelField($"Found: {issue.foundPath}");
                if (!string.IsNullOrEmpty(issue.note))
                    EditorGUILayout.LabelField($"Note: {issue.note}");

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping Prefab"))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.prefabPath);
                        EditorGUIUtility.PingObject(prefab);
                        Selection.activeObject = prefab;
                    }

                    // Optional: Open prefab stage for inspection
                    if (GUILayout.Button("Open Prefab"))
                    {
                        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.prefabPath));
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static string BuildTsv(List<Issue> issues)
    {
        // Tab-separated values for easy paste into sheets.
        var lines = new List<string>
        {
            "Severity\tProblem\tPrefab\tUIType\tRef\tExpected\tFoundPath\tNote"
        };

        foreach (var i in issues)
        {
            lines.Add($"{i.severity}\t{i.problem}\t{i.prefabPath}\t{i.uiTypeName}\t{i.refName}\t{i.expected}\t{i.foundPath}\t{i.note}");
        }
        return string.Join("\n", lines);
    }

    private void ScanPrefabs()
    {
        _issues.Clear();

        // 1) Collect candidate prefab GUIDs
        string[] searchInFolders = null;
        if (!_scanAllPrefabs && _scanFolder != null)
        {
            var folderPath = AssetDatabase.GetAssetPath(_scanFolder);
            if (AssetDatabase.IsValidFolder(folderPath))
                searchInFolders = new[] { folderPath };
        }

        var guids = AssetDatabase.FindAssets("t:Prefab", searchInFolders);

        // 2) Find all UI component types that look like UITop<TEnumRefs>
        var uiTypes = FindUiTopEnumTypes();
        if (uiTypes.Count == 0)
        {
            Debug.LogWarning("[CPS UI Refs Validator] No UITop<TEnum> style types found. " +
                             "If your base type name differs, adjust FindUiTopEnumTypes().");
            return;
        }

        // Cache: component type -> ref enum type
        var typeToRefEnum = uiTypes.ToDictionary(t => t, t => GetRefsEnumTypeFromUiType(t));

        // 3) Scan each prefab
        for (int gi = 0; gi < guids.Length; gi++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[gi]);
            if (string.IsNullOrEmpty(path)) continue;

            // Load prefab contents safely (doesn't instantiate into scene)
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // For each UI type, check if prefab contains it
                foreach (var uiType in uiTypes)
                {
                    var comps = root.GetComponentsInChildren(uiType, true);
                    if (comps == null || comps.Length == 0) continue;

                    var refEnum = typeToRefEnum[uiType];
                    if (refEnum == null) continue;

                    foreach (var comp in comps)
                    {
                        ValidateOneComponent(path, uiType, comp, refEnum);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Sort: Errors first, then prefab, then ref
        _issues.Sort((a, b) =>
        {
            int s = b.severity.CompareTo(a.severity); // Error(0) before Warning(1)? we invert below
            // Actually our enum is Error=0, Warning=1. Want Error first => compare normally
            s = a.severity.CompareTo(b.severity);
            if (s != 0) return s;
            int p = string.Compare(a.prefabPath, b.prefabPath, StringComparison.Ordinal);
            if (p != 0) return p;
            return string.Compare(a.refName, b.refName, StringComparison.Ordinal);
        });

        Debug.Log($"[CPS UI Refs Validator] Scan done. Issues: {_issues.Count}");
        Repaint();
    }

    private void ValidateOneComponent(string prefabPath, Type uiType, Component uiComp, Type refEnumType)
    {
        // Enumerate enum members (Refs)
        var values = Enum.GetValues(refEnumType);
        foreach (var v in values)
        {
            var name = Enum.GetName(refEnumType, v);
            if (string.IsNullOrEmpty(name)) continue;

            bool isOptional = IsOptionalEnumMember(refEnumType, name);
            if (isOptional && !_includeOptional)
                continue;

            var (rule, expectedRequired, expectedRecommended) = GuessRule(name);

            // Find transform by exact name in the same prefab root (global)
            // If you prefer "within this UI component subtree only", change searchRoot to uiComp.transform.
            var searchRoot = uiComp.transform;
            var t = FindDeepChildByName(searchRoot, name);

            if (t == null)
            {
                if (!isOptional)
                {
                    _issues.Add(new Issue
                    {
                        severity = Severity.Error,
                        prefabPath = prefabPath,
                        uiTypeName = uiType.FullName,
                        refName = name,
                        problem = "Missing",
                        expected = expectedRequired.Length > 0 ? string.Join(", ", expectedRequired.Select(x => x.Name)) : "(no rule)",
                        foundPath = "",
                        note = $"Not found under '{GetTransformPath(searchRoot)}'."
                    });
                }
                continue;
            }

            // Required component checks
            foreach (var req in expectedRequired)
            {
                if (!HasComponent(t, req))
                {
                    _issues.Add(new Issue
                    {
                        severity = Severity.Error,
                        prefabPath = prefabPath,
                        uiTypeName = uiType.FullName,
                        refName = name,
                        problem = "WrongType",
                        expected = req.Name,
                        foundPath = GetTransformPath(t),
                        note = "Required component missing."
                    });
                }
            }

            // Recommended component checks (warnings)
            foreach (var rec in expectedRecommended)
            {
                if (!HasComponent(t, rec))
                {
                    _issues.Add(new Issue
                    {
                        severity = Severity.Warning,
                        prefabPath = prefabPath,
                        uiTypeName = uiType.FullName,
                        refName = name,
                        problem = "RecommendedMissing",
                        expected = rec.Name,
                        foundPath = GetTransformPath(t),
                        note = "Recommended component missing."
                    });
                }
            }

            // Unknown rule? Let user know (warning, optional)
            if (rule == RefRule.Unknown)
            {
                _issues.Add(new Issue
                {
                    severity = Severity.Warning,
                    prefabPath = prefabPath,
                    uiTypeName = uiType.FullName,
                    refName = name,
                    problem = "UnknownRule",
                    expected = "(no suffix rule matched)",
                    foundPath = GetTransformPath(t),
                    note = "Add a suffix rule or mark as OptionalRef if intended."
                });
            }
        }
    }

    // ---------- Rules ----------
    private enum RefRule
    {
        Unknown,
        Root,
        Track,
        Anchor,
        Pad,
        Image,
        Text,
        Button,
        CanvasGroupExplicit,
    }

    private static (RefRule rule, Type[] required, Type[] recommended) GuessRule(string refName)
    {
        // Basic suffix rules (you can extend freely)
        // Required vs Recommended: keep it simple now.
        // If you dislike CanvasGroup-on-every-Root, move CanvasGroup to recommended or add *_Blocker_Root special-case.
        var rect = typeof(RectTransform);
        var cg = typeof(CanvasGroup);
        var img = typeof(Image);
        var btn = typeof(Button);

        Type tmpText = TryGetType("TMPro.TMP_Text, Unity.TextMeshPro");
        // fallback: UnityEngine.UI.Text (legacy)
        Type uguiText = typeof(UnityEngine.UI.Text);

        if (refName.EndsWith("_Root", StringComparison.Ordinal))
            return (RefRule.Root, new[] { rect, cg }, Array.Empty<Type>());

        if (refName.EndsWith("_Track", StringComparison.Ordinal))
            return (RefRule.Track, new[] { rect }, Array.Empty<Type>());

        if (refName.EndsWith("_Anchor", StringComparison.Ordinal))
            return (RefRule.Anchor, new[] { rect }, Array.Empty<Type>());

        if (refName.EndsWith("_Pad", StringComparison.Ordinal))
            return (RefRule.Pad, new[] { rect }, Array.Empty<Type>());

        if (refName.EndsWith("_Image", StringComparison.Ordinal))
            return (RefRule.Image, new[] { rect, img }, Array.Empty<Type>());

        if (refName.EndsWith("_Button", StringComparison.Ordinal))
            return (RefRule.Button, new[] { rect, btn }, Array.Empty<Type>());

        if (refName.EndsWith("_Text", StringComparison.Ordinal))
        {
            if (tmpText != null)
                return (RefRule.Text, new[] { rect, tmpText }, Array.Empty<Type>());
            return (RefRule.Text, new[] { rect, uguiText }, Array.Empty<Type>());
        }

        // If you have explicit naming for CanvasGroups:
        if (refName.EndsWith("_CanvasGroup", StringComparison.Ordinal))
            return (RefRule.CanvasGroupExplicit, new[] { rect, cg }, Array.Empty<Type>());

        return (RefRule.Unknown, Array.Empty<Type>(), Array.Empty<Type>());
    }

    // ---------- Type discovery ----------
    private static List<Type> FindUiTopEnumTypes()
    {
        // We detect any MonoBehaviour that has a generic base type with an enum generic argument.
        // This matches UITop<Refs> and similar patterns.
        var result = new List<Type>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

            foreach (var t in types)
            {
                if (t == null) continue;
                if (t.IsAbstract) continue;
                if (!typeof(MonoBehaviour).IsAssignableFrom(t)) continue;

                var refEnum = GetRefsEnumTypeFromUiType(t);
                if (refEnum == null) continue;

                result.Add(t);
            }
        }

        return result;
    }

    private static Type GetRefsEnumTypeFromUiType(Type uiType)
    {
        // Walk base chain, find first generic base with enum generic argument.
        var cur = uiType;
        while (cur != null && cur != typeof(MonoBehaviour))
        {
            if (cur.IsGenericType)
            {
                var args = cur.GetGenericArguments();
                if (args.Length == 1 && args[0].IsEnum)
                    return args[0];
            }
            cur = cur.BaseType;
        }
        return null;
    }

    private static bool IsOptionalEnumMember(Type enumType, string memberName)
    {
        var fi = enumType.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
        if (fi == null) return false;

        // [OptionalRef] support
        if (fi.GetCustomAttributes(typeof(OptionalRefAttribute), false).Length > 0)
            return true;

        // Naming convention fallback (optional): suffix or prefix
        // e.g. Opt__Something_Image or Something_Image_Optional
        if (memberName.StartsWith("Opt__", StringComparison.Ordinal))
            return true;
        if (memberName.EndsWith("_Optional", StringComparison.Ordinal))
            return true;

        return false;
    }

    // ---------- Hierarchy search ----------
    private static Transform FindDeepChildByName(Transform root, string exactName)
    {
        if (root == null) return null;
        // BFS
        var q = new Queue<Transform>();
        q.Enqueue(root);

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (t.name == exactName)
                return t;

            for (int i = 0; i < t.childCount; i++)
                q.Enqueue(t.GetChild(i));
        }
        return null;
    }

    private static bool HasComponent(Transform t, Type compType)
    {
        if (t == null || compType == null) return false;
        return t.GetComponent(compType) != null;
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "";
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack);
    }

    private static Type TryGetType(string assemblyQualifiedName)
    {
        try { return Type.GetType(assemblyQualifiedName); }
        catch { return null; }
    }
}
#endif
