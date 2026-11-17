using System;
using System.Collections.Generic;

namespace Ked.Presentation.Core
{
    /// <summary>
    /// 대사 줄의 글 안 표지 하나 — 글 순서대로 온다.
    ///   [beat=a/]          자리 표지. 명령은 줄 앞의 &lt;&lt;at a 명령&gt;&gt;에 있다
    ///   [cmd="명령"/]      명령을 글 안에 든 옛 마커 — 이행 동안만 받는다(docs/PLAN_line-beat.md L5에서 걷는다)
    /// </summary>
    public readonly struct LineMarker
    {
        /// <summary>글자 자리 — 이 글자가 드러나기 직전에 실행한다. 글 끝이면 글 길이.</summary>
        public readonly int Position;

        /// <summary>자리 표지의 이름. 옛 마커면 null.</summary>
        public readonly string Beat;

        /// <summary>옛 마커의 명령. 자리 표지면 쓰지 않는다.</summary>
        public readonly StageCommand Command;

        private LineMarker(int position, string beat, in StageCommand command)
        {
            Position = position;
            Beat = beat;
            Command = command;
        }

        public static LineMarker ForBeat(string name, int position) => new(position, name ?? string.Empty, default);

        public static LineMarker ForCommand(in StageCommand command, int position) => new(position, null, command);
    }

    /// <summary>&lt;&lt;at 표지 명령 …&gt;&gt; 하나 — 다음 대사 줄의 그 표지 자리에서 실행할 명령.</summary>
    public readonly struct AtCommand
    {
        public readonly string Beat;
        public readonly StageCommand Command;

        public AtCommand(string beat, in StageCommand command)
        {
            Beat = beat ?? string.Empty;
            Command = command;
        }

        public override string ToString() => $"<<at {Beat} {Command.Name} {string.Join(" ", Command.Args)}>>";
    }

    /// <summary>줄의 실행 목록 한 칸 — 이 자리(글자)에 닿으면 실행한다.</summary>
    public readonly struct ScheduledCommand
    {
        /// <summary>글자 자리. 타자기 끝(end · 자리를 잃은 것)은 LineBeatSchedule.LineEnd.</summary>
        public readonly int Position;
        public readonly StageCommand Command;

        /// <summary>대본에 쓴 꼴 — 로그용(&lt;&lt;at a …&gt;&gt; · [cmd="…"/]).</summary>
        public readonly string Authored;

        public ScheduledCommand(int position, in StageCommand command, string authored)
        {
            Position = position;
            Command = command;
            Authored = authored;
        }
    }

    /// <summary>
    /// 대사 줄 하나의 실행 목록을 짓는다 — 표지와 &lt;&lt;at&gt;&gt;를 이어 실행 순서로 늘어놓는다(docs/PLAN_line-beat.md §4).
    /// **재생(InlineCommandTrack)과 폴드(YarnCommandTextExtractor)가 같이 부른다** — 둘의 순서가 지어진 대로 같다.
    ///
    /// 순서(R2): 글자 자리 → 같은 자리면 글의 표지 순서 → 같은 표지면 &lt;&lt;at&gt;&gt;를 쓴 순서. 맨 끝에 end.
    /// 줄 머리 명령(at 없는 것)은 여기에 들지 않는다 — 그 앞에 따로 실행된다.
    ///
    /// 실패를 로그로 흘리지 않는다 — 문제는 problems에 적고, 명령은 사라지지 않게 둔다:
    ///   표지가 없는 at(R5 · 번역문이 빠뜨림)   줄 끝(end 자리)에서
    ///   at이 없는 표지(R6)                     무시
    ///   같은 표지 둘(R6)                       앞의 것
    ///   꼴이 틀린 표지 이름(R7)                무시 — 그 이름의 at은 줄 끝에서
    /// </summary>
    public static class LineBeatSchedule
    {
        /// <summary>글 안 자리 표지의 이름 — [beat=a/].</summary>
        public const string MarkerName = "beat";

        /// <summary>&lt;&lt;at&gt;&gt; 명령의 이름 — 대본 명령이 아니라 다음 줄로 넘기는 표시다.</summary>
        public const string AtCommandName = "at";

        /// <summary>예약된 자리 — 타자기 끝. 글에 표지를 두지 않는다.</summary>
        public const string EndBeat = "end";

        /// <summary>타자기 끝의 자리. 글자 자리 어느 것보다 뒤다.</summary>
        public const int LineEnd = int.MaxValue;

        /// <summary>표지 이름의 꼴(R7) — 영문 소문자 · 숫자, 하나 이상. end는 예약이라 글에 둘 수 없다.</summary>
        public static bool IsValidBeatName(string name)
        {
            if (string.IsNullOrEmpty(name) || name == EndBeat)
                return false;

            foreach (char c in name)
            {
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// &lt;&lt;at 표지 명령 …&gt;&gt;의 토큰(이름 뒤의 것)을 읽는다 — 첫 토큰이 표지, 그 뒤가 명령 한 줄.
        /// 명령이 없으면 false와 까닭.
        /// </summary>
        public static bool TryReadAt(IReadOnlyList<string> args, string source, out AtCommand at, out string error)
        {
            at = default;
            error = null;

            if (args == null || args.Count < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
            {
                error = "<<at 표지 명령 …>>의 꼴이 아니다 — 표지와 명령이 있어야 한다";
                return false;
            }

            string[] commandArgs = new string[args.Count - 2];

            for (int i = 2; i < args.Count; i++)
                commandArgs[i - 2] = args[i];

            at = new AtCommand(args[0], new StageCommand(args[1], commandArgs, source));
            return true;
        }

        /// <summary>
        /// markers는 글 순서, ats는 쓴 순서. 결과는 실행 순서다.
        /// </summary>
        public static List<ScheduledCommand> Build(
            IReadOnlyList<LineMarker> markers,
            IReadOnlyList<AtCommand> ats,
            List<string> problems = null)
        {
            List<Keyed> keyed = new();

            // 표지 이름 → (글자 자리, 글의 표지 순서). 같은 표지가 둘이면 앞의 것.
            Dictionary<string, (int Position, int TextOrder)> beats = new(StringComparer.Ordinal);
            HashSet<string> used = new(StringComparer.Ordinal);

            for (int k = 0; k < (markers?.Count ?? 0); k++)
            {
                LineMarker marker = markers[k];

                if (marker.Beat == null)
                {
                    keyed.Add(new Keyed(marker.Position, k, 0, new ScheduledCommand(
                        marker.Position, marker.Command, $"[cmd=\"{Describe(marker.Command)}\"/]")));
                    continue;
                }

                if (!IsValidBeatName(marker.Beat))
                {
                    problems?.Add(marker.Beat == EndBeat
                        ? "[beat=end/] — end는 타자기 끝의 예약 이름이라 글에 두지 않는다. 무시한다"
                        : $"[beat={marker.Beat}/] — 표지 이름은 영문 소문자 · 숫자다. 무시한다");
                    continue;
                }

                if (beats.ContainsKey(marker.Beat))
                {
                    problems?.Add($"[beat={marker.Beat}/]가 한 줄에 둘이다 — 앞의 것을 쓴다");
                    continue;
                }

                beats.Add(marker.Beat, (marker.Position, k));
            }

            for (int w = 0; w < (ats?.Count ?? 0); w++)
            {
                AtCommand at = ats[w];
                string authored = at.ToString();

                if (at.Beat == EndBeat)
                {
                    keyed.Add(new Keyed(LineEnd, int.MaxValue, w, new ScheduledCommand(LineEnd, at.Command, authored)));
                    continue;
                }

                if (beats.TryGetValue(at.Beat, out var beat))
                {
                    used.Add(at.Beat);
                    keyed.Add(new Keyed(beat.Position, beat.TextOrder, w, new ScheduledCommand(beat.Position, at.Command, authored)));
                    continue;
                }

                problems?.Add($"{authored} — 줄에 [beat={at.Beat}/]가 없다. 줄 끝에서 실행한다");
                keyed.Add(new Keyed(LineEnd, int.MaxValue, w, new ScheduledCommand(LineEnd, at.Command, authored)));
            }

            foreach (string name in beats.Keys)
            {
                if (!used.Contains(name))
                    problems?.Add($"[beat={name}/]에 <<at {name} …>>가 없다 — 무시한다");
            }

            // 열쇠가 모두 달라 순서가 하나로 정해진다(옛 마커는 글 순서, at은 쓴 순서가 가른다).
            keyed.Sort((a, b) =>
            {
                int c = a.Position.CompareTo(b.Position);

                if (c != 0)
                    return c;

                c = a.TextOrder.CompareTo(b.TextOrder);
                return c != 0 ? c : a.Written.CompareTo(b.Written);
            });

            List<ScheduledCommand> schedule = new(keyed.Count);

            foreach (Keyed item in keyed)
                schedule.Add(item.Command);

            return schedule;
        }

        private static string Describe(in StageCommand command)
            => command.Args.Count == 0 ? command.Name : $"{command.Name} {string.Join(" ", command.Args)}";

        private readonly struct Keyed
        {
            public readonly int Position;
            public readonly int TextOrder;
            public readonly int Written;
            public readonly ScheduledCommand Command;

            public Keyed(int position, int textOrder, int written, in ScheduledCommand command)
            {
                Position = position;
                TextOrder = textOrder;
                Written = written;
                Command = command;
            }
        }
    }
}
