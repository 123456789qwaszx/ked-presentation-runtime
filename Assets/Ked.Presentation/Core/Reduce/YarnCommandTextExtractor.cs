using System;
using System.Collections.Generic;
using System.Text;

namespace Ked.Presentation.Core
{
    /// <summary>
    /// 대사 라인 경계 하나. Commands는 "직전 대사 이후 ~ 이 대사까지" 사이의 커맨드들이다.
    /// 파일 끝의 대사 없는 꼬리 커맨드들은 LineText가 null인 마지막 항목으로 남는다.
    /// </summary>
    public sealed class YarnLineGroup
    {
        /// <summary>대사 원문(태그 · 표지 · 인라인 명령 마커 제외). 꼬리 그룹이면 null.</summary>
        public string LineText;

        /// <summary>
        /// #line 태그 값. **접두사를 포함한 전체**다 — Yarn 런타임의 라인 ID가
        /// "line:316f20c1" 형태이므로 접두사를 벗기면 조회가 빗나간다.
        /// 태그가 없으면 null (이 프로젝트의 원문이 그렇다 — 하네스는 순서 커서로 간다).
        /// </summary>
        public string LineId;

        /// <summary>
        /// 이 줄에서 실행되는 커맨드 — 줄 머리의 것(쓴 순서), 그 뒤에 표지 자리의 것(&lt;&lt;at&gt;&gt; · 옛 [cmd]),
        /// 마지막에 &lt;&lt;at end …&gt;&gt;. 순서는 재생과 같은 LineBeatSchedule이 정한다(docs/PLAN_line-beat.md R2 · R3).
        /// 줄이 끝났을 때의 무대가 이 열을 접은 것과 같다.
        /// </summary>
        public readonly List<StageCommand> Commands = new();
    }

    /// <summary>노드 하나의 라인 그룹들. 한 파일에 여러 노드가 있다(Setup + Story).</summary>
    public sealed class YarnNodeGroups
    {
        public string NodeName;
        public readonly List<YarnLineGroup> Groups = new();
    }

    /// <summary>
    /// yarn "원문"에서 커맨드 열을 라인 경계 단위로 뽑는다 — 등가성 하네스 전용.
    ///
    /// **원문을 접는 것이 가장 강한 대조다.** 브리지가 이미 파싱한 커맨드를 받아 접으면
    /// "파싱은 맞다고 치고" 비교하는 셈이다. 원문에서 직접 뽑으면 브리지 파싱과
    /// 코어 파싱이 다를 때 여기서 드러난다.
    ///
    /// 용도 한정: 이것은 Yarn 문법 해석기가 아니다. 대표 에피소드처럼 선형인 파일에서
    /// "재생이 만나는 순서 그대로"의 커맨드 열을 얻는 것이 목적이다. 분기(if/jump)·옵션(->)은
    /// 다루지 않으며, 만나면 커맨드가 아니라 경고 목록에 남긴다 — 그런 파일은 이 추출기로
    /// 접으면 안 된다는 신호다.
    /// </summary>
    public static class YarnCommandTextExtractor
    {
        /// <summary>
        /// 무대 스텝 — 여기까지 쌓인 커맨드를 대사 없이 실행하는 스텝 경계다. 무대 커맨드가 아니다.
        /// 접힘은 줄 단위로 쌓으므로 그룹을 가르지 않는다: 앞의 커맨드는 다음 대사의 그룹에 그대로 든다.
        /// </summary>
        public const string StageStepCommand = "step";

        /// <summary>기다리는 명령 — 시간만 쓴다. 무대 상태는 접을 것이 없다(StageReducer).</summary>
        public const string PauseCommand = "pause";

        /// <summary>
        /// 기다리는 명령인가 — pause · screen_cover(덮기). 대사 줄의 머리 · 자리에는 쓸 수 없고 무대 스텝에서만 기다린다
        /// (docs/PLAN_line-beat.md D3 · R8). 재생에서는 무대 파서가 이 둘의 스펙에 wait를 세운다 — 두 목록이 같아야 한다
        /// (EditMode 시험이 맞춰 본다). screen_cover_clear는 기다리지 않는다.
        /// </summary>
        public static bool IsWaitingCommand(string name) => name == PauseCommand || name == "screen_cover";

        /// <summary>
        /// 옛 인라인 명령 마커의 이름 — 대사 글 안의 [cmd="명령 한 줄"/]. 따옴표 안은 &lt;&lt; &gt;&gt; 안과 같은 글이다.
        /// 자리 표지 [beat=a/] + &lt;&lt;at a 명령&gt;&gt;로 옮기는 동안만 받는다(docs/PLAN_line-beat.md L5에서 걷는다).
        /// 재생은 Yarn의 마크업 파서로 읽는다(Playback의 InlineCommandTrack). 여기서는 이 두 마커만 읽는다 —
        /// 코어는 Yarn을 모른다.
        /// </summary>
        public const string InlineCommandMarker = "cmd";

        /// <summary>파일 전체를 노드 구분 없이 하나의 흐름으로 뽑는다(단일 노드 파일용).</summary>
        public static List<YarnLineGroup> Extract(
            string yarnText, string sourceName, List<string> warnings = null)
        {
            List<YarnLineGroup> flattened = new();

            foreach (YarnNodeGroups node in ExtractNodes(yarnText, sourceName, warnings))
                flattened.AddRange(node.Groups);

            return flattened;
        }

        /// <summary>노드별로 뽑는다. 한 파일에 Setup 노드와 Story 노드가 함께 있다.</summary>
        public static List<YarnNodeGroups> ExtractNodes(
            string yarnText, string sourceName, List<string> warnings = null)
        {
            if (yarnText == null)
                throw new ArgumentNullException(nameof(yarnText));

            List<YarnNodeGroups> nodes = new();
            YarnNodeGroups node = null;
            YarnLineGroup current = new();
            string pendingTitle = null;
            bool inHeader = true;

            // <<at 표지 명령>> — 다음 대사 줄의 것. 그 줄에서 머리 명령 뒤에 표지 순서로 접는다(R3).
            List<AtCommand> pendingAt = new();
            List<LineMarker> markers = new();
            List<string> problems = new();

            // 이 줄 그룹에서 대사 줄 머리가 시작하는 자리 — 그 앞은 무대 스텝(<<step>>)의 것이다.
            int headStart = 0;

            string[] lines = yarnText.Replace("\r\n", "\n").Split('\n');

            // R1 — <<at>> 다음이 대사 줄이 아니다. 재생은 경고하고 그 스텝의 머리 뒤에 붙인다 — 접기도 그렇게.
            void FlushStrayAt(string where, string source)
            {
                if (pendingAt.Count == 0)
                    return;

                warnings?.Add($"{source} <<at>> {pendingAt.Count}개 다음이 대사 줄이 아니라 {where}이다 — 붙을 줄이 없어 그 스텝의 머리 뒤에 둔다.");

                foreach (AtCommand at in pendingAt)
                    current.Commands.Add(at.Command);

                pendingAt.Clear();
            }

            void FlushNode()
            {
                if (node == null)
                    return;

                // 꼬리 커맨드(대사 없이 노드가 끝나는 경우)도 버리지 않는다.
                if (current.Commands.Count > 0)
                    node.Groups.Add(current);

                nodes.Add(node);
                node = null;
                current = new YarnLineGroup();
                headStart = 0;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripComment(lines[i]).Trim();

                // 파일 선두 BOM은 공백이 아니라 Trim에 안 걸린다.
                line = line.TrimStart('﻿');

                if (line.Length == 0)
                    continue;

                // 노드 헤더: title: ~ --- 사이. === 는 노드 끝.
                if (inHeader)
                {
                    if (line.StartsWith("title:", StringComparison.Ordinal))
                        pendingTitle = line.Substring("title:".Length).Trim();

                    if (line == "---")
                    {
                        node = new YarnNodeGroups { NodeName = pendingTitle };
                        pendingTitle = null;
                        inHeader = false;
                    }

                    continue;
                }

                if (line == "===")
                {
                    FlushStrayAt("노드 끝", $"{sourceName}:{i + 1}");
                    FlushNode();
                    inHeader = true;
                    continue;
                }

                if (line.StartsWith("<<", StringComparison.Ordinal))
                {
                    if (TryParseCommand(line, $"{sourceName}:{i + 1}", out StageCommand command))
                    {
                        if (command.Name == StageStepCommand)
                        {
                            FlushStrayAt("무대 스텝", command.Source);
                            headStart = current.Commands.Count;
                            continue;
                        }

                        if (command.Name == LineBeatSchedule.AtCommandName)
                        {
                            if (LineBeatSchedule.TryReadAt(command.Args, command.Source, out AtCommand at, out string error))
                                pendingAt.Add(at);
                            else
                                warnings?.Add($"{command.Source} {error}: {line}");

                            continue;
                        }

                        if (IsFlowControl(command.Name))
                        {
                            warnings?.Add(
                                $"{sourceName}:{i + 1} 분기/제어 커맨드 '{command.Name}' — " +
                                "이 추출기는 선형 파일 전용이다. 결과를 믿지 말 것.");
                        }

                        current.Commands.Add(command);
                    }
                    else
                    {
                        warnings?.Add($"{sourceName}:{i + 1} 커맨드를 읽지 못했다: {line}");
                    }

                    continue;
                }

                if (line.StartsWith("->", StringComparison.Ordinal))
                {
                    FlushStrayAt("선택지", $"{sourceName}:{i + 1}");
                    warnings?.Add($"{sourceName}:{i + 1} 옵션(->)은 다루지 않는다 — 선형 파일 전용.");
                    continue;
                }

                // 대사 라인 = 경계. 표지 자리의 명령은 줄 머리 커맨드 뒤에 실행 순서대로 붙는다 — 재생과 같은 함수가 정한다.
                string text = StripTags(line, out string lineId);
                string lineSource = $"{sourceName}:{i + 1}";

                markers.Clear();
                problems.Clear();
                current.LineText = StripLineMarkers(text, lineSource, markers, warnings);

                // R8 — 대사 줄에는 시간이 없다. 재생은 머리의 기다림을 내리고(오류), 자리의 것은 실행하지 않는다.
                for (int h = headStart; h < current.Commands.Count; h++)
                {
                    if (IsWaitingCommand(current.Commands[h].Name))
                        warnings?.Add($"{current.Commands[h].Source} 대사 줄 머리의 기다리는 명령 {current.Commands[h]} — 대사 줄에는 시간이 없다. 줄 앞에 <<step>>을 두고 거기서 기다린다.");
                }

                foreach (AtCommand at in pendingAt)
                {
                    if (IsWaitingCommand(at.Command.Name))
                        warnings?.Add($"{at.Command.Source} {at} — 기다리는 명령은 대사 줄의 자리에 쓸 수 없다(재생은 실행하지 않는다).");
                }

                foreach (ScheduledCommand scheduled in LineBeatSchedule.Build(markers, pendingAt, problems))
                    current.Commands.Add(scheduled.Command);

                foreach (string problem in problems)
                    warnings?.Add($"{lineSource} {problem}");

                pendingAt.Clear();
                current.LineId = lineId;
                node?.Groups.Add(current);
                current = new YarnLineGroup();
                headStart = 0;
            }

            FlushNode();

            return nodes;
        }

        /// <summary>"&lt;&lt;cmd a "b c" d&gt;&gt;" → StageCommand. 따옴표 인자를 존중한다.</summary>
        public static bool TryParseCommand(string line, string source, out StageCommand command)
        {
            command = default;

            string trimmed = line.Trim();

            if (!trimmed.StartsWith("<<", StringComparison.Ordinal) ||
                !trimmed.EndsWith(">>", StringComparison.Ordinal) ||
                trimmed.Length < 4)
            {
                return false;
            }

            string body = trimmed.Substring(2, trimmed.Length - 4).Trim();

            if (body.Length == 0)
                return false;

            List<string> tokens = SplitRespectingQuotes(body);

            if (tokens.Count == 0)
                return false;

            string name = tokens[0];
            tokens.RemoveAt(0);

            command = new StageCommand(name, tokens.ToArray(), source);
            return true;
        }

        /// <summary>
        /// 대사 글에서 표지를 떼어 글 순서대로 markers 뒤에 붙이고, 표지를 뺀 글을 돌려준다.
        ///   [beat=a/]       자리 표지(따옴표는 있어도 없어도 된다)
        ///   [cmd="…"/]     옛 인라인 명령 — 명령으로 읽는다
        /// 표지의 자리는 표지를 뺀 글의 글자 자리다. Yarn처럼 따옴표 안의 역슬래시는 벗기고,
        /// 공백 뒤(또는 줄 머리)의 표지는 뒤따르는 공백 하나를 같이 먹는다.
        /// </summary>
        public static string StripLineMarkers(
            string text, string source, List<LineMarker> markers, List<string> warnings = null)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('[') < 0)
                return text;

            StringBuilder plain = new(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] != '[' || !TryReadMarker(text, i, out int end, out string name, out string value))
                {
                    plain.Append(text[i++]);
                    continue;
                }

                if (name == LineBeatSchedule.MarkerName)
                    markers.Add(LineMarker.ForBeat(value, plain.Length));
                else if (TryParseCommand($"<<{value}>>", source, out StageCommand command))
                    markers.Add(LineMarker.ForCommand(command, plain.Length));
                else
                    warnings?.Add($"{source} 인라인 명령을 읽지 못했다: {text.Substring(i, end - i)}");

                bool afterWhitespace = plain.Length == 0 || char.IsWhiteSpace(plain[plain.Length - 1]);
                i = end;

                if (afterWhitespace && i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;
            }

            return plain.ToString();
        }

        // [ 이름 = 값 / ] — 이름은 beat 또는 cmd. cmd의 값은 따옴표 안이어야 한다(명령 한 줄에 공백이 있다).
        // start는 '['의 자리. 읽었으면 end는 ']' 다음 자리.
        private static bool TryReadMarker(string text, int start, out int end, out string name, out string value)
        {
            end = start;
            value = null;

            int i = SkipWhitespace(text, start + 1);

            if (string.CompareOrdinal(text, i, LineBeatSchedule.MarkerName, 0, LineBeatSchedule.MarkerName.Length) == 0)
                name = LineBeatSchedule.MarkerName;
            else if (string.CompareOrdinal(text, i, InlineCommandMarker, 0, InlineCommandMarker.Length) == 0)
                name = InlineCommandMarker;
            else
            {
                name = null;
                return false;
            }

            i = SkipWhitespace(text, i + name.Length);

            if (i >= text.Length || text[i] != '=')
                return false;

            i = SkipWhitespace(text, i + 1);

            StringBuilder read = new();

            if (i < text.Length && text[i] == '"')
            {
                for (i++; i < text.Length && text[i] != '"'; i++)
                {
                    // 역슬래시는 다음 글자를 그대로 싣는다(\" → ").
                    if (text[i] == '\\' && i + 1 < text.Length)
                        i++;

                    read.Append(text[i]);
                }

                if (i >= text.Length)
                    return false;

                i++;
            }
            else if (name == LineBeatSchedule.MarkerName)
            {
                for (; i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '/' && text[i] != ']'; i++)
                    read.Append(text[i]);
            }
            else
            {
                return false;
            }

            i = SkipWhitespace(text, i);

            if (i >= text.Length || text[i] != '/')
                return false;

            i = SkipWhitespace(text, i + 1);

            if (i >= text.Length || text[i] != ']')
                return false;

            end = i + 1;
            value = read.ToString();
            return true;
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            return index;
        }

        private static bool IsFlowControl(string name)
        {
            switch (name)
            {
                // 선형성을 깨는 것만 경고한다. set/declare는 무대 상태와 무관하고
                // 분기하지도 않으므로 그냥 흘러가 리듀서의 Unhandled로 남는다.
                case "if":
                case "elseif":
                case "else":
                case "endif":
                case "jump":
                case "stop":
                    return true;

                default:
                    return false;
            }
        }

        private static List<string> SplitRespectingQuotes(string body)
        {
            List<string> tokens = new();
            StringBuilder sb = new();
            bool inQuotes = false;

            foreach (char c in body)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }

                    continue;
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            return tokens;
        }

        private static string StripComment(string line)
        {
            int index = line.IndexOf("//", StringComparison.Ordinal);
            return index < 0 ? line : line.Substring(0, index);
        }

        private static string StripTags(string line, out string lineId)
        {
            lineId = null;

            int hash = line.IndexOf('#');

            if (hash < 0)
                return line.Trim();

            string tags = line.Substring(hash);
            string text = line.Substring(0, hash).Trim();

            foreach (string tag in tags.Split('#'))
            {
                string t = tag.Trim();

                // Yarn 런타임의 라인 ID는 "line:" 접두사를 포함한 전체다
                // (#line:abc 태그 → ID "line:abc"). 접두사를 벗기면 조회가 빗나간다.
                if (t.StartsWith("line:", StringComparison.Ordinal))
                    lineId = t;
            }

            return text;
        }
    }
}