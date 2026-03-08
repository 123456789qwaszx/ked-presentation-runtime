#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PortraitDbBuilder
{
    private const string DefaultSettingsPath = "Assets/Settings/PortraitBuildSettings.asset";

    [MenuItem("Tools/CPS/Portraits/Build Generated DB")]
    public static void Build() => BuildInternal(false);

    [MenuItem("Tools/CPS/Portraits/Build Generated DB (Strict)")]
    public static void BuildStrict() => BuildInternal(true);

    private static void BuildInternal(bool forceStrict)
    {
        var settings = LoadOrCreateSettings();
        var strictMode = forceStrict || settings.strictMode;

        EnsureFolder(Path.GetDirectoryName(settings.generatedDbPath));

        var db = AssetDatabase.LoadAssetAtPath<PortraitGeneratedDBSO>(settings.generatedDbPath);
        if (!db)
        {
            db = ScriptableObject.CreateInstance<PortraitGeneratedDBSO>();
            AssetDatabase.CreateAsset(db, settings.generatedDbPath);
        }

        var report = new List<string>();
        var entries = ScanPortraits(settings, report);

        Undo.RecordObject(db, "Build Portrait Generated DB");
        db.entries = entries;
        db.generatedTicksUtc = DateTime.UtcNow.Ticks;
        db.generatedTimeReadable = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        // Report
        var reportText = BuildReportText(db, report, strictMode);
        if (strictMode && HasErrors(report))
        {
            Debug.LogError(reportText, db);
            throw new Exception("Portrait DB build failed (strict mode). See console for details.");
        }
        else
        {
            Debug.Log(reportText, db);
        }

        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);

        PortraitEditorCache.Rebuild(db);
    }

    private static List<PortraitGeneratedDBSO.Entry> ScanPortraits(PortraitBuildSettings settings, List<string> report)
    {
        var list = new List<PortraitGeneratedDBSO.Entry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var validRoots = new List<string>();
        foreach (var folder in settings.scanFolders)
        {
            var root = (folder ?? "").Replace('\\', '/').Trim();
            if (string.IsNullOrEmpty(root)) continue;

            if (!AssetDatabase.IsValidFolder(root))
            {
                report.Add($"[ERROR] Scan folder not found: {root}");
                continue;
            }

            validRoots.Add(root);
        }

        if (validRoots.Count == 0)
        {
            report.Add("[ERROR] No valid scan folders");
            return list;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", validRoots.ToArray());

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                continue;

            if (!TryParse(path, validRoots, out var cid, out var variantFromFile))
                continue; // 조용히 스킵 (warn spam 방지)

            var sprites = LoadSprites(path);

            if (sprites.Length == 0)
            {
                report.Add($"[WARN] No sprite found: {path}");
                continue;
            }

            if (sprites.Length == 1)
            {
                if (!TrySplitBaseVariantAndEmotion(variantFromFile, out var baseVariant, out var emotionKey))
                {
                    report.Add($"[WARN] Cannot parse emotion from filename: {path}");
                    continue;
                }

                AddEntry(cid, baseVariant, emotionKey, sprites[0], path);
            }
            else
            {
                var baseVariant = NormalizeBaseVariant(variantFromFile);

                foreach (var sp in sprites)
                {
                    if (!sp) continue;

                    string raw = (sp.name ?? "").Trim();
                    if (string.IsNullOrEmpty(raw))
                    {
                        report.Add($"[WARN] Sub-sprite has empty name: {path}");
                        continue;
                    }

                    string emotion = PortraitResolver.NormalizeEmotionCode(raw);

                    if (string.IsNullOrEmpty(emotion))
                    {
                        report.Add($"[WARN] Sub-sprite emotion is not numeric code: '{raw}' ({path})");
                        continue;
                    }

                    AddEntry(cid, baseVariant, emotion, sp, path);
                }
            }
        }

        list.Sort((a, b) =>
        {
            int c = string.Compare(a.characterId, b.characterId, StringComparison.Ordinal);
            if (c != 0) return c;
            c = string.Compare(a.variantKey, b.variantKey, StringComparison.Ordinal);
            if (c != 0) return c;
            return string.Compare(a.emotionKey, b.emotionKey, StringComparison.Ordinal);
        });

        return list;

        void AddEntry(string cid, string variant, string emotion, Sprite sprite, string assetPath)
        {
            var key = cid + "|" + variant + "|" + emotion;
            if (!seen.Add(key))
            {
                report.Add($"[ERROR] Duplicate: {key} ({assetPath})");
                return;
            }

            list.Add(new PortraitGeneratedDBSO.Entry
            {
                characterId = cid,
                variantKey  = variant,
                emotionKey  = emotion,
                sprite      = sprite,
                assetPath   = assetPath
            });
        }
    }

    private static Sprite[] LoadSprites(string path)
    {
        // 1) Sub-sprites 시도
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .ToArray();

        if (sprites.Length > 0)
            return sprites;

        // 2) Single sprite 시도
        var single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (single)
            return new[] { single };

        return new Sprite[0];
    }

    private static bool TryParse(string assetPath, List<string> validRoots, out string characterId, out string variantKeyFromFile)
    {
        characterId = "";
        variantKeyFromFile = "";

        assetPath = assetPath.Replace('\\', '/');

        string matchedPrefix = null;
        foreach (var root in validRoots)
        {
            var prefix = root.TrimEnd('/') + "/";
            if (assetPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                matchedPrefix = prefix;
                break;
            }
        }

        if (matchedPrefix == null) return false;

        string rest = assetPath.Substring(matchedPrefix.Length); // {cid}/{file}
        var parts = rest.Split('/');

        // 현재 규칙: root/cid/file (2단계 고정)
        if (parts.Length != 2) return false;

        characterId = parts[0].Trim();
        var file = parts[parts.Length - 1];
        variantKeyFromFile = (Path.GetFileNameWithoutExtension(file) ?? "").Trim();

        if (string.IsNullOrEmpty(characterId)) return false;
        if (string.IsNullOrEmpty(variantKeyFromFile)) return false;

        return true;
    }

    private static bool TrySplitBaseVariantAndEmotion(
        string variantFromFile,
        out string baseVariant,
        out string emotionKey)
    {
        baseVariant = "";
        emotionKey = "";

        variantFromFile = (variantFromFile ?? "").Trim();
        if (variantFromFile.Length == 0)
            return false;

        int us = variantFromFile.LastIndexOf('_');
        if (us < 0 || us >= variantFromFile.Length - 1)
            return false;

        string rawEmotion = variantFromFile.Substring(us + 1).Trim();
        if (rawEmotion.Length == 0)
            return false;

        string normalized = PortraitResolver.NormalizeEmotionCode(rawEmotion);
        if (normalized.Length == 0)
            return false;

        baseVariant = variantFromFile.Substring(0, us);
        if (baseVariant.Length == 0)
            return false;

        emotionKey = normalized;
        return true;
    }

    private static string NormalizeBaseVariant(string variantFromFile)
    {
        if (TrySplitBaseVariantAndEmotion(variantFromFile, out var baseVariant, out _))
            return baseVariant;
        return (variantFromFile ?? "").Trim();
    }

    private static string BuildReportText(PortraitGeneratedDBSO db, List<string> report, bool strictMode)
    {
        var head =
            $"[PortraitDB] Build complete ({(strictMode ? "STRICT" : "NORMAL")} mode)\n" +
            $"  Entries: {db.TotalEntries}\n" +
            $"  Characters: {db.TotalCharacters}\n" +
            $"  Variants: {db.TotalVariants}\n" +
            $"  Unique Emotions: {db.TotalUniqueEmotions}\n" +
            $"  Generated: {db.generatedTimeReadable}\n";

        if (report.Count == 0) return head + "✅ No warnings/errors.";

        return head + "\n" + string.Join("\n", report);
    }

    private static bool HasErrors(List<string> report)
        => report.Any(r => r.Contains("[ERROR]"));

    private static PortraitBuildSettings LoadOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PortraitBuildSettings>(DefaultSettingsPath);
        if (!settings)
        {
            EnsureFolder(Path.GetDirectoryName(DefaultSettingsPath));
            settings = ScriptableObject.CreateInstance<PortraitBuildSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created default settings at {DefaultSettingsPath}", settings);
        }

        return settings;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;
        folderPath = folderPath.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parts = folderPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    [InitializeOnLoad]
    static class PortraitEditorCacheBootstrap
    {
        static PortraitEditorCacheBootstrap()
        {
            EditorApplication.delayCall += TryInit;
        }

        static void TryInit()
        {
            PortraitBuildSettings settings = AssetDatabase.LoadAssetAtPath<PortraitBuildSettings>(DefaultSettingsPath);
            if (settings != null && !string.IsNullOrEmpty(settings.generatedDbPath))
            {
                PortraitGeneratedDBSO db = AssetDatabase.LoadAssetAtPath<PortraitGeneratedDBSO>(settings.generatedDbPath);
                if (db != null)
                {
                    PortraitEditorCache.Rebuild(db);
                    return;
                }
            }

            // 2) fallback: 프로젝트에서 첫 번째 DB를 찾기 (설정이 없거나 경로가 틀린 경우)
            var guid = AssetDatabase.FindAssets("t:PortraitGeneratedDBSO").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<PortraitGeneratedDBSO>(path);
                if (db != null)
                    PortraitEditorCache.Rebuild(db);
            }
        }

    }
}
#endif
