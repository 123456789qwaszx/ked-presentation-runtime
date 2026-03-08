#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PortraitEditorCache
{
    private static PortraitGeneratedDBSO _db;
    private static Dictionary<string, HashSet<string>> _variantsByChar;
    private static Dictionary<(string, string), HashSet<string>> _emotionsByCharVariant;

    public static void Rebuild(PortraitGeneratedDBSO db)
    {
        _db = db;
        _variantsByChar = new();
        _emotionsByCharVariant = new();

        if (_db == null || _db.entries == null)
            return;

        foreach (var e in _db.entries)
        {
            if (!_variantsByChar.TryGetValue(e.characterId, out var vset))
            {
                vset = new HashSet<string>();
                _variantsByChar[e.characterId] = vset;
            }
            vset.Add(e.variantKey);

            var key = (e.characterId, e.variantKey);
            if (!_emotionsByCharVariant.TryGetValue(key, out var eset))
            {
                eset = new HashSet<string>();
                _emotionsByCharVariant[key] = eset;
            }
            eset.Add(e.emotionKey);
        }
    }

    public static List<string> GetCharacters()
        => _variantsByChar?.Keys.OrderBy(x => x).ToList() ?? new();

    public static List<string> GetVariants(string character)
    {
        if (string.IsNullOrEmpty(character)) return new();
        return _variantsByChar != null && _variantsByChar.TryGetValue(character, out var set)
            ? set.OrderBy(x => x).ToList()
            : new();
    }

    public static List<string> GetEmotions(string character, string variant)
    {
        if (string.IsNullOrEmpty(character)) return new();
        
        var resolvedVariant = ResolveVariant(character, variant);
        
        return _emotionsByCharVariant != null &&
               _emotionsByCharVariant.TryGetValue((character, resolvedVariant), out var set)
            ? set.OrderBy(x => x).ToList()
            : new();
    }
    
    public static Sprite GetSprite(
        string character,
        string variant,
        string emotion)
    {
        if (_db == null || _db.entries == null)
            return null;

        if (string.IsNullOrEmpty(character) ||
            string.IsNullOrEmpty(emotion))
            return null;

        // Variant 해석 (예: "b" → "Amber_b")
        var resolvedVariant = ResolveVariant(character, variant);
        
        // Emotion 해석 (예: "5" → "05")
        var resolvedEmotion = ResolveEmotion(character, resolvedVariant, emotion);

        var entry = _db.entries.FirstOrDefault(e =>
            e.characterId == character &&
            e.variantKey == resolvedVariant &&
            e.emotionKey == resolvedEmotion
        );

        return entry.sprite;
    }

    /// <summary>
    /// Variant를 전체 형식으로 확장합니다.
    /// 예: character="Amber", variant="b" → "Amber_b"
    /// </summary>
    private static string ResolveVariant(string character, string variant)
    {
        if (string.IsNullOrEmpty(variant))
            return "";
        
        // 이미 정확히 일치하는 variant가 있으면 그대로 반환
        if (_variantsByChar != null && 
            _variantsByChar.TryGetValue(character, out var variants))
        {
            // 정확한 매칭
            if (variants.Contains(variant))
                return variant;
            
            // "Character_variant" 형식으로 확장 시도
            var expanded = $"{character}_{variant}";
            if (variants.Contains(expanded))
                return expanded;
            
            // "_variant"로 끝나는 것 찾기
            var suffix = $"_{variant}";
            var match = variants.FirstOrDefault(v => v.EndsWith(suffix));
            if (match != null)
                return match;
        }
        
        return variant;
    }

    /// <summary>
    /// Emotion을 전체 형식으로 확장합니다.
    /// 예: "5" → "05"
    /// </summary>
    private static string ResolveEmotion(string character, string variant, string emotion)
    {
        if (string.IsNullOrEmpty(emotion))
            return "";
    
        var key = (character, variant);
    
        if (_emotionsByCharVariant != null &&
            _emotionsByCharVariant.TryGetValue(key, out var emotions))
        {
            if (emotions.Contains(emotion))
                return emotion;
        
            if (int.TryParse(emotion, out int num))
            {
                var padded = num.ToString("D2"); // 2자리로 패딩
                if (emotions.Contains(padded))
                    return padded;
            }
        }
    
        return emotion;
    }
}
#endif