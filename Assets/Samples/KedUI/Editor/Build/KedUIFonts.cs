using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// TMP 글꼴 자산. 시안의 Noto Sans KR은 가변 글꼴(wght 100–900)이고 기본 인스턴스가 Thin이라 TMP가 그대로 쓰면
    /// 가는 글자가 된다(IMPLEMENTATION §3). 그래서 같은 글꼴에서 뽑은 정적 굵기 둘(Medium 500 · Bold 700, OFL 그대로)을 쓴다.
    /// 둘 다 동적 SDF — 한국어 글자는 쓰는 대로 아틀라스에 들어간다. Bold는 Medium의 굵기 표(700)에 걸어
    /// 글자 스타일 Bold가 가짜 굵게가 아니라 Bold 글꼴로 그려지게 한다.
    /// </summary>
    internal static class KedUIFonts
    {
        public const string FontDir = KedUIKit.Root + "/Fonts";
        public const string OutputDir = KedUIKit.Root + "/Generated/Fonts";

        public static TMP_FontAsset Ensure(List<string> problems)
        {
            KedUIBuild.EnsureFolder(OutputDir);

            TMP_FontAsset bold = EnsureAsset(FontDir + "/NotoSansKR-Bold.ttf", OutputDir + "/NotoSansKR-Bold SDF.asset", problems);
            TMP_FontAsset medium = EnsureAsset(FontDir + "/NotoSansKR-Medium.ttf", OutputDir + "/NotoSansKR-Medium SDF.asset", problems);

            if (medium != null && bold != null)
                LinkBold(medium, bold, problems);

            return medium;
        }

        private static TMP_FontAsset EnsureAsset(string fontPath, string assetPath, List<string> problems)
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                problems.Add($"글꼴 파일 없음: {fontPath}");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                font,
                72,
                7,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            if (asset == null)
            {
                problems.Add($"TMP 글꼴 자산을 만들지 못했다: {fontPath}");
                return null;
            }

            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);

            // 아틀라스와 재질을 글꼴 자산 안에 둔다(TMP 메뉴로 만든 자산과 같은 꼴).
            if (asset.atlasTextures != null && asset.atlasTextures.Length > 0 && asset.atlasTextures[0] != null)
            {
                asset.atlasTextures[0].name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            }

            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            // 빌드에는 비운 아틀라스로 싣는다 — 실행 중 쓰는 글자만 다시 채운다(원본 TTF가 자산에 걸려 있다).
            var so = new SerializedObject(asset);
            SerializedProperty clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null)
            {
                clear.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void LinkBold(TMP_FontAsset medium, TMP_FontAsset bold, List<string> problems)
        {
            var so = new SerializedObject(medium);
            SerializedProperty table = so.FindProperty("m_FontWeightTable");
            if (table == null || !table.isArray)
            {
                problems.Add("TMP 글꼴 굵기 표(m_FontWeightTable)를 찾지 못했다 — Bold는 가짜 굵게로 그려진다.");
                return;
            }

            if (table.arraySize < 10)
                table.arraySize = 10;

            SerializedProperty regular = table.GetArrayElementAtIndex(7).FindPropertyRelative("regularTypeface");
            if (regular == null)
            {
                problems.Add("TMP 글꼴 굵기 표의 regularTypeface 칸이 없다.");
                return;
            }

            regular.objectReferenceValue = bold;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(medium);
        }
    }
}
