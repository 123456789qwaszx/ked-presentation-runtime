// using System;
//
// [Serializable]
// public struct VNOptionEffectPreview
// {
//     public string StatKey { get; private set; }
//     public int MinValue { get; private set; }
//     public int MaxValue { get; private set; }
//
//     public bool HasRange
//     {
//         get { return MinValue != MaxValue; }
//     }
//
//     public VNOptionEffectPreview(string statKey, int minValue, int maxValue)
//     {
//         StatKey = statKey ?? string.Empty;
//         MinValue = minValue;
//         MaxValue = maxValue;
//     }
//
//     public string ToDisplayText()
//     {
//         if (string.IsNullOrEmpty(StatKey))
//             return string.Empty;
//
//         string displayName = VNOptionEffectDisplayNameResolver.Resolve(StatKey);
//
//         if (HasRange)
//             return string.Format("{0} {1:+#;-#;0}~{2:+#;-#;0}", displayName, MinValue, MaxValue);
//
//         return string.Format("{0} {1:+#;-#;0}", displayName, MinValue);
//     }
// }