using System.Collections.Generic;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;

// 화면 밖 무대(StageRigRegistry.BuildOffscreen — 촬영소) — 키로는 꺼내지만 화면의 무대 목록(All)에는 없다.
// 화면 전체에 걸리는 일(마스크 지우기 · 어둡게 · 샷 · 틀 · 캡처)이 All을 돌기 때문에, 이 경계가 촬영소를 그 일들에서 뺀다.
// docs/PLAN_portrait-capture.md §3 규칙 3.
public class StageRigRegistryOffscreenTests
{
    private readonly List<GameObject> _spawned = new();
    private StageRigRegistry _stages;
    private RectTransform _offscreenMount;

    [SetUp]
    public void SetUp()
    {
        RectTransform mount = Mount("__Mount");
        _offscreenMount = Mount("__OffscreenMount");

        _stages = new StageRigRegistry(new StageRigBuilder());
        _stages.Build(mount, "stage00", StageLayerLabels.Depths);
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

    private RectTransform Mount(string name)
    {
        GameObject go = new(name, typeof(RectTransform));
        _spawned.Add(go);

        RectTransform mount = (RectTransform)go.transform;
        mount.sizeDelta = new Vector2(1920f, 1080f);

        return mount;
    }

    [Test]
    public void 화면_밖_무대는_키로_꺼내지만_화면의_무대_목록에는_없다()
    {
        StageRigRefs offscreen = _stages.BuildOffscreen(_offscreenMount, PortraitStudio.StageKey);

        Assert.That(_stages.Get(PortraitStudio.StageKey), Is.SameAs(offscreen));
        Assert.That(_stages.All.Count, Is.EqualTo(1));
        Assert.That(_stages.All[0].StageKey, Is.EqualTo("stage00"));
    }

    [Test]
    public void 화면_밖_무대에도_층이_선다()
    {
        _stages.BuildOffscreen(_offscreenMount, PortraitStudio.StageKey);

        StageLayerRefs layer = _stages.EnsureLayer(PortraitStudio.StageKey, StageLayerLabels.Mid);

        Assert.That(layer.Content, Is.Not.Null);
        Assert.That(layer.Content.IsChildOf(_offscreenMount), Is.True);
    }

    [Test]
    public void 장면_경계의_되돌리기는_화면_밖_무대의_마스크를_건드리지_않는다()
    {
        StageRigRefs offscreen = _stages.BuildOffscreen(_offscreenMount, PortraitStudio.StageKey);
        offscreen.Mask.SetMaskedHidden();
        Vector2 hidden = offscreen.Mask.Graphic.ShapeOffsetPixels;

        _stages.ResetAllMasksToBaseline();

        Assert.That(hidden, Is.Not.EqualTo(Vector2.zero));
        Assert.That(offscreen.Mask.Graphic.ShapeOffsetPixels, Is.EqualTo(hidden));
    }

    [Test]
    public void 같은_키로_두_번_세우면_예외다()
    {
        _stages.BuildOffscreen(_offscreenMount, PortraitStudio.StageKey);

        Assert.Throws<System.InvalidOperationException>(
            () => _stages.BuildOffscreen(_offscreenMount, PortraitStudio.StageKey));
        Assert.Throws<System.InvalidOperationException>(
            () => _stages.BuildOffscreen(_offscreenMount, "stage00"));
    }
}
