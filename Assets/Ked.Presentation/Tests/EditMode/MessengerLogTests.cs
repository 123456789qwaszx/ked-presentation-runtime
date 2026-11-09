using NUnit.Framework;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 메신저 기록 — 구간이 열린 뒤 지나온 줄의 목록(docs/PLAN_messenger.md §3).
    /// 되감기 · 로드는 장면 경계의 Reset 뒤에 같은 커맨드와 줄을 다시 지나가므로, 여기서는 "같은 입력이면 같은 기록"만 본다.
    /// </summary>
    public sealed class MessengerLogTests
    {
        [Test]
        public void 열기_전의_줄은_적히지_않는다()
        {
            MessengerLog log = new();

            log.RecordLine("닉스", "안녕");

            Assert.That(log.IsOpen, Is.False);
            Assert.That(log.Entries, Is.Empty);
        }

        [Test]
        public void 열린_동안의_줄이_순서대로_적힌다()
        {
            MessengerLog log = new();

            log.Open("코하쿠");
            log.RecordLine("닉스", "지금 어디야?");
            log.RecordLine("코하쿠", "가는 중!");

            Assert.That(log.IsOpen, Is.True);
            Assert.That(log.Entries.Count, Is.EqualTo(2));
            Assert.That(log.Entries[0].Speaker, Is.EqualTo("닉스"));
            Assert.That(log.Entries[0].Text, Is.EqualTo("지금 어디야?"));
            Assert.That(log.Entries[1].Text, Is.EqualTo("가는 중!"));
        }

        [Test]
        public void 나는_오른쪽_나머지는_왼쪽()
        {
            MessengerLog log = new();

            log.Open("코하쿠");
            log.RecordLine("닉스", "a");
            log.RecordLine("코하쿠", "b");
            log.RecordLine("카스미", "c");

            Assert.That(log.Entries[0].Side, Is.EqualTo(MessengerSide.Left));
            Assert.That(log.Entries[1].Side, Is.EqualTo(MessengerSide.Right));
            Assert.That(log.Entries[2].Side, Is.EqualTo(MessengerSide.Left));
        }

        [Test]
        public void 화자_없는_줄은_왼쪽이고_화자는_빈_문자열()
        {
            MessengerLog log = new();

            log.Open("코하쿠");
            log.RecordLine(null, "시스템 글");

            Assert.That(log.Entries[0].Side, Is.EqualTo(MessengerSide.Left));
            Assert.That(log.Entries[0].Speaker, Is.EqualTo(string.Empty));
        }

        [Test]
        public void 나를_비워_열면_화자_없는_줄도_왼쪽이다()
        {
            MessengerLog log = new();

            log.Open(" ");
            log.RecordLine(null, "a");
            log.RecordLine("닉스", "b");

            Assert.That(log.Entries[0].Side, Is.EqualTo(MessengerSide.Left));
            Assert.That(log.Entries[1].Side, Is.EqualTo(MessengerSide.Left));
        }

        [Test]
        public void 이름의_앞뒤_공백은_무시한다()
        {
            MessengerLog log = new();

            log.Open(" 코하쿠 ");
            log.RecordLine("코하쿠", "a");

            Assert.That(log.Entries[0].Side, Is.EqualTo(MessengerSide.Right));
        }

        [Test]
        public void 닫으면_비고_뒤의_줄은_적히지_않는다()
        {
            MessengerLog log = new();

            log.Open("코하쿠");
            log.RecordLine("닉스", "a");
            log.Close();
            log.RecordLine("닉스", "b");

            Assert.That(log.IsOpen, Is.False);
            Assert.That(log.Entries, Is.Empty);
        }

        [Test]
        public void 열린_채_다시_열면_새_대화다()
        {
            MessengerLog log = new();

            log.Open("코하쿠");
            log.RecordLine("코하쿠", "a");
            log.Open("닉스");
            log.RecordLine("코하쿠", "b");

            Assert.That(log.Entries.Count, Is.EqualTo(1));
            Assert.That(log.Entries[0].Text, Is.EqualTo("b"));
            Assert.That(log.Entries[0].Side, Is.EqualTo(MessengerSide.Left));
        }

        [Test]
        public void 리셋_뒤에_같은_입력을_다시_지나가면_같은_기록이다()
        {
            MessengerLog log = new();

            Replay(log);
            string first = Dump(log);

            // 되감기 · 로드: 장면 경계가 비우고, 루트부터 같은 커맨드와 줄이 다시 지나간다.
            log.Reset();
            Assert.That(log.IsOpen, Is.False);
            Assert.That(log.Entries, Is.Empty);

            Replay(log);

            Assert.That(Dump(log), Is.EqualTo(first));
        }

        private static void Replay(MessengerLog log)
        {
            log.RecordLine("하린", "구간 앞의 줄");
            log.Open("코하쿠");
            log.RecordLine("닉스", "a");
            log.RecordLine("코하쿠", "b");
        }

        private static string Dump(MessengerLog log)
        {
            System.Text.StringBuilder sb = new();

            foreach (MessengerEntry entry in log.Entries)
                sb.Append(entry.Speaker).Append('|').Append(entry.Text).Append('|').Append(entry.Side).Append('\n');

            return sb.ToString();
        }
    }
}
