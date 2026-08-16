// using System;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;
//
// public sealed class VNOptionEffectPreviewResolver
// {
//     private static readonly Regex EffectRegex = new Regex(
//         @"^(?<key>[A-Za-z_][A-Za-z0-9_]*):(?<min>[+-]?\d+)(?:~(?<max>[+-]?\d+))?$",
//         RegexOptions.Compiled);
//
//     public List<VNOptionEffectPreview> Resolve(string[] metadata)
//     {
//         var result = new List<VNOptionEffectPreview>();
//
//         if (metadata == null || metadata.Length == 0)
//             return result;
//
//         for (int i = 0; i < metadata.Length; i++)
//         {
//             string tag = NormalizeMetadataTag(metadata[i]);
//
//             if (string.IsNullOrEmpty(tag))
//                 continue;
//
//             VNOptionEffectPreview preview;
//             if (TryResolveEffectTag(tag, out preview))
//                 result.Add(preview);
//         }
//
//         return result;
//     }
//
//     private static bool TryResolveEffectTag(
//         string tag,
//         out VNOptionEffectPreview preview)
//     {
//         preview = default(VNOptionEffectPreview);
//
//         Match match = EffectRegex.Match(tag);
//
//         if (!match.Success)
//             return false;
//
//         string key = match.Groups["key"].Value;
//
//         int min;
//         if (!int.TryParse(match.Groups["min"].Value, out min))
//             return false;
//
//         int max = min;
//         if (match.Groups["max"].Success)
//         {
//             if (!int.TryParse(match.Groups["max"].Value, out max))
//                 return false;
//         }
//
//         preview = new VNOptionEffectPreview(key, min, max);
//         return true;
//     }
//
//     private static string NormalizeMetadataTag(string tag)
//     {
//         if (string.IsNullOrWhiteSpace(tag))
//             return string.Empty;
//
//         tag = tag.Trim();
//
//         if (tag.StartsWith("#"))
//             tag = tag.Substring(1);
//
//         return tag.Trim().ToLowerInvariant();
//     }
// }