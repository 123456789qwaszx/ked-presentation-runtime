using System.Collections.Generic;
using NUnit.Framework;
using Ked.Presentation.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 무대 리그 리팩터의 오라클 — 런타임 샷 시스템이 무대에 쓰는 트랜스폼 전부(카메라 루트 2 + 층 30)가
    /// 코어 ShotLayoutMath.Solve의 출력과 같아야 한다.
    ///
    /// P1(지금)에서는 종전 런타임 식(PresentationResponseMath)이 코어 식과 같은지를 고정하고,
    /// P3(런타임이 코어 식을 쓰게 된 뒤)에도 같은 테스트가 그대로 통과해야 한다 — 그게 동작 불변의 증명이다.
    ///
    /// 실제 씬과 같은 계층을 세운다: ShotRoot → Pan → Zoom → Stage × 3 → DepthSystem → 층 × 5 (Root → FramingTransform → FramingScale).
    /// 씬 값은 전부 항등이다(2026-11-05 확인).
    /// </summary>
    public sealed class ShotLayoutUnityParityTests
    {
        private const float Eps = 1e-4f;

        private readonly List<GameObject> _spawned = new();

        private RectTransform _shotRoot;
        private RectTransform _pan;
        private RectTransform _zoom;
        private StageDepthLayerRects[][] _layers;   // [stage][layer]

        private FakeStageProvider _provider;
        private PresentationShotResponseSystem _system;

        private sealed class FakeStageProvider : IShotResponseStageProvider
        {
            private readonly RectTransform _rigSpaceRoot;
            private readonly RectTransform _pan;
            private readonly RectTransform _zoom;
            private readonly StageDepthLayerRects[][] _layers;

            public FakeStageProvider(RectTransform rigSpaceRoot, RectTransform pan, RectTransform zoom, StageDepthLayerRects[][] layers)
            {
                _rigSpaceRoot = rigSpaceRoot;
                _pan = pan;
                _zoom = zoom;
                _layers = layers;
            }

            public RectTransform RigSpaceRoot => _rigSpaceRoot;
            public RectTransform StagePanRoot => _pan;
            public RectTransform StageZoomRoot => _zoom;

            public StageDepthLayerRects GetLayerRects(PresentationStageKey root, PresentationDepthLayerKey layer)
                => _layers[(int)root][(int)layer];
        }

        [SetUp]
        public void SetUp()
        {
            _shotRoot = NewRoot("__ShotRoot");
            _pan = NewStretchChild("__Pan", _shotRoot);
            _zoom = NewStretchChild("__Zoom", _pan);

            int stageCount = (int)PresentationStageKey.Count;
            int layerCount = (int)PresentationDepthLayerKey.Count;

            _layers = new StageDepthLayerRects[stageCount][];

            for (int s = 0; s < stageCount; s++)
            {
                RectTransform stage = NewStretchChild($"__Stage0{s}", _zoom);
                RectTransform depthSystem = NewStretchChild("__DepthSystem", stage);

                _layers[s] = new StageDepthLayerRects[layerCount];

                for (int l = 0; l < layerCount; l++)
                {
                    string label = StageLayerLabels.Names[l];
                    RectTransform root = NewStretchChild($"__Depth_{label}_Root", depthSystem);
                    RectTransform framingTransform = NewStretchChild("__FramingTransform", root);
                    RectTransform framingScale = NewStretchChild("__FramingScale", framingTransform);

                    _layers[s][l] = new StageDepthLayerRects(root, framingTransform, framingScale);
                }
            }

            _provider = new FakeStageProvider(_shotRoot, _pan, _zoom, _layers);
            _system = new PresentationShotResponseSystem(_provider);
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

        private RectTransform NewRoot(string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            _spawned.Add(go);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1920f, 1080f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static RectTransform NewStretchChild(string name, RectTransform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static readonly ShotIntentState[] Intents =
        {
            ShotIntentState.Default,
            new(6f, Vec2.Zero, Vec2.Zero),                               // 줌만
            new(0f, new Vec2(250f, -30f), Vec2.Zero),                    // 팬만
            new(6f, Vec2.Zero, new Vec2(100f, 0f)),                      // 줌 + 초점 오른쪽
            new(6f, new Vec2(250f, -30f), new Vec2(-100f, 40f)),         // 전부 + 초점 왼쪽 위
            new(-4f, new Vec2(-120f, 60f), new Vec2(0f, -200f)),         // 줌 아웃 + 초점 아래
        };

        private static PresentationIntentState ToRuntime(in ShotIntentState s)
            => new()
            {
                zoom = s.Zoom,
                panInRigSpace = new Vector2(s.PanInRigSpace.X, s.PanInRigSpace.Y),
                focusPointInRigSpace = new Vector2(s.FocusPointInRigSpace.X, s.FocusPointInRigSpace.Y),
            };

        [Test]
        public void 런타임이_무대에_쓰는_트랜스폼_전부가_코어_Solve와_같다()
        {
            foreach (ShotIntentState intent in Intents)
            {
                _system.Snap(ToRuntime(in intent));

                ShotLayout layout = ShotLayoutMath.Solve(in intent, StageLayerLabels.Depths, LayerProfileCurve.Default);

                string tag = $"intent={{zoom {intent.Zoom}, pan {intent.PanInRigSpace}, focus {intent.FocusPointInRigSpace}}}";

                // 카메라 루트 둘
                Assert.That(_zoom.localScale.x, Is.EqualTo(layout.CameraScale).Within(Eps), $"{tag} Zoom_Root 배율 x");
                Assert.That(_zoom.localScale.y, Is.EqualTo(layout.CameraScale).Within(Eps), $"{tag} Zoom_Root 배율 y");
                Assert.That(_pan.anchoredPosition.x, Is.EqualTo(layout.Pan.X).Within(Eps), $"{tag} Pan_Root x");
                Assert.That(_pan.anchoredPosition.y, Is.EqualTo(layout.Pan.Y).Within(Eps), $"{tag} Pan_Root y");

                // 층 — 오프셋은 rig space 벡터를 층 부모 공간으로 옮긴 값이다. 부모는 Zoom_Root 아래라
                // 배율이 cameraScale이고 그 밖은 항등이므로, 부모 공간 벡터 = rig space 벡터 / cameraScale.
                for (int s = 0; s < _layers.Length; s++)
                {
                    for (int l = 0; l < _layers[s].Length; l++)
                    {
                        LayerResponse expected = layout.Layers[l];
                        Vector2 expectedAnchored = new(
                            expected.OffsetInRigSpace.X / layout.CameraScale,
                            expected.OffsetInRigSpace.Y / layout.CameraScale);

                        string where = $"{tag} stage0{s}/{StageLayerLabels.Names[l]}";

                        Vector2 actualAnchored = _layers[s][l].Position.anchoredPosition;
                        Vector3 actualScale = _layers[s][l].Scale.localScale;

                        Assert.That(actualAnchored.x, Is.EqualTo(expectedAnchored.x).Within(Eps), $"{where} 위치 x");
                        Assert.That(actualAnchored.y, Is.EqualTo(expectedAnchored.y).Within(Eps), $"{where} 위치 y");
                        Assert.That(actualScale.x, Is.EqualTo(expected.Scale.X).Within(Eps), $"{where} 배율 x");
                        Assert.That(actualScale.y, Is.EqualTo(expected.Scale.Y).Within(Eps), $"{where} 배율 y");
                    }
                }
            }
        }

        [Test]
        public void 중립은_Solve_Default다_런타임_TryGetNeutral과_같다()
        {
            // 샷을 한 번 움직인 뒤에도 중립은 Default의 값이어야 한다.
            _system.Snap(ToRuntime(Intents[4]));

            ShotLayout neutral = ShotLayoutMath.Solve(ShotIntentState.Default, StageLayerLabels.Depths, LayerProfileCurve.Default);

            Assert.That(_system.TryGetNeutral(_pan, out Vector2 panPos, out _), Is.True);
            Assert.That(panPos.x, Is.EqualTo(neutral.Pan.X).Within(Eps));

            Assert.That(_system.TryGetNeutral(_zoom, out _, out Vector3 zoomScale), Is.True);
            Assert.That(zoomScale.x, Is.EqualTo(neutral.CameraScale).Within(Eps));

            for (int l = 0; l < _layers[0].Length; l++)
            {
                Assert.That(_system.TryGetNeutral(_layers[0][l].Position, out Vector2 pos, out _), Is.True, StageLayerLabels.Names[l]);
                Assert.That(pos.x, Is.EqualTo(neutral.Layers[l].OffsetInRigSpace.X).Within(Eps));

                Assert.That(_system.TryGetNeutral(_layers[0][l].Scale, out _, out Vector3 scale), Is.True, StageLayerLabels.Names[l]);
                Assert.That(scale.x, Is.EqualTo(neutral.Layers[l].Scale.X).Within(Eps));
            }

            // 리그 노드 · 무대 루트는 샷 소유가 아니다.
            Assert.That(_system.TryGetNeutral(_shotRoot, out _, out _), Is.False);
        }
    }
}
