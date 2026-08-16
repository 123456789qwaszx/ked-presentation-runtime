// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// /// <summary>
// /// "정식" PlayerState.
// /// - 런타임 메모리 기반이지만, 나중에 직렬화/저장 가능한 형태를 목표로 한다.
// /// - 진행 기록은 HashSet으로 유지(중복 방지, 조회 빠름).
// /// </summary>
// public sealed class PlayerState
// {
//     public int Intuition;
//     public int Analysis;
//     public int Chaos;
//
//     // 진행 기록
//     public readonly HashSet<string> UnlockedEpisodes = new(StringComparer.Ordinal);
//     public readonly HashSet<string> ClearedEpisodes  = new(StringComparer.Ordinal);
//
//     // 엔딩 “봤다” 기록 (ownerEpisodeId와 분리)
//     public readonly HashSet<string> SeenEpisodeEndings = new(StringComparer.Ordinal);
//     public readonly HashSet<string> SeenEndings = new(StringComparer.Ordinal);
//     
//     public static PlayerState CreateNew(int intuition = 33, int analysis = 33, int chaos = 33)
//     {
//         var playerState = new PlayerState
//         {
//             Intuition = Mathf.Clamp(intuition, 0, 100),
//             Analysis  = Mathf.Clamp(analysis,  0, 100),
//             Chaos     = Mathf.Clamp(chaos,     0, 100)
//         };
//         
//         return playerState;
//     }
//
//     public PlayerStateSnapshot Snapshot()
//     {
//         return new PlayerStateSnapshot(Intuition, Analysis, Chaos);
//     }
// }
//
// public readonly struct PlayerStateSnapshot
// {
//     public readonly int Intuition;
//     public readonly int Analysis;
//     public readonly int Chaos;
//
//     public PlayerStateSnapshot(int intuition, int analysis, int chaos)
//     {
//         Intuition = Mathf.Clamp(intuition, 0, 100);
//         Analysis  = Mathf.Clamp(analysis,  0, 100);
//         Chaos     = Mathf.Clamp(chaos,     0, 100);
//     }
// }