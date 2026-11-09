using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 컷인 판 그림을 그린다 — 메뉴 Ked › Cut-in › 판 그림 만들기 (docs/PLAN_cutin.md §4).
///
/// 판의 모양은 프리셋(CutInPresetDBSO) 한 곳이 정한다: 판 크기 · 네 모서리 비율 · 외곽선 두께(= maskInsetPixels,
/// 마스크의 아래 변이 외곽선 안쪽에 오도록). 이 도구는 그 값으로 비튼 사각형을 그리고 투명 여백을 남긴다 —
/// 런타임은 그림을 판 크기 사각형에 그대로 펴므로, 모서리를 바꿨으면 여기서 다시 그리면 마스크와 그림이 같이 따라온다.
/// 그림은 캔버스 크기의 2배 해상도다. 테마(색 · 무늬)는 아래 표 — 원본 게임 그림이 아니라 직접 만든 것이다.
///
/// 프리셋 에셋이 있으면 그것을(선택한 것이 먼저), 없으면 코드 기본값을 쓴다. 그림 자리는 이미 있는
/// Resources/CutInPanels 폴더, 없으면 예제 폴더다. 같은 이름의 PNG는 덮어쓴다(.meta는 그대로).
/// </summary>
public static class CutInPanelGenerator
{
    private const string MenuPath = "Ked/Cut-in/판 그림 만들기";
    private const string DefaultFolder = "Assets/Samples/Content/Resources/CutInPanels";
    private const int Resolution = 2;

    private enum Pattern { Stars, Questions, Rays }

    private sealed class Theme
    {
        public string Key;
        public Color A, B, C;      // 왼위 → 가운데 → 오른아래
        public Pattern Pattern;
        public int Seed;
    }

    private static readonly Theme[] Themes =
    {
        new() { Key = "pink_stars",    A = Rgb(246, 120, 160), B = Rgb(250, 178, 200), C = Rgb(255, 236, 240), Pattern = Pattern.Stars,     Seed = 7 },
        new() { Key = "blue_question", A = Rgb(40, 72, 150),   B = Rgb(92, 150, 215),  C = Rgb(205, 232, 250), Pattern = Pattern.Questions, Seed = 11 },
        new() { Key = "yellow_burst",  A = Rgb(255, 190, 60),  B = Rgb(255, 220, 120), C = Rgb(255, 246, 214), Pattern = Pattern.Rays,      Seed = 13 },
    };

    private static readonly Color Outline = Rgb(34, 30, 38);

    [MenuItem(MenuPath)]
    public static void Generate()
    {
        CutInPresetDBSO preset = FindPreset(out bool temporary);

        try
        {
            string folder = FindFolder();
            int w = Mathf.Max(8, Mathf.RoundToInt(preset.panelSize.x * Resolution));
            int h = Mathf.Max(8, Mathf.RoundToInt(preset.panelSize.y * Resolution));

            foreach (Theme theme in Themes)
            {
                string assetPath = $"{folder}/{theme.Key}.png";
                Texture2D tex = Draw(theme, preset, w, h);

                try
                {
                    File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                EnsureSpriteImport(assetPath);
            }

            Debug.Log($"[CutInPanelGenerator] 판 그림 {Themes.Length}장을 그렸다 — {folder}, {w}×{h}px, 프리셋 '{preset.name}'.");
        }
        finally
        {
            if (temporary)
                Object.DestroyImmediate(preset);
        }
    }

    // ── 그리기 ───────────────────────────────────────────────────────

    private static Texture2D Draw(Theme theme, CutInPresetDBSO preset, int w, int h)
    {
        // 모서리(픽셀, y 위). 반시계 = 왼아래 → 오른아래 → 오른위 → 왼위.
        Vector2 size = new(w, h);
        Vector2[] quad =
        {
            Vector2.Scale(preset.cornerBottomLeft, size),
            Vector2.Scale(preset.cornerBottomRight, size),
            Vector2.Scale(preset.cornerTopRight, size),
            Vector2.Scale(preset.cornerTopLeft, size),
        };

        float outline = Mathf.Max(1f, preset.maskInsetPixels * Resolution);
        Feature[] features = BuildFeatures(theme, w, h);

        Color32[] pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float sd = SignedDistanceInside(quad, p);
                float coverage = Mathf.Clamp01(sd + 0.5f);

                if (coverage <= 0f)
                {
                    pixels[y * w + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                float yTop = h - p.y;
                float t = p.x / w * 0.55f + yTop / h * 0.45f;
                Color c = t < 0.6f ? Color.Lerp(theme.A, theme.B, t / 0.6f) : Color.Lerp(theme.B, theme.C, (t - 0.6f) / 0.4f);

                c = Halftone(c, p, w, h);

                for (int i = 0; i < features.Length; i++)
                    c = Color.Lerp(c, Color.white, features[i].Alpha * features[i].Coverage(p));

                float edge = Mathf.Clamp01(outline - sd + 0.5f);
                c = Color.Lerp(c, Outline, edge);
                c.a = coverage;

                pixels[y * w + x] = c;
            }
        }

        Texture2D tex = new(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // 볼록 사각형(반시계) 안쪽으로의 거리 — 바깥이면 음수.
    private static float SignedDistanceInside(Vector2[] quad, Vector2 p)
    {
        float min = float.MaxValue;

        for (int i = 0; i < quad.Length; i++)
        {
            Vector2 a = quad[i];
            Vector2 b = quad[(i + 1) % quad.Length];
            Vector2 e = b - a;
            float d = (e.x * (p.y - a.y) - e.y * (p.x - a.x)) / e.magnitude;
            min = Mathf.Min(min, d);
        }

        return min;
    }

    // 오른아래로 갈수록 커지는 망점.
    private static Color Halftone(Color c, Vector2 p, int w, int h)
    {
        const float step = 18f;
        const float maxRadius = 7f;

        float gx = Mathf.Round(p.x / step) * step;
        float gyTop = Mathf.Round((h - p.y) / step) * step;
        float t = Mathf.Max(0f, gx / w + gyTop / h - 1f);
        float r = maxRadius * t;

        if (r < 0.6f)
            return c;

        float d = Vector2.Distance(new Vector2(gx, h - gyTop), p);
        return Color.Lerp(c, Color.white, 0.45f * Mathf.Clamp01(r - d + 0.5f));
    }

    // ── 무늬 ─────────────────────────────────────────────────────────

    private abstract class Feature
    {
        public float Alpha;
        public abstract float Coverage(Vector2 p);
    }

    // 네 갈래 반짝임 — √|dx| + √|dy| ≤ √r.
    private sealed class Sparkle : Feature
    {
        public Vector2 Center;
        public float Radius;

        public override float Coverage(Vector2 p)
        {
            Vector2 d = p - Center;

            if (Mathf.Abs(d.x) > Radius || Mathf.Abs(d.y) > Radius)
                return 0f;

            float s = Mathf.Sqrt(Mathf.Abs(d.x)) + Mathf.Sqrt(Mathf.Abs(d.y));
            float rs = Mathf.Sqrt(Radius);
            return Mathf.Clamp01((rs - s) * rs);
        }
    }

    private sealed class Dot : Feature
    {
        public Vector2 Center;
        public float Radius;

        public override float Coverage(Vector2 p) => Mathf.Clamp01(Radius - Vector2.Distance(p, Center) + 0.5f);
    }

    // 물음표 — 위 갈고리(호) · 줄기 · 점. 글꼴 없이 거리로 그린다.
    private sealed class Question : Feature
    {
        public Vector2 Center;
        public float Height;
        public float Angle;   // 라디안

        public override float Coverage(Vector2 p)
        {
            Vector2 d = p - Center;

            if (d.sqrMagnitude > Height * Height)
                return 0f;

            float cos = Mathf.Cos(-Angle), sin = Mathf.Sin(-Angle);
            Vector2 q = new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos) / Height;   // 글자 높이 1

            const float stroke = 0.075f;
            Vector2 arcCenter = new(0f, 0.18f);
            const float arcRadius = 0.2f;

            // 호: 왼쪽(180°)에서 위를 지나 오른아래(−50°)까지.
            Vector2 r = q - arcCenter;
            float ang = Mathf.Atan2(r.y, r.x) * Mathf.Rad2Deg;
            float arc = ang >= -50f || ang <= -180f + 1e-3f
                ? Mathf.Abs(r.magnitude - arcRadius)
                : Mathf.Min(
                    Vector2.Distance(q, arcCenter + Polar(arcRadius, 180f)),
                    Vector2.Distance(q, arcCenter + Polar(arcRadius, -50f)));

            Vector2 stemTop = arcCenter + Polar(arcRadius, -50f);
            Vector2 stemBottom = new(0.02f, -0.12f);
            float stem = DistanceToSegment(q, stemTop, stemBottom);
            float dot = Vector2.Distance(q, new Vector2(0.02f, -0.36f)) - 0.06f;

            float sd = Mathf.Min(Mathf.Min(arc, stem) - stroke, dot);
            return Mathf.Clamp01(-sd * Height + 0.5f);
        }

        private static Vector2 Polar(float r, float degrees)
            => new(r * Mathf.Cos(degrees * Mathf.Deg2Rad), r * Mathf.Sin(degrees * Mathf.Deg2Rad));
    }

    // 가운데에서 뻗는 빛살 — 56칸 중 짝수 칸.
    private sealed class Rays : Feature
    {
        public Vector2 Center;
        public int Count;

        public override float Coverage(Vector2 p)
        {
            Vector2 d = p - Center;
            float a = Mathf.Atan2(d.y, d.x);

            if (a < 0f)
                a += Mathf.PI * 2f;

            int sector = Mathf.FloorToInt(a / (Mathf.PI * 2f / (Count * 2)));
            return sector % 2 == 0 ? 1f : 0f;
        }
    }

    private static Feature[] BuildFeatures(Theme theme, int w, int h)
    {
        System.Random rng = new(theme.Seed);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        System.Collections.Generic.List<Feature> list = new();

        switch (theme.Pattern)
        {
            case Pattern.Stars:
                for (int i = 0; i < 26; i++)
                    list.Add(new Sparkle { Center = new Vector2(R(0, w), R(h * 0.2f, h)), Radius = R(10, 26), Alpha = R(70, 140) / 255f });
                break;

            case Pattern.Questions:
                for (int i = 0; i < 16; i++)
                    list.Add(new Question { Center = new Vector2(R(0, w), R(0, h)), Height = R(48, 64), Angle = R(-25, 25) * Mathf.Deg2Rad, Alpha = R(55, 110) / 255f });
                for (int i = 0; i < 40; i++)
                    list.Add(new Dot { Center = new Vector2(R(0, w), R(0, h)), Radius = R(2, 5), Alpha = 90f / 255f });
                break;

            case Pattern.Rays:
                list.Add(new Rays { Center = new Vector2(w * 0.30f, h * 0.62f), Count = 28, Alpha = 60f / 255f });
                for (int i = 0; i < 14; i++)
                    list.Add(new Sparkle { Center = new Vector2(R(0, w), R(h * 0.2f, h)), Radius = R(8, 18), Alpha = 120f / 255f });
                break;
        }

        return list.ToArray();
    }

    // ── 찾기 · 임포트 ────────────────────────────────────────────────

    private static CutInPresetDBSO FindPreset(out bool temporary)
    {
        temporary = false;

        if (Selection.activeObject is CutInPresetDBSO selected)
            return selected;

        string[] guids = AssetDatabase.FindAssets("t:CutInPresetDBSO");

        if (guids.Length > 0)
        {
            if (guids.Length > 1)
                Debug.LogWarning($"[CutInPanelGenerator] 컷인 프리셋이 {guids.Length}개다. 첫 것을 쓴다 — 다른 것을 쓰려면 그 에셋을 선택하고 다시 부른다.");

            return AssetDatabase.LoadAssetAtPath<CutInPresetDBSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        Debug.LogWarning("[CutInPanelGenerator] 컷인 프리셋 에셋이 없다. 코드 기본값으로 그린다.");
        temporary = true;
        return CutInPresetDBSO.CreateDefault();
    }

    private static string FindFolder()
    {
        string marker = "/Resources/" + CutInPresetDBSO.PanelResourcesRoot + "/";

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

            if (path.Contains(marker))
                return Path.GetDirectoryName(path)?.Replace('\\', '/');
        }

        Directory.CreateDirectory(Path.GetFullPath(DefaultFolder));
        AssetDatabase.Refresh();
        return DefaultFolder;
    }

    private static void EnsureSpriteImport(string assetPath)
    {
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            return;

        bool dirty = false;

        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }

        if (dirty)
            importer.SaveAndReimport();
    }

    private static Color Rgb(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f, 1f);

    // 점에서 선분까지의 거리.
    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        float t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }
}
