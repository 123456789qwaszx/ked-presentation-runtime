using System.Collections.Generic;
using NUnit.Framework;
using Ked.Presentation.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// 무대 리그는 스키마가 소유한다 — 씬이 깨져도 빌더가 다시 세운다(캐릭터 리그 b470fc88과 같은 시험).
    /// </summary>
    public sealed class StageRigBuilderTests
    {
        private readonly List<GameObject> _spawned = new();
        private RectTransform _mount;
        private StageRigBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new("__Mount", typeof(RectTransform));
            _spawned.Add(go);
            _mount = (RectTransform)go.transform;
            _mount.sizeDelta = new Vector2(1920f, 1080f);
            _builder = new StageRigBuilder();
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

        [Test]
        public void 스키마대로_선다_카메라_리그는_마스크_아래_층은_그_가장_안쪽_아래()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);

            Assert.That(refs.Root.parent, Is.EqualTo(_mount));
            Assert.That(refs.MaskRoot.parent, Is.EqualTo(refs.Root));
            Assert.That(refs.MaskEdgeRoot.parent, Is.EqualTo(refs.Root));
            Assert.That(refs.Camera.Root.parent, Is.EqualTo(refs.MaskRoot));
            Assert.That(refs.MaskRoot.childCount, Is.EqualTo(1), "마스크 아래는 카메라 리그 하나다");

            Assert.That(refs.Layers.Count, Is.EqualTo(5));

            for (int i = 0; i < refs.Layers.Count; i++)
            {
                StageLayerRefs layer = refs.Layers[i];
                Assert.That(layer.Rect.parent, Is.EqualTo(refs.Camera.Mount));
                Assert.That(layer.Content.parent, Is.EqualTo(layer.Rect));
                Assert.That(layer.Rect.GetSiblingIndex(), Is.EqualTo(i), "먼 층이 앞 형제 = 뒤에 그려진다");
                Assert.That(layer.Depth, Is.EqualTo(StageLayerLabels.Depths[i]));
            }

            Assert.That(refs.Layers[0].Rect.name, Is.EqualTo("stage00_Layer_far"));
            Assert.That(refs.Layers[0].Content.name, Is.EqualTo("stage00_Layer_far_Content"));

            Assert.That(refs.Mask.HasMask, Is.True);
            Assert.That(refs.MaskRoot.GetComponent<Mask>().showMaskGraphic, Is.False);
            Assert.That(refs.MaskEdgeRoot.GetComponent<StageMaskEdgeGraphic>(), Is.Not.Null);
        }

        [Test]
        public void 카메라_리그는_스키마의_사슬대로_선다()
        {
            // 바깥 → 안: Root → Shake → Pan → Zoom → Roll → Dolly → Track. 카메라에서 부모인 몸이 가장 안쪽이다.
            CameraRigRefs camera = _builder.Build(_mount, "stage00", StageLayerLabels.Depths).Camera;

            foreach (CameraRigSchema.NodeDef node in CameraRigSchema.Nodes)
            {
                RectTransform rect = camera.Get(node.Id);

                Assert.That(rect.name, Is.EqualTo($"stage00_{node.Id}"));

                if (node.Parent.HasValue)
                    Assert.That(rect.parent, Is.EqualTo(camera.Get(node.Parent.Value)), node.Id.ToString());
            }

            Assert.That(camera.Shake.parent, Is.EqualTo(camera.Root));
            Assert.That(camera.Pan.parent, Is.EqualTo(camera.Shake));
            Assert.That(camera.Zoom.parent, Is.EqualTo(camera.Pan));
            Assert.That(camera.Roll.parent, Is.EqualTo(camera.Zoom));
            Assert.That(camera.Dolly.parent, Is.EqualTo(camera.Roll));
            Assert.That(camera.Track.parent, Is.EqualTo(camera.Dolly));
            Assert.That(camera.Mount, Is.SameAs(camera.Track), "층은 가장 안쪽 노드 아래에 선다");
        }

        [Test]
        public void 모든_노드는_항등이다()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);

            foreach (RectTransform rect in refs.Root.GetComponentsInChildren<RectTransform>(true))
            {
                Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero), rect.name);
                Assert.That(rect.localScale, Is.EqualTo(Vector3.one), rect.name);
                Assert.That(rect.localEulerAngles, Is.EqualTo(Vector3.zero), rect.name);
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)), rect.name);
            }
        }

        [Test]
        public void 층은_참조될_때_서고_깊이_순서로_끼어든다()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", new[] { StageLayerLabels.Far, StageLayerLabels.Close });

            StageLayerRefs mid = _builder.EnsureLayer(refs, StageLayerLabels.Mid);

            Assert.That(refs.Layers.Count, Is.EqualTo(3));
            Assert.That(refs.Layers[1], Is.SameAs(mid));
            Assert.That(mid.Rect.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(_builder.EnsureLayer(refs, StageLayerLabels.Mid), Is.SameAs(mid), "같은 d는 같은 층");

            StageLayerRefs odd = _builder.EnsureLayer(refs, 0.25f);
            Assert.That(odd.Rect.name, Is.EqualTo("stage00_Layer_0.25"));
            Assert.That(odd.Rect.GetSiblingIndex(), Is.EqualTo(2), "mid(0)와 close(1) 사이");
        }

        [Test]
        public void 있는_계층은_그대로_묶고_깨진_계층은_다시_세운다()
        {
            StageRigRefs first = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);
            RectTransform pan = first.Camera.Pan;

            // 멀쩡한 계층 — 같은 노드를 묶는다.
            StageRigRefs again = _builder.BindRefsFromRoot(first.Root, "stage00");
            Assert.That(again.Camera.Pan, Is.SameAs(pan));
            Assert.That(again.Layers.Count, Is.EqualTo(5));

            // 층 하나를 카메라 리그 밖으로 옮긴다 — 계층이 깨졌다.
            first.Layers[2].Rect.SetParent(first.Root, false);

            StageRigRefs rebuilt = _builder.BindRefsFromRoot(first.Root, "stage00");
            Assert.That(rebuilt.Camera.Pan, Is.Not.SameAs(pan), "부수고 새로 세웠다");
            Assert.That(rebuilt.Layers.Count, Is.EqualTo(0), "층은 참조될 때 다시 선다");
            Assert.That(rebuilt.Mask.HasMask, Is.True);
        }

        [Test]
        public void 카메라_리그의_사슬이_어긋나면_무대를_다시_세운다()
        {
            // 카메라 리그 이전의 계층(마스크 아래에 팬 · 줌 둘)도 이 경우다 — 노드가 빠졌거나 순서가 다르다.
            StageRigRefs first = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);
            RectTransform shake = first.Camera.Shake;

            // 롤을 사슬에서 뺀다: 달리를 줌 바로 아래로 옮기고 롤을 부순다.
            first.Camera.Dolly.SetParent(first.Camera.Zoom, false);
            first.Camera.Roll.SetParent(null, false);
            Object.DestroyImmediate(first.Camera.Roll.gameObject);

            StageRigRefs rebuilt = _builder.BindRefsFromRoot(first.Root, "stage00");

            Assert.That(rebuilt.Camera.Shake, Is.Not.SameAs(shake), "부수고 새로 세웠다");
            Assert.That(rebuilt.Camera.Roll, Is.Not.Null);
            Assert.That(rebuilt.Camera.Dolly.parent, Is.EqualTo(rebuilt.Camera.Roll));
        }

        [Test]
        public void 샷_소유는_카메라_리그의_노드와_층의_Rect뿐이다()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);

            foreach (CameraRigSchema.NodeDef node in CameraRigSchema.Nodes)
            {
                Assert.That(refs.IsShotOwned(refs.Camera.Get(node.Id), out StageLayerRefs none), node.Id.ToString());
                Assert.That(none, Is.Null, "카메라 노드는 층이 아니다");
            }

            Assert.That(refs.IsShotOwned(refs.Layers[3].Rect, out StageLayerRefs layer) && layer == refs.Layers[3]);
            Assert.That(refs.IsShotOwned(refs.Layers[3].Content, out _), Is.False);
            Assert.That(refs.IsShotOwned(refs.Root, out _), Is.False);
            Assert.That(refs.IsShotOwned(refs.MaskRoot, out _), Is.False);
        }
    }
}
