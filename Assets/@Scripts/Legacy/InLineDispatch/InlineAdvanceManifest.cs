// using System;
// using System.Collections.Generic;
// using Yarn.Markup;
//
// // ─────────────────────────────────────────────────────────────────────────────
// // InlineAdvanceManifest
// //
// // 한 메인 라인 안의 [advance/] point 마커를 "plain-text 위치 오름차순"으로 굳혀
// // 두는 라인-스코프 불변 명세 + 단조 소비 커서.
// //
// // 계약:
// //  - LocalizedLine.TextWithoutCharacterName(타입라이터가 실제로 드러내는
// //    MarkupParseResult)에서 만든다. 그래야 마커 Position이 타입라이터의
// //    character index 공간과 일치한다. (핸들러의 OnPrepareForLine에 들어오는
// //    line 파라미터가 바로 그것이다.)
// //  - [advance/]는 point tag(Length == 0)만 인정한다. range tag는 styling/semantic
// //    span이므로 무시한다. (기존 InlineEventMarkupHandler의 attr.Length>0 무시 규칙 유지.)
// //  - advance는 "이벤트"가 아니라 presentation table cursor를 1칸 소비하라는
// //    결정적 sync point다. ordinal 0..N-1 순서는 모든 재생 경로
// //    (normal / fast-forward / seek pass-through / rollback / load)에서 동일하다.
// //  - 소비는 ordinal 단조 증가로만 진행된다(멱등성). 같은 ordinal 재발화 불가,
// //    건너뛴 ordinal 부활 불가.
// //
// // 비계약:
// //  - 실제 sub lane advance 디스패치(SyncGate 경로)는 host가 한다.
// //  - cancellation / run-generation fence는 line run / lane run 쪽 책임이다.
// // ─────────────────────────────────────────────────────────────────────────────
// public sealed class InlineAdvanceManifest
// {
//     public const string DefaultMarkerName = "adv";
//
//     private readonly int[] _positions; // 오름차순. 같은 위치 중복 보존.
//     private int _cursor;               // 다음 소비 ordinal. [0, Count].
//
//     public static InlineAdvanceManifest Empty { get; } =
//         new(Array.Empty<int>());
//
//     private InlineAdvanceManifest(int[] positions)
//     {
//         _positions = positions;
//     }
//
//     public int Count => _positions.Length;
//     public int ConsumedCount => _cursor;
//     public bool IsEmpty => _positions.Length == 0;
//     public bool IsExhausted => _cursor >= _positions.Length;
//
//     public static InlineAdvanceManifest Build(
//         MarkupParseResult markup,
//         string markerName = DefaultMarkerName)
//     {
//         if (markup.Attributes == null || markup.Attributes.Count == 0)
//             return Empty;
//
//         string text = markup.Text ?? string.Empty;
//         int textLength = text.Length;
//
//         List<int> positions = null;
//
//         for (int i = 0; i < markup.Attributes.Count; i++)
//         {
//             MarkupAttribute attribute = markup.Attributes[i];
//
//             if (!string.Equals(attribute.Name, markerName, StringComparison.Ordinal))
//                 continue;
//
//             // Inline advance는 point event 전용. range [advance]...[/advance]는 계약 밖.
//             if (attribute.Length > 0)
//                 continue;
//
//             // 빈 텍스트 라인은 character callback이 없다(현재 타입라이터 기준).
//             // textless presentation advance가 필요하면 presentation beat / 커맨드 라인을 쓴다.
//             if (textLength <= 0)
//                 continue;
//
//             int position = attribute.Position;
//
//             if (position < 0)
//                 position = 0;
//
//             // 현재 ActionMarkupHandler는 visible character '직전'에만 발화한다.
//             // 줄 끝 위치(position == textLength)는 callback index로 도달하지 못하므로
//             // 마지막 글자에 붙도록 보정한다. (기존 NormalizeIndexFast와 동일한 계약.)
//             if (position >= textLength)
//                 position = textLength - 1;
//
//             (positions ??= new List<int>()).Add(position);
//         }
//
//         if (positions == null)
//             return Empty;
//
//         positions.Sort();
//         return new InlineAdvanceManifest(positions.ToArray());
//     }
//
//     // 타입라이터가 charIndex 글자를 '아직 드러내기 전' 호출하는 게이트.
//     // 그 위치(또는 그 이전)에 걸린 미소비 advance가 있으면 true.
//     //
//     // '<= charIndex'인 이유: fast-forward/hurry로 위치를 건너뛰어도 누락 없이
//     // 순서대로 회수하기 위해서다. (normal 경로에서는 사실상 ==로 동작한다.)
//     public bool HasPendingAt(int charIndex)
//     {
//         return !IsExhausted && _positions[_cursor] <= charIndex;
//     }
//
//     // 다음 ordinal 하나를 소비. normal / fast-forward 공통.
//     public bool TryConsumeNext(out int ordinal)
//     {
//         if (IsExhausted)
//         {
//             ordinal = -1;
//             return false;
//         }
//
//         ordinal = _cursor;
//         _cursor++;
//         return true;
//     }
//
//     // seek pass-through / rollback 재구성용. 남은 advance 수를 반환하고 전부 소비 처리.
//     // 주의: 타겟 라인 자신의 advance는 drain하면 안 된다(재개 후 normal 경로로 발화).
//     public int DrainRemaining()
//     {
//         int remaining = _positions.Length - _cursor;
//         _cursor = _positions.Length;
//         return remaining;
//     }
//
//     // 같은 라인 재진입(rollback/load 타겟) 시 커서만 되감는다. _positions는 불변.
//     public void Reset()
//     {
//         _cursor = 0;
//     }
// }