using System;
using System.Collections.Generic;

namespace Ked.Presentation.Core
{
    // ─────────────────────────────────────────────────────────────────
    // U12 프리셋 덤프(presets/depth.json · focus-tuning.json)의 전송 타입.
    // EditorJsonUtility 직렬화 모양 그대로다: {"MonoBehaviour": { ...필드... }}.
    // m_* 메타 필드는 담지 않는다(역직렬화가 무시한다).
    // 필드 의미·단위는 ExportedTuning/schema.md가 규범이다.
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public sealed class DepthTuningFileDto
    {
        public DepthTuningBodyDto MonoBehaviour;
    }

    [Serializable]
    public sealed class DepthTuningBodyDto
    {
        public DepthPresetSetDto presets;
        // levelTuning(AnimationCurve)은 담지 않는다 — 커브 폴드는 미지원(레벨 입력은 Unhandled).
    }

    [Serializable]
    public sealed class DepthPresetSetDto
    {
        public DepthPresetDto far;
        public DepthPresetDto back;
        public DepthPresetDto mid;
        public DepthPresetDto front;
        public DepthPresetDto close;
        public DepthPresetDto exp1;
        public DepthPresetDto exp2;

        /// <summary>프리셋 토큰 → 값. 모르는 토큰이면 false — 비슷한 이름으로 잇지 않는다.</summary>
        public bool TryGet(string presetKey, out DepthPresetDto preset)
        {
            switch (presetKey)
            {
                case "far": preset = far; return preset != null;
                case "back": preset = back; return preset != null;
                case "mid": preset = mid; return preset != null;
                case "front": preset = front; return preset != null;
                case "close": preset = close; return preset != null;
                case "exp1": preset = exp1; return preset != null;
                case "exp2": preset = exp2; return preset != null;
                default: preset = null; return false;
            }
        }
    }

    [Serializable]
    public sealed class DepthPresetDto
    {
        public Float2Dto depthY;
        public float depthScale;
        public int preserveFocusPreset;   // CharacterFocusPreset 값 (0/10/20/30/31/40/41)
        public Float2Dto preserveFocusOffset;
    }

    [Serializable]
    public sealed class FocusTuningFileDto
    {
        public FocusTuningBodyDto MonoBehaviour;
    }

    [Serializable]
    public sealed class FocusTuningBodyDto
    {
        public FocusOffsetSetDto baseOffsets;
        public List<FocusEntryDto> entries = new List<FocusEntryDto>();

        public bool TryGetEntry(string key, out FocusEntryDto entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i]?.key, key, StringComparison.Ordinal))
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    [Serializable]
    public sealed class FocusEntryDto
    {
        public string key;
        public Float2Dto defaultOffset;
        public FocusOffsetSetDto offsets;
    }

    [Serializable]
    public sealed class FocusOffsetSetDto
    {
        public Float2Dto feet;
        public Float2Dto body;
        public Float2Dto bust;
        public Float2Dto face;
        public Float2Dto faceAura;
        public Float2Dto handLeft;
        public Float2Dto handRight;

        public bool TryGet(string presetName, out Vec2 offset)
        {
            Float2Dto value = presetName switch
            {
                "feet" => feet,
                "body" => body,
                "bust" => bust,
                "face" => face,
                "faceAura" => faceAura,
                "handLeft" => handLeft,
                "handRight" => handRight,
                _ => null,
            };

            offset = value?.ToVec2() ?? Vec2.Zero;
            return value != null;
        }
    }

    // ── 초상 치수 (portrait-dimensions.json) ──────────────────────────

    [Serializable]
    public sealed class PortraitDimensionsFileDto
    {
        public List<PortraitDimensionDto> entries = new List<PortraitDimensionDto>();

        /// <summary>
        /// 캐릭터의 초상 종횡비(폭/높이)가 변형·표정 전체에서 균일하면 그 값을 준다.
        /// 상이하면 false — 표정 축 없이는 사이징을 접을 수 없다는 뜻이고,
        /// 호출자는 이를 침묵 대신 Unhandled로 남긴다.
        /// </summary>
        public bool TryGetUniformAspect(string characterKey, out float aspect, out string reason)
        {
            aspect = 0f;
            reason = null;

            bool found = false;

            for (int i = 0; i < entries.Count; i++)
            {
                PortraitDimensionDto entry = entries[i];

                if (entry == null || !string.Equals(entry.character, characterKey, StringComparison.Ordinal))
                    continue;

                if (entry.height <= 0f)
                    continue;

                float entryAspect = entry.width / entry.height;

                if (!found)
                {
                    aspect = entryAspect;
                    found = true;
                    continue;
                }

                if (Math.Abs(entryAspect - aspect) > 1e-4f)
                {
                    reason = $"캐릭터 '{characterKey}'의 초상 종횡비가 표정/변형마다 다르다 " +
                             $"({aspect:F4} vs {entryAspect:F4} at {entry.variant}/{entry.emotion}) — 표정 축 폴드가 필요하다";
                    return false;
                }
            }

            if (!found)
            {
                reason = $"캐릭터 '{characterKey}'의 초상 치수가 덤프에 없다";
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public sealed class PortraitDimensionDto
    {
        public string character;
        public string variant;
        public string emotion;
        public float width;
        public float height;
    }

    /// <summary>focus 프리셋 어휘 — 런타임 CharacterFocusPreset과의 대응은 값으로 잇는다.</summary>
    public static class FocusPresetName
    {
        /// <summary>덤프의 preserveFocusPreset 정수 → 이름. 모르면 false.</summary>
        public static bool TryFromEnumValue(int value, out string name)
        {
            name = value switch
            {
                0 => "feet",
                10 => "body",
                20 => "bust",
                30 => "face",
                31 => "faceAura",
                40 => "handLeft",
                41 => "handRight",
                _ => null,
            };

            return name != null;
        }

        /// <summary>yarn 토큰("bust") 정규화. 모르면 false — 추측 보정하지 않는다.</summary>
        public static bool TryNormalizeToken(string token, out string name)
        {
            name = (token ?? "").Trim().ToLowerInvariant() switch
            {
                "feet" => "feet",
                "body" => "body",
                "bust" => "bust",
                "face" => "face",
                "face_aura" or "faceaura" => "faceAura",
                "hand_left" or "handleft" => "handLeft",
                "hand_right" or "handright" => "handRight",
                _ => null,
            };

            return name != null;
        }
    }
}
