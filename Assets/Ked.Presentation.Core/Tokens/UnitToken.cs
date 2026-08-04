using System;
using System.Globalization;

namespace Ked.Presentation.Core
{
    /// <summary>
    /// 거리 단위 토큰 규약.
    ///   1u = 40px  (기준 스테이지 폭 1920 / 48)
    ///
    /// 음수 허용 여부가 커맨드마다 다르다 — 그래서 진입점이 둘.
    /// </summary>
    public static class UnitToken
    {
        public const float ReferenceStageWidth = 1920f;
        public const float StageWidthDivisor = 48f;
        public const float UnitPixels = ReferenceStageWidth / StageWidthDivisor; // 40px

        public static float UnitsToPixels(float units) => units * UnitPixels;

        /// <summary>"3u" / "3" -> 픽셀. 음수는 0으로 클램프한다.</summary>
        public static bool TryParsePixels(string token, out float pixels)
        {
            if (!TryParseUnits(token, out float units))
            {
                pixels = 0f;
                return false;
            }

            pixels = Math.Max(0f, units) * UnitPixels;
            return true;
        }

        /// <summary>"-3u" 를 그대로 살린다.</summary>
        public static bool TryParseSignedPixels(string token, out float pixels)
        {
            if (!TryParseUnits(token, out float units))
            {
                pixels = 0f;
                return false;
            }

            pixels = units * UnitPixels;
            return true;
        }

        private static bool TryParseUnits(string token, out float units)
        {
            units = 0f;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            string normalized = token.Trim().ToLowerInvariant();

            if (normalized.EndsWith("u", StringComparison.Ordinal))
                normalized = normalized.Substring(0, normalized.Length - 1);

            return float.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out units);
        }
    }
}