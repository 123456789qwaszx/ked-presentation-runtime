using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// 어둡게(stage_dim · stage_dim_reset) — 무대 리그의 층마다 선 어둡기 판. docs/PLAN_screen-cover.md §3.2.
// 어느 판을 고르나(all = 가장 앞 층, 층 이름 = 그 층) · 압축 끝값 · 걷기 · 정지에서 0 · 문법의 생략값을 고정한다.
public class StageDimTests
{
    private sealed class Pace : ICommandPacePolicy
    {
        public bool Compress;
        public bool CompressExecution => Compress;
    }

    private readonly List<GameObject> _spawned = new();
    private StageRigRegistry _stages;

    [SetUp]
    public void SetUp()
    {
        GameObject go = new("__Mount", typeof(RectTransform));
        _spawned.Add(go);
        RectTransform mount = (RectTransform)go.transform;
        mount.sizeDelta = new Vector2(1920f, 1080f);

        _stages = new StageRigRegistry(new StageRigBuilder());
        _stages.Build(mount, "stage00", StageLayerLabels.Depths);
        _stages.Build(mount, "stage01", StageLayerLabels.Depths);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned)
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }

        _spawned.Clear();
    }

    private StageLayerDimController Dim(string stage, float depth) => _stages.Get(stage).GetLayer(depth).Dim;

    private void Run(StageLayerDimCommandSpec spec, bool compress)
    {
        IEnumerator run = new StageLayerDimCommand(spec, _stages).Execute(new CommandRunScope(new Pace { Compress = compress }));

        // 커맨드는 안쪽 코루틴을 객체로 yield한다 — 한 겹 더 돌린다. 어둡게는 기다리지 않는다.
        while (run.MoveNext())
        {
            if (run.Current is IEnumerator inner)
                while (inner.MoveNext()) { }
        }
    }

    private static StageLayerDimCommandSpec Layer(float depth, float amount, string stage = "stage00")
        => new() { stage = stage, layerDepth = depth, amount = amount, duration = 0f, ease = Ease.Linear };

    private static StageLayerDimCommandSpec All(float amount, string stage = "stage00")
        => new() { stage = stage, allLayers = true, amount = amount, duration = 0f, ease = Ease.Linear };

    [Test]
    public void all은_그_무대의_가장_앞_층_판이다()
    {
        Run(All(0.45f), compress: false);

        Assert.AreEqual(0.45f, Dim("stage00", StageLayerLabels.Close).Amount, 1e-5f, "가장 앞 층의 판이 그 뒤를 모두 가린다 — 무대 전체");
        Assert.AreEqual(0f, Dim("stage00", StageLayerLabels.Far).Amount);
        Assert.AreEqual(0f, Dim("stage01", StageLayerLabels.Close).Amount, "다른 무대(컷인)는 밝다");

        Image image = Dim("stage00", StageLayerLabels.Close).GetComponent<Image>();
        Assert.IsTrue(image.enabled);
        Assert.AreEqual(new Color(0f, 0f, 0f, 0.45f), image.color);
    }

    [Test]
    public void 층_이름은_그_층의_판이다_far는_배경만()
    {
        Run(Layer(StageLayerLabels.Far, 0.6f), compress: false);

        Assert.AreEqual(0.6f, Dim("stage00", StageLayerLabels.Far).Amount, 1e-5f);
        Assert.AreEqual(0f, Dim("stage00", StageLayerLabels.Mid).Amount, "인물이 선 mid 층 위는 가리지 않는다");
    }

    [Test]
    public void 압축_중에는_끝값을_바로_세운다()
    {
        StageLayerDimCommandSpec spec = All(0.7f);
        spec.duration = 2f;

        Run(spec, compress: true);

        Assert.AreEqual(0.7f, Dim("stage00", StageLayerLabels.Close).Amount, 1e-5f, "빨리 넘기기 · 시크로 지나가도 어두운 채");
        Assert.IsFalse(Dim("stage00", StageLayerLabels.Close).IsTransitioning);
    }

    [Test]
    public void 세기_0은_그_판을_걷고_reset은_모든_무대의_판을_걷는다()
    {
        Run(All(0.5f), compress: true);
        Run(Layer(StageLayerLabels.Far, 0.6f), compress: true);
        Run(All(0.3f, "stage01"), compress: true);

        Run(Layer(StageLayerLabels.Far, 0f), compress: true);
        Assert.AreEqual(0f, Dim("stage00", StageLayerLabels.Far).Amount);
        Assert.IsFalse(Dim("stage00", StageLayerLabels.Far).GetComponent<Image>().enabled, "걷힌 판은 그리지 않는다");
        Assert.AreEqual(0.5f, Dim("stage00", StageLayerLabels.Close).Amount, 1e-5f, "다른 판은 그대로");

        Run(new StageLayerDimCommandSpec { resetAll = true, duration = 0f }, compress: false);

        foreach (StageRigRefs stage in _stages.All)
            foreach (StageLayerRefs layer in stage.Layers)
                Assert.AreEqual(0f, layer.Dim.Amount, $"{stage.StageKey} {layer.Depth}");
    }

    [Test]
    public void 정지에서_모든_판이_걷힌다()
    {
        Run(All(0.5f), compress: true);
        Run(All(0.5f, "stage01"), compress: true);

        _stages.ResetAllDimsToBaseline();

        foreach (StageRigRefs stage in _stages.All)
            foreach (StageLayerRefs layer in stage.Layers)
                Assert.AreEqual(0f, layer.Dim.Amount);
    }

    [Test]
    public void 어둡게는_전경이_아니다()
    {
        Assert.IsFalse(new StageLayerDimCommand(All(0.5f), _stages).WaitForCompletion, "기다리지 않는다 — 줄을 넘는다");
    }

    // ── 문법 ─────────────────────────────────────────────────────

    private static List<CommandSpecBase> Parse(string name, params string[] args)
    {
        List<CommandSpecBase> output = new();
        Assert.IsTrue(new StageCommandParser(null).TryParse(new StageCommand(name, args, "test.yarn:1"), output));
        return output;
    }

    [Test]
    public void 문법_층_세기_길이_이징_무대()
    {
        StageLayerDimCommandSpec spec = (StageLayerDimCommandSpec)Parse("stage_dim", "far", "0.6", "1s", "OutSine", "stage01")[0];

        Assert.IsFalse(spec.allLayers);
        Assert.AreEqual(StageLayerLabels.Far, spec.layerDepth);
        Assert.AreEqual(0.6f, spec.amount, 1e-5f);
        Assert.AreEqual("stage01", spec.stage);
        Assert.AreEqual(1f, spec.duration, 1e-4f);
        Assert.AreEqual(Ease.OutSine, spec.ease);
    }

    [Test]
    public void 문법_생략값_all_stage00_0점3초_Linear()
    {
        StageLayerDimCommandSpec spec = (StageLayerDimCommandSpec)Parse("stage_dim", "all", "0.45")[0];

        Assert.IsTrue(spec.allLayers);
        Assert.AreEqual("stage00", spec.stage);
        Assert.AreEqual(0.3f, spec.duration, 1e-4f);
        Assert.AreEqual(Ease.Linear, spec.ease);

        StageLayerDimCommandSpec reset = (StageLayerDimCommandSpec)Parse("stage_dim_reset")[0];
        Assert.IsTrue(reset.resetAll);
        Assert.AreEqual(0.3f, reset.duration, 1e-4f);
    }

    [Test]
    public void 문법_길이는_무대보다_앞이다()
    {
        // <<stage_dim all 0.2 0fr>> — 0fr는 길이다(무대가 아니다).
        StageLayerDimCommandSpec spec = (StageLayerDimCommandSpec)Parse("stage_dim", "all", "0.2", "0fr")[0];

        Assert.AreEqual("stage00", spec.stage);
        Assert.AreEqual(0f, spec.duration, 1e-4f);
    }

    [Test]
    public void 문법_세기를_적지_않으면_건너뛴다()
    {
        Assert.AreEqual(0, Parse("stage_dim", "all").Count, "걷으려면 0을 적는다 — 빈 세기를 짐작하지 않는다");
    }
}
