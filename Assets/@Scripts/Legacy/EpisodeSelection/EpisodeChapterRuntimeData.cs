// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class EpisodeChapterYarnMap
// {
//     [Serializable]
//     public sealed class Entry
//     {
//         public string EpisodeId;
//         public EpisodeNodeKind Kind;
//         public string YarnNodeName;
//     }
//     
//     public string ChapterId;
//     public string DisplayName;
//     public string StartEpisodeId;
//
//     private readonly Dictionary<string, Entry> _entryByEpisodeId = new(StringComparer.Ordinal);
//
//     public void AddEntry(Entry entry)
//     {
//         if (entry == null)
//             return;
//
//         if (string.IsNullOrEmpty(entry.EpisodeId))
//             return;
//
//         _entryByEpisodeId[entry.EpisodeId] = entry;
//     }
//
//     public string GetYarnNodeName(string episodeId)
//     {
//         Entry entry = _entryByEpisodeId[episodeId];
//         return entry.YarnNodeName;
//     }
//
//     public Entry GetEntry(string episodeId)
//     {
//         return _entryByEpisodeId[episodeId];
//     }
// }