using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;

/// <summary>
/// 명세 표(코어 StageCommandSpecTable) ↔ 무대 파서의 표(StageCommandParser.Table.cs) — 이름 · 인자 수 · 생략값이
/// 어긋나면 여기서 실패한다(ked-story-tools docs/PLAN.md §4.1 · 소유자 D4).
///
/// 파서의 표는 이름마다 람다라 인자 자리와 생략값이 데이터로 없다. 파서는 고치지 않기로 했으므로
/// **표의 원문을 읽어** 항목마다 c.Arg(자리[, 생략값]) · Float(c, 자리, 생략값)을 모은다.
/// 자리가 숫자가 아닌 항목(size — 셋째 칸이 keep_y인가에 따라 자리가 밀린다)은 손으로 맞춘다:
/// 그 항목의 원문을 여기에 그대로 적어 두고, 원문이 바뀌면 실패해 다시 맞추게 한다.
/// </summary>
public sealed class StageCommandSpecTableParserTests
{
    // ── 손으로 맞춘 항목 — 원문(공백을 뺀 것)이 이것과 다르면 실패한다 ──────────────
    private static readonly Dictionary<string, string> HandChecked = new()
    {
        ["size"] =
            "(p,c)=>{boolkeepY=SizeToken.IsKeepY(c.Arg(2));inttail=keepY?3:2;" +
            "p.EnqueueDepthAtPresetSpec(c.Arg(0),c.Arg(1),keepY,c.Arg(tail,DefaultDepthDurationToken),c.Arg(tail+1,\"\"));},",
    };

    // ── 생략값이 글자가 아니라 상수인 것 — 이름 → 값 ────────────────────────────────
    private static readonly Dictionary<string, string> Constants = new()
    {
        ["MotionModeToken.Relative"] = MotionModeToken.Relative,
        ["ScreenCoverPresetDBSO.DefaultPresetKey"] = ScreenCoverPresetDBSO.DefaultPresetKey,
        ["ScreenNoisePresetDBSO.DefaultPresetKey"] = ScreenNoisePresetDBSO.DefaultPresetKey,
        ["DefaultDepthDurationToken"] = PrivateConst("DefaultDepthDurationToken"),
    };

    private static StageCommandSpecTable Specs => StageCommandSpecTable.Default;

    [Test]
    public void 이름_집합이_같다()
    {
        string[] parser = StageCommandParser.CommandNames.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        string[] queued = Specs.All.Where(s => !s.Immediate).Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.That(parser.Except(queued), Is.Empty,
            "파서 표에는 있고 명세 표에는 없다 — StageCommandSpecTable.Core.cs(포크는 .Fork.cs)에 종류와 인자 칸을 적는다");
        Assert.That(queued.Except(parser), Is.Empty,
            "명세 표에는 있고 파서 표에는 없다 — 걷은 커맨드면 명세 표에서도 지운다");
    }

    [Test]
    public void 바로_실행하는_명령은_파서_표에_없다()
    {
        // 브리지가 쌓지 않고 바로 실행한다 — <<at 이름 …>>으로 적으면 파서로 넘어가 모르는 명령이 된다.
        foreach (StageCommandSpec spec in Specs.All.Where(s => s.Immediate))
            Assert.That(StageCommandParser.Knows(spec.Name), Is.False, spec.Name);
    }

    [Test]
    public void 원문에서_읽은_이름이_파서_표와_같다()
    {
        // 원문을 읽는 손이 항목을 빠뜨리지 않는가 — 아래 대조의 전제다.
        Assert.That(ReadTable().Keys.OrderBy(n => n, StringComparer.Ordinal),
            Is.EqualTo(StageCommandParser.CommandNames.OrderBy(n => n, StringComparer.Ordinal)));
    }

    [Test]
    public void 인자_수와_생략값이_같다()
    {
        List<string> mismatches = new();

        foreach (KeyValuePair<string, TableEntry> pair in ReadTable())
        {
            string name = pair.Key;
            TableEntry entry = pair.Value;

            if (!Specs.TryGet(name, out StageCommandSpec spec))
                continue; // 이름_집합이_같다가 잡는다

            if (entry.HandChecked)
                continue; // 손으로_맞춘_항목이 본다

            if (spec.Args.Any(a => a.Flag))
            {
                mismatches.Add($"<<{name}>> — 낱말 칸(Flag)은 자리가 밀리는 항목에만 쓴다(손으로 맞춘 항목)");
                continue;
            }

            if (spec.Args.Count != entry.Count)
            {
                mismatches.Add($"<<{name}>> 인자 수 — 파서 표 {entry.Count} · 명세 표 {spec.Args.Count}");
                continue;
            }

            for (int i = 0; i < entry.Count; i++)
            {
                if (!entry.Slots.TryGetValue(i, out TableDefault tableDefault))
                {
                    mismatches.Add($"<<{name}>> — 파서 표가 인자 {i}를 읽지 않는다");
                    continue;
                }

                string specDefault = spec.Args[i].Default;

                if (!tableDefault.Has)
                {
                    if (specDefault != null)
                        mismatches.Add($"<<{name}>> {spec.Args[i].Name} — 파서 표에는 생략값이 없는데 명세 표는 '{specDefault}'다");

                    continue;
                }

                if (specDefault == null)
                {
                    mismatches.Add($"<<{name}>> {spec.Args[i].Name} — 파서 표의 생략값은 '{tableDefault.Text}'인데 명세 표에는 없다");
                    continue;
                }

                if (!SameDefault(tableDefault, specDefault))
                    mismatches.Add($"<<{name}>> {spec.Args[i].Name} — 생략값이 파서 표 '{tableDefault.Text}' · 명세 표 '{specDefault}'");
            }
        }

        Assert.That(mismatches, Is.Empty, "\n" + string.Join("\n", mismatches));
    }

    [Test]
    public void 손으로_맞춘_항목()
    {
        Dictionary<string, TableEntry> table = ReadTable();

        foreach (string name in HandChecked.Keys)
            Assert.That(table.ContainsKey(name) && table[name].HandChecked, Is.True, $"<<{name}>> — 이제 자리가 숫자다. HandChecked에서 뺀다");

        // size 슬롯 <레벨·라벨·x배율> [keep_y] [길이=13fr] [이징=""] — 원문이 그대로일 때만 아래가 참이다(ReadTable이 본다).
        Assert.That(Specs.TryGet("size", out StageCommandSpec size), Is.True);
        Assert.That(size.Args.Select(a => a.Name), Is.EqualTo(new[] { "slot", "level", "keep_y", "duration", "ease" }));
        Assert.That(size.Args.Select(a => a.Default), Is.EqualTo(new[] { null, null, null, Constants["DefaultDepthDurationToken"], "" }));
        Assert.That(size.Args.Select(a => a.Flag), Is.EqualTo(new[] { false, false, true, false, false }));
        Assert.That(size.Args[2].Choices, Is.EqualTo(new[] { SizeToken.KeepY }));
    }

    [Test]
    public void pause를_적지_않았을_때의_길이가_줄_읽기와_같다()
    {
        // 파서 표에는 pause의 생략값이 없다 — 핸들러가 제 값으로 간다. 줄 읽기(지연의 셈)가 같은 값을 써야 한다.
        List<CommandSpecBase> output = new();
        Assert.That(new StageCommandParser(null).TryParse(new StageCommand("pause", Array.Empty<string>()), output), Is.True);

        Assert.That(output, Has.Count.EqualTo(1));
        Assert.That(((WaitCommandSpec)output[0]).duration, Is.EqualTo(YarnLineReader.DefaultPauseSeconds).Within(1e-6f));
    }

    // ── 표의 원문을 읽는다 ──────────────────────────────────────────────────────────

    private readonly struct TableDefault
    {
        public readonly bool Has;
        public readonly string Text;
        public readonly bool Numeric;

        public TableDefault(bool has, string text, bool numeric)
        {
            Has = has;
            Text = text;
            Numeric = numeric;
        }
    }

    private sealed class TableEntry
    {
        public readonly Dictionary<int, TableDefault> Slots = new();
        public bool HandChecked;
        public int Count => Slots.Count == 0 ? 0 : Slots.Keys.Max() + 1;
    }

    private static bool SameDefault(TableDefault table, string spec)
    {
        if (!table.Numeric)
            return table.Text == spec;

        return float.TryParse(spec, NumberStyles.Float, CultureInfo.InvariantCulture, out float specValue) &&
               float.TryParse(table.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float tableValue) &&
               specValue == tableValue;
    }

    private static string PrivateConst(string name)
    {
        FieldInfo field = typeof(StageCommandParser).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, $"StageCommandParser.{name}이 없다 — 표의 원문이 바뀌었다");
        return (string)field.GetRawConstantValue();
    }

    // 이 시험 파일의 자리에서 찾는다(패키지로 들여도 따라온다). 컴파일러가 경로를 바꿔 적었으면 프로젝트 루트에서 찾는다.
    private static string TablePath([CallerFilePath] string thisFile = "")
    {
        string beside = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(thisFile) ?? "", "..", "..", "Stage", "Grammar", "StageCommandParser.Table.cs"));

        return File.Exists(beside)
            ? beside
            : Path.GetFullPath(Path.Combine("Assets", "Ked.Presentation", "Stage", "Grammar", "StageCommandParser.Table.cs"));
    }

    private static Dictionary<string, TableEntry> ReadTable()
    {
        string path = TablePath();
        Assert.That(File.Exists(path), Is.True, $"파서 표의 원문을 찾지 못했다: {path}");

        string source = StripComments(File.ReadAllText(path));

        int open = source.IndexOf("Table = new", StringComparison.Ordinal);
        Assert.That(open, Is.GreaterThanOrEqualTo(0), "파서 표(Table = new …)를 찾지 못했다");

        int bodyStart = source.IndexOf('{', open);
        int bodyEnd = MatchingBrace(source, bodyStart);
        string body = source.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);

        MatchCollection heads = Regex.Matches(body, @"\[""(?<name>[^""]+)""\]\s*=");
        Dictionary<string, TableEntry> table = new(StringComparer.Ordinal);

        for (int h = 0; h < heads.Count; h++)
        {
            string name = heads[h].Groups["name"].Value;
            int from = heads[h].Index + heads[h].Length;
            int to = h + 1 < heads.Count ? heads[h + 1].Index : body.Length;
            string text = body.Substring(from, to - from);

            Assert.That(table.ContainsKey(name), Is.False, $"파서 표에 <<{name}>>이 둘이다");
            table.Add(name, ReadEntry(name, text));
        }

        return table;
    }

    private static TableEntry ReadEntry(string name, string text)
    {
        TableEntry entry = new();

        if (HandChecked.TryGetValue(name, out string expected))
        {
            Assert.That(Regex.Replace(text, @"\s+", ""), Is.EqualTo(expected),
                $"<<{name}>>의 파서 표 원문이 바뀌었다 — 명세 표를 다시 맞추고 이 시험의 HandChecked를 고친다");
            entry.HandChecked = true;
            return entry;
        }

        foreach (string call in new[] { "c.Arg(", "Float(c," })
        {
            int at = 0;

            while ((at = text.IndexOf(call, at, StringComparison.Ordinal)) >= 0)
            {
                // "c.Arg("가 다른 이름의 꼬리가 아닌가(abc.Arg( 같은 것).
                bool wordStart = at == 0 || !(char.IsLetterOrDigit(text[at - 1]) || text[at - 1] == '_' || text[at - 1] == '.');
                int argsStart = at + call.Length;
                at = argsStart;

                if (!wordStart)
                    continue;

                List<string> args = SplitArguments(text, argsStart);
                bool isFloat = call[0] == 'F';

                Assert.That(args.Count, Is.InRange(1, 2), $"<<{name}>> — {call}…)의 인자를 읽지 못했다");
                Assert.That(!isFloat || args.Count == 2, Is.True, $"<<{name}>> — Float(c, 자리, 생략값)의 꼴이 아니다");

                Assert.That(int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out int index), Is.True,
                    $"<<{name}>> — 인자 자리 '{args[0]}'이 숫자가 아니다. 손으로 맞춘다(이 시험의 HandChecked)");

                TableDefault read = args.Count == 2 ? ReadDefault(name, args[1], isFloat) : new TableDefault(false, null, false);

                if (entry.Slots.TryGetValue(index, out TableDefault before))
                {
                    Assert.That(before.Has == read.Has && before.Text == read.Text, Is.True,
                        $"<<{name}>> — 인자 {index}를 두 번 읽는데 생략값이 다르다");
                    continue;
                }

                entry.Slots.Add(index, read);
            }
        }

        return entry;
    }

    private static TableDefault ReadDefault(string name, string expression, bool numeric)
    {
        if (numeric)
        {
            string number = expression.TrimEnd('f', 'F');
            Assert.That(float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _), Is.True,
                $"<<{name}>> — Float의 생략값 '{expression}'을 읽지 못했다");
            return new TableDefault(true, number, true);
        }

        if (expression.Length >= 2 && expression[0] == '"' && expression[expression.Length - 1] == '"')
            return new TableDefault(true, expression.Substring(1, expression.Length - 2), false);

        Assert.That(Constants.TryGetValue(expression, out string value), Is.True,
            $"<<{name}>> — 생략값 '{expression}'이 글자가 아니다. 이 시험의 Constants에 그 상수를 더한다");
        return new TableDefault(true, value, false);
    }

    // start는 여는 괄호 다음 자리. 맨 바깥의 쉼표로 나눈다 — 따옴표와 안쪽 괄호를 존중한다.
    private static List<string> SplitArguments(string text, int start)
    {
        List<string> args = new();
        StringBuilder current = new();
        int depth = 0;
        bool quoted = false;

        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];

            if (quoted)
            {
                current.Append(c);

                if (c == '\\' && i + 1 < text.Length)
                    current.Append(text[++i]);
                else if (c == '"')
                    quoted = false;

                continue;
            }

            if (c == '"')
            {
                quoted = true;
                current.Append(c);
                continue;
            }

            if (c == '(')
                depth++;

            if (c == ')')
            {
                if (depth == 0)
                {
                    args.Add(current.ToString().Trim());
                    return args;
                }

                depth--;
            }

            if (c == ',' && depth == 0)
            {
                args.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        Assert.Fail("닫는 괄호를 찾지 못했다");
        return args;
    }

    private static int MatchingBrace(string text, int open)
    {
        int depth = 0;
        bool quoted = false;

        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];

            if (quoted)
            {
                if (c == '\\')
                    i++;
                else if (c == '"')
                    quoted = false;

                continue;
            }

            if (c == '"')
                quoted = true;
            else if (c == '{')
                depth++;
            else if (c == '}' && --depth == 0)
                return i;
        }

        Assert.Fail("표의 닫는 중괄호를 찾지 못했다");
        return -1;
    }

    // 줄 주석(// …)을 뺀다 — 따옴표 안의 //는 그대로 둔다. 주석 속의 대본 예시가 항목으로 읽히지 않게.
    private static string StripComments(string source)
    {
        StringBuilder result = new(source.Length);
        bool quoted = false;

        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];

            if (quoted)
            {
                result.Append(c);

                if (c == '\\' && i + 1 < source.Length)
                    result.Append(source[++i]);
                else if (c == '"')
                    quoted = false;

                continue;
            }

            if (c == '"')
            {
                quoted = true;
                result.Append(c);
                continue;
            }

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n')
                    i++;

                result.Append('\n');
                continue;
            }

            result.Append(c);
        }

        return result.ToString();
    }
}
