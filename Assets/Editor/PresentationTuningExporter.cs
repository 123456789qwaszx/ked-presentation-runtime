using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ─────────────────────────────────────────────────────────────────────────────
// U12-전체 — 연출 기준값 덤프.
//
// VnTool Phase 2a가 읽는다. 이 JSON의 스키마가 곧 Ked.Presentation.Core의
// Tuning 타입 모양이 된다(코어 계약 2: 게임별 값은 코드가 아니라 데이터).
//
// 원칙:
// - 값은 지금 값 그대로. 이번에 튜닝하지 않는다.
// - 리그 스키마는 빌더 로직을 베껴 적지 않는다 — 실제 빌더로 리그를 세우고
//   실물 RectTransform 값을 읽는다(사본 금지). 프리팹도 부트스트랩이 쓰는
//   바로 그 프리팹을 씬에서 꺼내 쓴다.
// - 내보낼 수 없는 항목은 건너뛰되 경고 목록으로 남긴다. 조용히 빠뜨리지 않는다.
//
// 실행: 메뉴 Ked/U12/Export Presentation Tuning Dump,
//       또는 batchmode -executeMethod PresentationTuningExporter.ExportAll
// 출력: <프로젝트 루트>/ExportedTuning/
// ─────────────────────────────────────────────────────────────────────────────
public static class PresentationTuningExporter
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    private static readonly (string group, string path)[] PresetAssets =
    {
        ("visual-focus",   "Assets/Data/ScreenEffect/CharacterVisualFocusPresetDB.asset"),
        ("mask-motion",    "Assets/Data/Generated/StageMaskMotionPresetDB.asset"),
        ("screen-flash",   "Assets/Data/ScreenEffect/ScreenFlashPresetDB.asset"),
        ("screen-noise",   "Assets/Data/ScreenEffect/ScreenNoisePresetDB.asset"),
        ("screen-vignette","Assets/Data/ScreenEffect/ScreenVignettePresetDB.asset"),
        ("depth",          "Assets/Data/Generated/CharacterDepthTuning.asset"),
        ("focus-tuning",   "Assets/Data/Generated/CharacterFocusTuningDB.asset"),
        ("role-anchor",    "Assets/Data/Generated/RoleAnchorTuningDB.asset"),
        ("surface-layout", "Assets/Data/Generated/DialogueSurfaceLayoutPresetDB.asset"),
    };

    // U12 지시의 7묶음에 없어서 이번에 내보내지 않는 것들. 존재는 기록한다.
    private static readonly string[] KnownButOutOfScope =
    {
        "Assets/Data/Generated/DialogueSpeakerPresentationPolicyDB.asset (speaker policy — 지시 목록 밖)",
        "Assets/Data/Generated/CharacterEmojiVisualPreset.asset (emoji preset — 지시 목록 밖)",
        "Assets/Data/Generated/CharacterEmojiLibrary.asset (emoji library — 지시 목록 밖)",
        "Assets/Data/Generated/PortraitGeneratedDB.asset (초상화 — U12-v1의 몫)",
    };

    [MenuItem("Ked/U12/Export Presentation Tuning Dump")]
    public static void ExportAll()
    {
        List<string> warnings = new();
        string outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning");
        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(Path.Combine(outDir, "presets"));

        // 씬을 연다 — 프리팹 배선과 CanvasScaler의 원천이 씬이다.
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        BaseResolutionDto baseResolution = ExportBaseResolution(outDir, warnings);
        ExportRigSchemas(outDir, baseResolution, warnings);
        ExportPresets(outDir, warnings);
        ExportPortraitDimensions(outDir, warnings);
        WriteReport(outDir, warnings);

        Debug.Log($"[PresentationTuningExporter] Done. out={outDir}, warnings={warnings.Count}");
    }

    // ── 기준 해상도 ──────────────────────────────────────────────────

    private static BaseResolutionDto ExportBaseResolution(string outDir, List<string> warnings)
    {
        BaseResolutionDto dto = new();

        PresentationUIRoot uiRoot = Object.FindFirstObjectByType<PresentationUIRoot>(FindObjectsInactive.Include);
        CanvasScaler scaler = uiRoot != null ? uiRoot.GetComponentInParent<CanvasScaler>(true) : null;

        if (scaler == null)
        {
            warnings.Add("PresentationUIRoot의 CanvasScaler를 찾지 못했다. 폴백 1920x1080을 썼다 — 확인 필요.");
            dto.canvasName = "(not found — fallback)";
            dto.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize.ToString();
            dto.referenceResolution = new Vector2(1920f, 1080f);
            dto.matchWidthOrHeight = 0f;
        }
        else
        {
            dto.canvasName = scaler.gameObject.name;
            dto.uiScaleMode = scaler.uiScaleMode.ToString();
            dto.referenceResolution = scaler.referenceResolution;
            dto.matchWidthOrHeight = scaler.matchWidthOrHeight;

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                warnings.Add($"프레젠테이션 CanvasScaler가 ScaleWithScreenSize가 아니다({scaler.uiScaleMode}). 좌표 해석 전제가 흔들린다 — 확인 필요.");
        }

        File.WriteAllText(
            Path.Combine(outDir, "base-resolution.json"),
            JsonUtility.ToJson(dto, true));

        return dto;
    }

    // ── 리그 스키마 4종 ──────────────────────────────────────────────

    private static void ExportRigSchemas(string outDir, BaseResolutionDto baseResolution, List<string> warnings)
    {
        // 부트스트랩이 실제로 쓰는 프리팹을 그대로 쓴다.
        VnAppBootstrap bootstrap = Object.FindFirstObjectByType<VnAppBootstrap>(FindObjectsInactive.Include);

        RectTransform charPrefab = null, bgPrefab = null, overlayPrefab = null;

        if (bootstrap == null)
        {
            warnings.Add("VnAppBootstrap을 씬에서 찾지 못했다. 리그를 프리팹 없이(스키마 베이크) 세웠다 — 런타임과 다를 수 있다.");
        }
        else
        {
            SerializedObject so = new(bootstrap);
            charPrefab = so.FindProperty("rigPrefab")?.objectReferenceValue as RectTransform;
            bgPrefab = so.FindProperty("backgroundRigPrefab")?.objectReferenceValue as RectTransform;
            overlayPrefab = so.FindProperty("overlayRigPrefab")?.objectReferenceValue as RectTransform;
        }

        // 리그가 딛고 설 임시 부모: 기준 해상도 크기.
        GameObject stageGo = new("__U12ExportStage", typeof(RectTransform));
        RectTransform stage = (RectTransform)stageGo.transform;
        stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
        stage.pivot = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = baseResolution.referenceResolution;

        RigSchemasDto dump = new()
        {
            capturedUnderParentSize = baseResolution.referenceResolution,
        };

        try
        {
            // Character
            {
                CharacterRigBuilder builder = new();
                RectTransform root = builder.BuildCharacterRigRoot(charPrefab);
                root.SetParent(stage, false);
                builder.BindRefsFromRoot(root, "", out _); // 런타임과 같은 경로: 그래프 검증·복구까지 태운다

                List<(string id, string parent)> nodes = new();
                foreach (CharacterRigSchema.NodeDef n in CharacterRigSchema.Nodes)
                    nodes.Add((n.Id.ToString(), n.Parent?.ToString()));

                dump.rigs.Add(CaptureRig("character", AssetPathOf(charPrefab), root, nodes, warnings));
                Object.DestroyImmediate(root.gameObject);
            }

            // Background
            {
                BackgroundRigBuilder builder = new();
                RectTransform root = builder.BuildBackgroundRigRoot(bgPrefab);
                root.SetParent(stage, false);
                builder.BindRefsFromRoot(root, "", out _);

                List<(string id, string parent)> nodes = new();
                foreach (BackgroundRigSchema.NodeDef n in BackgroundRigSchema.Nodes)
                    nodes.Add((n.Id.ToString(), n.Parent?.ToString()));

                dump.rigs.Add(CaptureRig("background", AssetPathOf(bgPrefab), root, nodes, warnings));
                Object.DestroyImmediate(root.gameObject);
            }

            // Overlay
            {
                OverlayRigBuilder builder = new();
                RectTransform root = builder.BuildOverlayRoot(overlayPrefab);
                root.SetParent(stage, false);
                builder.BindRefsFromRoot(root, "", out _);

                List<(string id, string parent)> nodes = new();
                foreach (OverlayRigSchema.NodeDef n in OverlayRigSchema.Nodes)
                    nodes.Add((n.Id.ToString(), n.Parent?.ToString()));

                dump.rigs.Add(CaptureRig("overlay", AssetPathOf(overlayPrefab), root, nodes, warnings));
                Object.DestroyImmediate(root.gameObject);
            }

            // ScreenEffect (부트스트랩에 프리팹 배선이 없다 — 스키마 베이크가 곧 런타임 경로다)
            {
                ScreenEffectRigBuilder builder = new();
                RectTransform root = builder.BuildRigRoot();
                root.SetParent(stage, false);
                builder.BindRefsFromRoot(root, out _);

                List<(string id, string parent)> nodes = new();
                foreach (ScreenEffectRigSchema.NodeDef n in ScreenEffectRigSchema.Nodes)
                    nodes.Add((n.Id.ToString(), n.Parent?.ToString()));

                dump.rigs.Add(CaptureRig("screenEffect", null, root, nodes, warnings));
                Object.DestroyImmediate(root.gameObject);
            }
        }
        finally
        {
            Object.DestroyImmediate(stageGo);
        }

        File.WriteAllText(
            Path.Combine(outDir, "rig-schemas.json"),
            JsonUtility.ToJson(dump, true));
    }

    private static RigDto CaptureRig(
        string rigKind,
        string sourcePrefabPath,
        RectTransform root,
        List<(string id, string parent)> schemaNodes,
        List<string> warnings)
    {
        RigDto rig = new()
        {
            rigKind = rigKind,
            sourcePrefab = sourcePrefabPath,
        };

        // 루트 자체도 담는다 — 스키마의 Parent=null 노드는 루트의 자식이다.
        rig.nodes.Add(CaptureNode("__root", null, root));

        foreach ((string id, string parent) in schemaNodes)
        {
            RectTransform rect = FindByName(root, id);

            if (rect == null)
            {
                warnings.Add($"{rigKind}: 노드 '{id}'를 세운 리그에서 찾지 못했다. 건너뛰었다.");
                continue;
            }

            rig.nodes.Add(CaptureNode(id, parent ?? "__root", rect));
        }

        return rig;
    }

    private static RigNodeDto CaptureNode(string id, string parent, RectTransform rect)
    {
        // 가시성 축: CanvasGroup 유무 + 초기 alpha.
        // U14 폴드가 스폰 시 초기 가시성을 재현하는 데 쓴다.
        bool hasCanvasGroup = rect.TryGetComponent(out CanvasGroup group);

        return new RigNodeDto
        {
            id = id,
            parent = parent,
            anchoredPosition = rect.anchoredPosition,
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            pivot = rect.pivot,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale,
            localEulerAngles = rect.localEulerAngles,
            measuredRectSize = rect.rect.size,
            hasCanvasGroup = hasCanvasGroup,
            canvasGroupAlpha = hasCanvasGroup ? group.alpha : 0f,
        };
    }

    // ── DBSO 프리셋 ──────────────────────────────────────────────────

    private static void ExportPresets(string outDir, List<string> warnings)
    {
        foreach ((string group, string path) in PresetAssets)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset == null)
            {
                warnings.Add($"프리셋 '{group}': {path} 를 찾지 못했다. 건너뛰었다.");
                continue;
            }

            // 유니티 직렬화 그대로 낸다 — 필드 전사 실수가 생길 자리를 없앤다.
            // 의미·단위는 schema.md가 설명한다.
            string json = EditorJsonUtility.ToJson(asset, true);

            if (json.Contains("\"instanceID\""))
            {
                warnings.Add(
                    $"프리셋 '{group}': 에셋 참조 필드(instanceID)가 들어 있다. " +
                    "그 필드는 이 덤프만으로는 해석할 수 없다 — schema.md의 해당 절 참조.");
            }

            File.WriteAllText(Path.Combine(outDir, "presets", $"{group}.json"), json);
        }
    }

    // ── 초상 치수 ────────────────────────────────────────────────────

    // 초상 스프라이트의 픽셀 치수 테이블 (U14 사이징 폴드 + VnTool 정지 프레임용).
    // CharRigImageSizingPolicy.HeightFitPreserveAspect가 폭을 "부모 높이 × 종횡비"로
    // 정하므로, 치수가 곧 sizeDelta의 원료다.
    private static void ExportPortraitDimensions(string outDir, List<string> warnings)
    {
        const string dbPath = "Assets/Data/Generated/PortraitGeneratedDB.asset";

        PortraitGeneratedDbSo db = AssetDatabase.LoadAssetAtPath<PortraitGeneratedDbSo>(dbPath);

        if (db == null)
        {
            warnings.Add($"초상 DB를 찾지 못했다: {dbPath} — portrait-dimensions.json 생략.");
            return;
        }

        PortraitDimensionsDto dto = new();

        foreach (PortraitGeneratedDbSo.Entry entry in db.entries)
        {
            if (entry.sprite == null)
            {
                warnings.Add($"초상 '{entry.characterId}|{entry.variantKey}|{entry.emotionKey}'의 스프라이트가 비어 있다 — 건너뜀.");
                continue;
            }

            dto.entries.Add(new PortraitDimensionDto
            {
                character = entry.characterId,
                variant = entry.variantKey,
                emotion = entry.emotionKey,
                width = entry.sprite.rect.width,
                height = entry.sprite.rect.height,
            });
        }

        File.WriteAllText(
            Path.Combine(outDir, "portrait-dimensions.json"),
            JsonUtility.ToJson(dto, true));
    }

    [Serializable]
    private sealed class PortraitDimensionsDto
    {
        public List<PortraitDimensionDto> entries = new();
    }

    [Serializable]
    private sealed class PortraitDimensionDto
    {
        public string character;
        public string variant;
        public string emotion;
        public float width;
        public float height;
    }

    // ── 보고서 ───────────────────────────────────────────────────────

    private static void WriteReport(string outDir, List<string> warnings)
    {
        ReportDto report = new()
        {
            exportedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            unityVersion = Application.unityVersion,
            scenePath = ScenePath,
            warnings = warnings,
            knownButNotExported = new List<string>(KnownButOutOfScope),
        };

        File.WriteAllText(
            Path.Combine(outDir, "export-report.json"),
            JsonUtility.ToJson(report, true));

        foreach (string w in warnings)
            Debug.LogWarning($"[PresentationTuningExporter] {w}");
    }

    // ── helper ───────────────────────────────────────────────────────

    private static string AssetPathOf(RectTransform prefab)
        => prefab == null ? null : AssetDatabase.GetAssetPath(prefab);

    private static RectTransform FindByName(Transform root, string name)
    {
        if (root.name == name)
            return root as RectTransform;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform found = FindByName(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    // ── DTO (JsonUtility 직렬화 대상) ─────────────────────────────────

    [Serializable]
    private sealed class BaseResolutionDto
    {
        public string canvasName;
        public string uiScaleMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
    }

    [Serializable]
    private sealed class RigSchemasDto
    {
        public Vector2 capturedUnderParentSize;
        public List<RigDto> rigs = new();
    }

    [Serializable]
    private sealed class RigDto
    {
        public string rigKind;
        public string sourcePrefab;
        public List<RigNodeDto> nodes = new();
    }

    [Serializable]
    private sealed class RigNodeDto
    {
        public string id;
        public string parent;
        public Vector2 anchoredPosition;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Vector3 localEulerAngles;
        public Vector2 measuredRectSize;
        public bool hasCanvasGroup;      // 가시성 축 대상인가
        public float canvasGroupAlpha;   // hasCanvasGroup일 때만 의미 있는 초기 alpha
    }

    [Serializable]
    private sealed class ReportDto
    {
        public string exportedAtUtc;
        public string unityVersion;
        public string scenePath;
        public List<string> warnings = new();
        public List<string> knownButNotExported = new();
    }
}
