#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ked.Samples.UI.Editor
{
    // Optional, manual-only tool. It does not create scenes or touch runtime code.
    public static class KedUIArtImporter
    {
        private const string DefaultRoot = "Assets/Samples/KedUI";
        [Serializable] private sealed class Manifest { public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string name;
            public string file;
            public int width;
            public int height;
            public int[] border;
            public int pixelsPerUnit;
        }

        [MenuItem("Tools/Ked UI/Apply Art Import Settings")]
        public static void Apply()
        {
            ApplyAt(DefaultRoot);
        }

        public static void ApplyAt(string root)
        {
            string manifestPath = root.TrimEnd('/') + "/asset-manifest.json";
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Ked UI manifest not found. Keep the package under Assets/Samples/KedUI, or call ApplyAt with the new root.", manifestPath);
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.assets == null)
                throw new InvalidDataException("Ked UI manifest has no assets.");
            AssetDatabase.Refresh();
            // ked: 그림을 화면 묶음으로 나눠 두므로(Common/Sprites · Screens/*/Sprites) 자리는 manifest의 경로가 아니라
            // 파일 이름으로 찾는다. manifest는 받은 그대로("Art/…") 두고, 같은 이름이 둘이면 멈춘다.
            var problems = new System.Collections.Generic.List<string>();
            UISpriteIndex index = KedUIUnits.Art(problems);
            if (problems.Count > 0)
                throw new InvalidDataException(string.Join("\n", problems));
            int applied = 0;
            foreach (Entry entry in manifest.assets)
            {
                string relative = entry.file.Replace('\\', '/');
                if (!relative.StartsWith("Art/", StringComparison.Ordinal) || relative.Contains("..") || !relative.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Unexpected asset path: " + relative);
                if (!index.TryGetPath(Path.GetFileNameWithoutExtension(relative), out string path))
                    throw new FileNotFoundException("PNG not found under " + root + ": " + relative);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("PNG was not imported: " + path);
                int[] b = entry.border;
                if (b == null || b.Length != 4 || b[0] < 0 || b[1] < 0 || b[2] < 0 || b[3] < 0 || b[0] + b[2] >= entry.width || b[1] + b[3] >= entry.height)
                    throw new InvalidDataException("Invalid sprite border: " + entry.name);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit > 0 ? entry.pixelsPerUnit : 100;
                importer.spriteBorder = new Vector4(b[0], b[1], b[2], b[3]);
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                applied++;
            }
            Debug.Log("[Ked UI] Applied sprite import settings: " + applied + ". Set each UI Image.type from the manifest (Simple/Sliced). Scene wiring is not included.");
        }
    }
}
#endif
