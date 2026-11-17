using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// 화면 덮개(screen_cover · screen_cover_clear) — docs/PLAN_screen-cover.md §3.1 · §3.3.
// 리그의 자리(화면 효과의 맨 아래) · 컨트롤러의 상태 · 커맨드의 전경과 압축 · 문법의 생략값을 고정한다.
public class ScreenCoverTests
{
    private sealed class Pace : ICommandPacePolicy
    {
        public bool Compress;
        public bool CompressExecution => Compress;
    }

    private readonly List<Object> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in _spawned)
        {
            if (o != null)
                Object.DestroyImmediate(o is Component c ? c.gameObject : o);
        }

        _spawned.Clear();
    }

    private ScreenEffectRig NewRig()
    {
        RectTransform root = new ScreenEffectRigBuilder().BuildRigRoot();
        ScreenEffectRig rig = root.gameObject.AddComponent<ScreenEffectRig>();
        rig.Initialize();
        _spawned.Add(rig);
        return rig;
    }

    private ScreenCoverPresetDBSO NewPresets()
    {
        ScreenCoverPresetDBSO db = ScreenCoverPresetDBSO.CreateDefault();
        _spawned.Add(db);
        return db;
    }

    // 덮기는 전경이다 — 파서가 wait를 세운다(문법 시험이 본다).
    private static ScreenCoverCommandSpec Cover(string preset, float duration)
        => new() { wait = true, presetKey = preset, duration = duration, ease = Ease.Linear };

    private static ScreenCoverCommandSpec Clear(float duration)
        => new() { clear = true, duration = duration, ease = Ease.Linear };

    // ── 리그 ─────────────────────────────────────────────────────

    [Test]
    public void 덮개는_화면_효과의_맨_아래에_선다()
    {
        ScreenEffectRig rig = NewRig();
        RectTransform overlay = rig.Refs.ScreenOverlay_Root;

        Assert.AreEqual("Cover_Image", overlay.GetChild(0).name, "덮개가 맨 아래 — 비네트 · 노이즈 · 플래시가 그 위에 얹힌다");
        Assert.AreEqual("Vignette_Image", overlay.GetChild(1).name);
        Assert.AreEqual("Flash_Image", overlay.GetChild(overlay.childCount - 1).name);
        Assert.IsNotNull(rig.Cover, "덮개 컨트롤러가 묶인다");
        Assert.AreSame(rig.Refs.Cover_Image, rig.Cover.GetComponent<Image>());
    }

    [Test]
    public void 처음에는_걷혀_있고_클릭을_받지_않는다()
    {
        ScreenEffectRig rig = NewRig();
        Image image = rig.Refs.Cover_Image;

        Assert.AreEqual(0f, rig.Cover.Amount);
        Assert.IsFalse(image.enabled, "걷힌 덮개는 그리지 않는다");
        Assert.IsFalse(image.raycastTarget, "덮개는 클릭을 받지 않는다 — HUD 입력은 위 캔버스다");
    }

    // ── 컨트롤러 ─────────────────────────────────────────────────

    [Test]
    public void 덮으면_그림이_그_색으로_켜지고_0으로_되돌리면_걷힌다()
    {
        ScreenEffectRig rig = NewRig();
        Image image = rig.Refs.Cover_Image;

        rig.Cover.Snap(new ScreenCoverEffectController.State(Color.white, 1f));

        Assert.IsTrue(image.enabled);
        Assert.AreEqual(new Color(1f, 1f, 1f, 1f), image.color);

        rig.ResetToBaselineImmediate();

        Assert.AreEqual(0f, rig.Cover.Amount, "정지 · 장면 경계에서 걷힌다");
        Assert.IsFalse(image.enabled);
        Assert.AreEqual(Color.white, rig.Cover.Color, "색은 남긴다 — 걷기는 덮인 정도만 0으로");
    }

    // ── 커맨드 ───────────────────────────────────────────────────

    [Test]
    public void 덮기는_전경이고_걷기는_전경이_아니다()
    {
        ScreenEffectRig rig = NewRig();
        ScreenCoverPresetDBSO db = NewPresets();

        Assert.IsTrue(new ScreenCoverCommand(Cover("black", 0.5f), rig, db).WaitForCompletion,
            "덮기는 다 덮일 때까지 기다린다 — 뒤의 무대 정리가 덮인 밑에서");
        Assert.IsFalse(new ScreenCoverCommand(Clear(0.5f), rig, db).WaitForCompletion,
            "걷기는 기다리지 않는다 — 다음 줄이 열려도 제 길이로");

        ScreenCoverCommandSpec released = Cover("black", 0.5f);
        released.wait = false;
        Assert.IsFalse(new ScreenCoverCommand(released, rig, db).WaitForCompletion,
            "대사 줄 머리가 기다림을 내린 덮기 — 덮기만 하고 기다리지 않는다(docs/PLAN_line-beat.md Q1)");
    }

    [Test]
    public void 압축_중의_덮기와_걷기는_끝값을_바로_세운다()
    {
        ScreenEffectRig rig = NewRig();
        ScreenCoverPresetDBSO db = NewPresets();
        CommandRunScope scope = new(new Pace { Compress = true });

        Drain(new ScreenCoverCommand(Cover("white", 0.5f), rig, db).Execute(scope));

        Assert.AreEqual(1f, rig.Cover.Amount, "빨리 넘기기 · 시크로 지나가도 덮인 채 — 플래시처럼 끊지 않는다");
        Assert.AreEqual(Color.white, rig.Cover.Color);
        Assert.IsFalse(rig.Cover.IsTransitioning);

        Drain(new ScreenCoverCommand(Clear(0.5f), rig, db).Execute(scope));

        Assert.AreEqual(0f, rig.Cover.Amount);
        Assert.IsFalse(rig.Refs.Cover_Image.enabled);
    }

    [Test]
    public void 덮는_도중에_압축이_켜지면_다_덮고_기다림을_놓는다()
    {
        ScreenEffectRig rig = NewRig();
        ScreenCoverPresetDBSO db = NewPresets();
        Pace pace = new();
        CommandRunScope scope = new(pace);

        Nested run = new(new ScreenCoverCommand(Cover("black", 2f), rig, db).Execute(scope));

        Assert.IsTrue(run.Tick(), "덮는 동안 기다린다");
        Assert.IsTrue(rig.Cover.IsTransitioning);

        pace.Compress = true;   // 빨리 넘기기를 눌렀다

        Assert.IsFalse(run.Tick(), "다음 프레임에 놓는다");

        Assert.AreEqual(1f, rig.Cover.Amount, "그 자리에서 다 덮는다");
        Assert.IsFalse(rig.Cover.IsTransitioning);
    }

    [Test]
    public void 걷혀_있던_덮개는_새_색에서_덮기_시작한다()
    {
        ScreenEffectRig rig = NewRig();
        ScreenCoverPresetDBSO db = NewPresets();
        CommandRunScope scope = new(new Pace());

        rig.Cover.Snap(new ScreenCoverEffectController.State(Color.white, 0f));   // 지난번에 흰 덮개를 걷었다

        new Nested(new ScreenCoverCommand(Cover("black", 1f), rig, db).Execute(scope)).Tick();

        Assert.AreEqual(Color.black, rig.Cover.Color, "흰색에서 검정으로 옮겨 가며 덮이지 않는다");
        Assert.AreEqual(0f, rig.Cover.Amount, 1e-4f);

        rig.Cover.KillTween(false);
    }

    [Test]
    public void 걷기는_색을_바꾸지_않고_덮인_정도만_0으로()
    {
        ScreenEffectRig rig = NewRig();
        ScreenCoverPresetDBSO db = NewPresets();
        CommandRunScope scope = new(new Pace { Compress = true });

        Drain(new ScreenCoverCommand(Cover("white", 0.5f), rig, db).Execute(scope));
        Drain(new ScreenCoverCommand(Clear(0.5f), rig, db).Execute(scope));

        Assert.AreEqual(Color.white, rig.Cover.Color);
        Assert.AreEqual(0f, rig.Cover.Amount);
    }

    // ── 프리셋 · 문법 ─────────────────────────────────────────────

    [Test]
    public void 코드_기본_프리셋은_검정과_흰색이다()
    {
        ScreenCoverPresetDBSO db = NewPresets();

        Assert.IsTrue(db.TryGet("black", out ScreenCoverPresetDBSO.Entry black));
        Assert.AreEqual(Color.black, black.color);
        Assert.IsTrue(db.TryGet("White", out ScreenCoverPresetDBSO.Entry white), "이름은 대소문자를 가리지 않는다");
        Assert.AreEqual(Color.white, white.color);
        Assert.AreEqual("black", ScreenCoverPresetDBSO.NormalizeKey(""), "비우면 검정");
    }

    [Test]
    public void 문법_생략값_덮기는_검정_0점5초_Linear()
    {
        List<CommandSpecBase> output = new();

        Assert.IsTrue(new StageCommandParser(null).TryParse(new StageCommand("screen_cover", new string[0], "test.yarn:1"), output));

        ScreenCoverCommandSpec spec = (ScreenCoverCommandSpec)output[0];
        Assert.IsFalse(spec.clear);
        Assert.IsTrue(spec.wait, "덮기는 다 덮일 때까지 기다린다");
        Assert.AreEqual("black", spec.presetKey);
        Assert.AreEqual(0.5f, spec.duration, 1e-4f);
        Assert.AreEqual(Ease.Linear, spec.ease);
    }

    [Test]
    public void 문법_덮기와_걷기의_인자()
    {
        List<CommandSpecBase> output = new();
        StageCommandParser parser = new(null);

        Assert.IsTrue(parser.TryParse(new StageCommand("screen_cover", new[] { "white", "0.3s", "OutSine" }, "test.yarn:1"), output));
        Assert.IsTrue(parser.TryParse(new StageCommand("screen_cover_clear", new[] { "0.8s" }, "test.yarn:2"), output));

        ScreenCoverCommandSpec cover = (ScreenCoverCommandSpec)output[0];
        Assert.AreEqual("white", cover.presetKey);
        Assert.AreEqual(0.3f, cover.duration, 1e-4f);
        Assert.AreEqual(Ease.OutSine, cover.ease);

        ScreenCoverCommandSpec clear = (ScreenCoverCommandSpec)output[1];
        Assert.IsTrue(clear.clear);
        Assert.IsFalse(clear.wait, "걷기는 기다리지 않는다");
        Assert.AreEqual(0.8f, clear.duration, 1e-4f);
        Assert.AreEqual(Ease.Linear, clear.ease);
    }

    // 커맨드는 안쪽 코루틴을 객체로 yield한다(CommandBase.Execute) — Unity처럼 안쪽부터 돌린다.
    private sealed class Nested
    {
        private readonly Stack<IEnumerator> _stack = new();

        public Nested(IEnumerator root) => _stack.Push(root);

        // 한 프레임. 기다리는 중이면 true, 끝났으면 false.
        public bool Tick()
        {
            while (_stack.Count > 0)
            {
                IEnumerator top = _stack.Peek();

                if (!top.MoveNext())
                {
                    _stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator inner)
                {
                    _stack.Push(inner);
                    continue;
                }

                return true;
            }

            return false;
        }
    }

    private static void Drain(IEnumerator run)
    {
        Nested nested = new(run);
        int guard = 0;

        while (nested.Tick())
        {
            if (++guard > 10000)
                Assert.Fail("커맨드가 끝나지 않는다");
        }
    }
}
