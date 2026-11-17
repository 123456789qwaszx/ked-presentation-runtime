using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// 틀 되돌리기 — tx · tx_stage_mask_clear가 마스크 커맨드 앞에 내는 커맨드(stage-frame.md §4, docs/PLAN_stage-code-homes.md §3).
// 마스크 커맨드는 틀을 모른다 — 무대를 새로 보여 주면 틀이 끝난다는 규칙은 파서가 두 커맨드로 드러낸다.
// 그 무대만 · 전부 · 까닭이 있을 때만 경고 · 파서가 내는 순서를 고정한다. 그림 팝업은 틀이 아니다(무대 안의 리그).
public class StageFrameResetTests
{
    private sealed class Pace : ICommandPacePolicy
    {
        public bool Compress;
        public bool CompressExecution => Compress;
    }

    private readonly List<Object> _spawned = new();
    private StageFrameRegistry _frames;

    [SetUp]
    public void SetUp()
    {
        GameObject go = new("__Mount", typeof(RectTransform));
        _spawned.Add(go);
        RectTransform mount = (RectTransform)go.transform;
        mount.sizeDelta = new Vector2(1920f, 1080f);

        StageRigRegistry stages = new(new StageRigBuilder());
        stages.Build(mount, "stage00", StageLayerLabels.Depths);
        stages.Build(mount, "stage01", StageLayerLabels.Depths);
        stages.Build(mount, "stage02", StageLayerLabels.Depths);
        _frames = new StageFrameRegistry(stages);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in _spawned)
        {
            if (o != null)
                Object.DestroyImmediate(o);
        }

        _spawned.Clear();
    }

    private void Open(string stage)
    {
        CutInPresetDBSO cutIn = CutInPresetDBSO.CreateDefault();
        _spawned.Add(cutIn);

        _frames.CutIn(stage).SnapOpen(cutIn, new Vector2(-640f, 50f), 1f, null);
    }

    private void Run(StageFrameResetCommandSpec spec, bool compress = false, bool keepOpenFrames = false)
    {
        IEnumerator run = new StageFrameResetCommand(spec, _frames, keepOpenFrames).Execute(new CommandRunScope(new Pace { Compress = compress }));

        while (run.MoveNext())
        {
            if (run.Current is IEnumerator inner)
                while (inner.MoveNext()) { }
        }
    }

    private bool IsNeutral(string stage)
        => !_frames.CutIn(stage).IsOpen
           && _frames.CutIn(stage).Window.localScale == Vector3.one;

    [Test]
    public void 그_무대의_틀만_중립으로_돌리고_까닭을_적어_경고한다()
    {
        Open("stage01");
        Open("stage02");

        LogAssert.Expect(LogType.Warning, new Regex("stage01의 컷인을 tx 'daze_close'가 덮는다"));

        Run(new StageFrameResetCommandSpec { stage = "stage01", cause = "tx 'daze_close'" });

        Assert.IsTrue(IsNeutral("stage01"));
        Assert.IsTrue(_frames.CutIn("stage02").IsOpen, "다른 무대의 틀은 그대로");
    }

    [Test]
    public void 까닭이_없으면_조용히_닫는다()
    {
        Open("stage01");

        int warnings = 0;
        void Count(string message, string stack, LogType type)
        {
            if (type == LogType.Warning && message.Contains("[StageFrameResetCommand]"))
                warnings++;
        }

        Application.logMessageReceived += Count;

        try
        {
            Run(new StageFrameResetCommandSpec { stage = "stage01" });
        }
        finally
        {
            Application.logMessageReceived -= Count;
        }

        Assert.IsTrue(IsNeutral("stage01"));
        Assert.AreEqual(0, warnings, "tx_stage_mask_clear처럼 까닭 없이 닫을 때는 경고하지 않는다");
    }

    [Test]
    public void 전부면_모든_무대의_틀을_닫는다_압축도_같다()
    {
        Open("stage01");
        Open("stage02");

        Run(new StageFrameResetCommandSpec { allStages = true }, compress: true);

        Assert.IsTrue(IsNeutral("stage00"));
        Assert.IsTrue(IsNeutral("stage01"));
        Assert.IsTrue(IsNeutral("stage02"));
    }

    // ── 실험 A — 열린 틀은 그대로(docs/PLAN_stage-code-homes.md §8, 정하면 지운다) ──

    [Test]
    public void 실험A_열린_컷인은_그대로_두고_경고만_한다()
    {
        Open("stage01");

        LogAssert.Expect(LogType.Warning, new Regex("stage01의 열린 컷인 위에 tx 'iris_out'"));

        Run(new StageFrameResetCommandSpec { stage = "stage01", cause = "tx 'iris_out'" }, keepOpenFrames: true);

        Assert.IsTrue(_frames.CutIn("stage01").IsOpen);
    }

    [Test]
    public void 실험A_닫힌_컷인은_중립_그대로다()
    {
        Open("stage01");
        _frames.CutIn("stage01").SnapClose();
        Assert.IsTrue(IsNeutral("stage01"), "닫힌 컷인 창은 항등이다 — 숨기는 것은 마스크다");

        Run(new StageFrameResetCommandSpec { allStages = true }, keepOpenFrames: true);

        Assert.IsTrue(IsNeutral("stage01"));
    }

    [Test]
    public void 기다리지_않는다()
    {
        Assert.IsFalse(new StageFrameResetCommand(new StageFrameResetCommandSpec(), _frames).WaitForCompletion);
    }

    // ── 파서가 내는 순서 ─────────────────────────────────────────

    private static List<CommandSpecBase> Parse(string name, params string[] args)
    {
        List<CommandSpecBase> output = new();
        Assert.IsTrue(new StageCommandParser(null).TryParse(new StageCommand(name, args, "test.yarn:1"), output));
        return output;
    }

    [Test]
    public void tx는_그_무대의_틀_되돌리기를_먼저_내고_마스크_모션을_낸다()
    {
        List<CommandSpecBase> specs = Parse("tx", "daze_close", "stage00");

        Assert.AreEqual(2, specs.Count);

        StageFrameResetCommandSpec reset = (StageFrameResetCommandSpec)specs[0];
        Assert.AreEqual("stage00", reset.stage);
        Assert.IsFalse(reset.allStages);
        Assert.AreEqual("tx 'daze_close'", reset.cause, "경고에 적을 까닭 — 정규화한 프리셋 키");

        StageMaskMotionCommandSpec mask = (StageMaskMotionCommandSpec)specs[1];
        Assert.AreEqual("stage00", mask.stage);
        Assert.AreEqual("daze_close", mask.presetKey);
    }

    [Test]
    public void tx_stage_mask_clear는_틀_전부를_조용히_되돌리고_마스크_전부를_지운다()
    {
        List<CommandSpecBase> specs = Parse("tx_stage_mask_clear");

        Assert.AreEqual(2, specs.Count);

        StageFrameResetCommandSpec reset = (StageFrameResetCommandSpec)specs[0];
        Assert.IsTrue(reset.allStages);
        Assert.IsTrue(string.IsNullOrEmpty(reset.cause), "지우기는 경고하지 않는다(종전과 같다)");

        Assert.IsTrue(((StageMaskClearCommandSpec)specs[1]).allStages);
    }
}
