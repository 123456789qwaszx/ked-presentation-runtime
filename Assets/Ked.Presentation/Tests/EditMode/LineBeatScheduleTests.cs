using System.Collections.Generic;
using System.Linq;
using Ked.Presentation.Core;
using NUnit.Framework;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 대사 줄의 실행 목록(docs/PLAN_line-beat.md R2 · R5 · R6 · R7) — 재생과 폴드가 같이 부르는 함수다.
    /// 명령의 둘째 인자를 이름표로 써서 순서를 본다.
    /// </summary>
    public sealed class LineBeatScheduleTests
    {
        private static StageCommand Cmd(string tag) => new("face", new[] { "s1", tag });

        private static AtCommand At(string beat, string tag) => new(beat, Cmd(tag));

        private static string[] Order(List<ScheduledCommand> schedule) => schedule.Select(c => c.Command.Args[1]).ToArray();

        [Test]
        public void 글자_자리_다음_표지_순서_다음_쓴_순서이고_end가_마지막이다()
        {
            // 글: "가[beat=b/][beat=a/]나[beat=c/]다" — b와 a는 같은 자리(1), c는 2.
            LineMarker[] markers =
            {
                LineMarker.ForBeat("b", 1),
                LineMarker.ForBeat("a", 1),
                LineMarker.ForBeat("c", 2),
            };

            AtCommand[] ats =
            {
                At("end", "e1"),
                At("c", "c1"),
                At("a", "a1"),
                At("b", "b1"),
                At("a", "a2"),
                At("end", "e2"),
            };

            List<string> problems = new();
            List<ScheduledCommand> schedule = LineBeatSchedule.Build(markers, ats, problems);

            Assert.That(problems, Is.Empty);
            Assert.That(Order(schedule), Is.EqualTo(new[] { "b1", "a1", "a2", "c1", "e1", "e2" }));
            Assert.That(schedule.Select(c => c.Position), Is.EqualTo(new[] { 1, 1, 1, 2, LineBeatSchedule.LineEnd, LineBeatSchedule.LineEnd }));
            Assert.That(schedule[1].Authored, Is.EqualTo("<<at a face s1 a1>>"));
        }

        [Test]
        public void 옛_cmd_마커는_글_순서로_표지_사이에_선다()
        {
            LineMarker[] markers =
            {
                LineMarker.ForCommand(Cmd("x"), 0),
                LineMarker.ForBeat("a", 0),
                LineMarker.ForCommand(Cmd("y"), 0),
                LineMarker.ForCommand(Cmd("z"), 5),
            };

            List<ScheduledCommand> schedule = LineBeatSchedule.Build(markers, new[] { At("a", "a1") });

            Assert.That(Order(schedule), Is.EqualTo(new[] { "x", "a1", "y", "z" }));
            Assert.That(schedule[0].Authored, Is.EqualTo("[cmd=\"face s1 x\"/]"));
        }

        [Test]
        public void 표지가_없는_at은_줄_끝에서_쓴_순서로_실행하고_적는다()
        {
            // 번역문이 표지를 빠뜨린 줄(R5) — 연출이 사라지지 않는다.
            List<string> problems = new();
            List<ScheduledCommand> schedule = LineBeatSchedule.Build(
                new[] { LineMarker.ForBeat("a", 3) },
                new[] { At("end", "e1"), At("b", "b1"), At("a", "a1") },
                problems);

            Assert.That(Order(schedule), Is.EqualTo(new[] { "a1", "e1", "b1" }));
            Assert.That(schedule[2].Position, Is.EqualTo(LineBeatSchedule.LineEnd));
            Assert.That(problems.Single(), Does.Contain("[beat=b/]가 없다"));
        }

        [Test]
        public void at이_없는_표지는_무시하고_적는다()
        {
            List<string> problems = new();
            List<ScheduledCommand> schedule = LineBeatSchedule.Build(
                new[] { LineMarker.ForBeat("a", 0), LineMarker.ForBeat("b", 2) },
                new[] { At("a", "a1") },
                problems);

            Assert.That(Order(schedule), Is.EqualTo(new[] { "a1" }));
            Assert.That(problems.Single(), Does.Contain("[beat=b/]에 <<at b …>>가 없다"));
        }

        [Test]
        public void 같은_표지가_둘이면_앞의_것을_쓰고_적는다()
        {
            List<string> problems = new();
            List<ScheduledCommand> schedule = LineBeatSchedule.Build(
                new[] { LineMarker.ForBeat("a", 1), LineMarker.ForBeat("a", 4) },
                new[] { At("a", "a1") },
                problems);

            Assert.That(schedule.Single().Position, Is.EqualTo(1));
            Assert.That(problems.Single(), Does.Contain("둘이다"));
        }

        [TestCase("end")]
        [TestCase("A")]
        [TestCase("a-b")]
        [TestCase("")]
        public void 꼴이_틀린_표지는_무시하고_그_이름의_at은_줄_끝이다(string name)
        {
            List<string> problems = new();
            List<ScheduledCommand> schedule = LineBeatSchedule.Build(
                new[] { LineMarker.ForBeat(name, 1) },
                new[] { At(name, "x") },
                problems);

            Assert.That(schedule.Single().Position, Is.EqualTo(LineBeatSchedule.LineEnd));

            // end는 예약이라 at end는 본디 줄 끝이다 — 표지만 문제다.
            Assert.That(problems.Count, Is.EqualTo(name == "end" ? 1 : 2), string.Join("\n", problems));
        }

        [TestCase("a", true)]
        [TestCase("b2", true)]
        [TestCase("12", true)]
        [TestCase("end", false)]
        [TestCase("Ab", false)]
        [TestCase("a_b", false)]
        [TestCase(null, false)]
        public void 표지_이름의_꼴(string name, bool valid)
        {
            Assert.That(LineBeatSchedule.IsValidBeatName(name), Is.EqualTo(valid));
        }

        [Test]
        public void at의_토큰을_표지와_명령으로_읽는다()
        {
            Assert.That(LineBeatSchedule.TryReadAt(new[] { "a", "face_swap", "s2", "03", "0.3s" }, "f:3", out AtCommand at, out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(at.Beat, Is.EqualTo("a"));
            Assert.That(at.Command.Name, Is.EqualTo("face_swap"));
            Assert.That(at.Command.Args, Is.EqualTo(new[] { "s2", "03", "0.3s" }));
            Assert.That(at.Command.Source, Is.EqualTo("f:3"));

            Assert.That(LineBeatSchedule.TryReadAt(new[] { "a" }, "f:4", out _, out error), Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void 빈_줄은_빈_목록이다()
        {
            Assert.That(LineBeatSchedule.Build(null, null), Is.Empty);
            Assert.That(LineBeatSchedule.Build(new LineMarker[0], new AtCommand[0]), Is.Empty);
        }
    }
}
