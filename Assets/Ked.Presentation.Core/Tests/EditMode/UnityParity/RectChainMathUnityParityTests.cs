using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Ked.Presentation.Core.Tests
{
    /// <summary>
    /// 유니티 대조 하네스 — b-1의 핵심 산출물이자 U14의 축소판.
    ///
    /// 실제 RectTransform 계층을 코드로 조립해, 같은 상태를 RectNodeState 체인으로 만들고
    /// rect.TransformPoint / InverseTransformPoint와 RectChainMath의 결과를 비교한다.
    ///
    /// "월드"는 루트의 로컬 좌표다. 유니티 쪽도 root.InverseTransformPoint(world)로
    /// 루트 로컬로 내려서 비교한다 — 코어 규약(rootSpace 로컬)과 같은 공간이 된다.
    ///
    /// ε = 0.01px: 이 하네스는 체인이 얕아(≤ 6단) float 잡음 상한이 ~0.01px이다.
    /// 정책 전문과 U14용 ε(0.1px)은 Documentation~/transform-math-and-epsilon.md.
    /// </summary>
    public sealed class RectChainMathUnityParityTests
    {
        private const float Eps = 0.01f;

        private static readonly Vector3[] SamplePoints =
        {
            Vector3.zero,
            new Vector3(123.4f, -56.7f, 0f),
            new Vector3(-321f, 210f, 0f),
            new Vector3(960f, -540f, 0f),
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);
            }

            _spawned.Clear();
        }

        // ── 케이스 ───────────────────────────────────────────────────

        [Test]
        public void 스트레치_풀_3단_anchoredPosition()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(100f, 50f)),
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(-30f, 10f)),
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(5f, 5f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void 리그_모양_바닥_pivot과_스케일()
        {
            // CharSlot_DepthY > CharSlot_DepthScale(바닥 pivot + 스케일) > CharSlot_Track 모양.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(0f, -120f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithLocalScale(new Vec3(0.8f, 0.8f, 1f)),
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(240f, 0f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void Z_회전과_스케일_중첩()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithAnchoredPosition(new Vec2(50f, -20f))
                    .WithLocalEuler(new Vec3(0f, 0f, 15f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithLocalScale(new Vec3(1.3f, 0.7f, 1f))
                    .WithLocalEuler(new Vec3(0f, 0f, -30f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void 고정_앵커와_sizeDelta_초상화_이미지_모양()
        {
            // CharRigImageSizingPolicy가 만드는 상태: sizeDelta로 폭 지정, pivot.x 정렬.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithPivot(new Vec2(0.5f, 0f)),
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.Half, Vec2.Half)
                    .WithSizeDelta(new Vec2(730f, 0f))
                    .WithPivot(new Vec2(0f, 0f))
                    .WithAnchoredPosition(new Vec2(0f, 12f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void 부분_스트레치와_sizeDelta_오버레이_모양()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.Zero, new Vec2(1f, 0.5f))
                    .WithSizeDelta(new Vec2(-100f, 20f))
                    .WithAnchoredPosition(new Vec2(30f, -15f)),
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(-12f, 8f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void 오일러_3축_순서()
        {
            // Quaternion.Euler(Z→X→Y)와 순서가 같은지를 고정하는 케이스.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithLocalEuler(new Vec3(10f, 20f, 30f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void 전_요소_혼합_5단()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithAnchoredPosition(new Vec2(120f, -40f))
                    .WithLocalScale(new Vec3(1.25f, 1.25f, 1f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithLocalEuler(new Vec3(0f, 0f, 15f)),
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.Half, Vec2.Half)
                    .WithSizeDelta(new Vec2(300f, 600f))
                    .WithAnchoredPosition(new Vec2(-80f, 33f))
                    .WithLocalScale(new Vec3(0.8f, 0.9f, 1f)),
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.Zero, new Vec2(1f, 0.5f))
                    .WithSizeDelta(new Vec2(-100f, 20f))
                    .WithAnchoredPosition(new Vec2(7f, -7f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0f, 1f))
                    .WithAnchoredPosition(new Vec2(3f, 4f))
                    .WithLocalEuler(new Vec3(5f, -10f, 45f)),
            };

            AssertParity(chain);
        }

        [Test]
        public void 루트_pivot이_가운데가_아니어도_같다()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(10f, 20f)),
                RectNodeState.StretchFull.WithPivot(new Vec2(0.5f, 0f)),
            };

            AssertParity(chain, rootPivot: new Vector2(0.5f, 0f));
        }

        // ── 하네스 ───────────────────────────────────────────────────

        /// <summary>
        /// 같은 체인을 유니티와 코어에 각각 세우고 표본점 전부에서
        /// TransformPoint / InverseTransformPoint 양방향을 비교한다.
        /// </summary>
        private void AssertParity(RectNodeState[] chain, Vector2? rootPivot = null)
        {
            Vector2 pivot = rootPivot ?? new Vector2(0.5f, 0.5f);

            RectTransform root = CreateRoot(new Vector2(1920f, 1080f), pivot);
            RectTransform leaf = BuildUnityChain(root, chain);

            RectSpace space = new RectSpace(
                new Vec2(root.rect.size.x, root.rect.size.y),
                new Vec2(root.pivot.x, root.pivot.y));

            foreach (Vector3 p in SamplePoints)
            {
                // 로컬 → "월드"(루트 로컬).
                Vector3 unityWorld = root.InverseTransformPoint(leaf.TransformPoint(p));
                Vec3 coreWorld = RectChainMath.TransformPoint(
                    chain, space, new Vec3(p.x, p.y, p.z));

                AssertNear(unityWorld, coreWorld, $"TransformPoint({p})");

                // "월드"(루트 로컬) → 로컬.
                Vector3 unityLocal = leaf.InverseTransformPoint(root.TransformPoint(p));
                Vec3 coreLocal = RectChainMath.InverseTransformPoint(
                    chain, space, new Vec3(p.x, p.y, p.z));

                AssertNear(unityLocal, coreLocal, $"InverseTransformPoint({p})");
            }
        }

        private RectTransform CreateRoot(Vector2 size, Vector2 pivot)
        {
            GameObject go = new GameObject("ParityRoot", typeof(RectTransform));
            _spawned.Add(go);

            RectTransform rt = (RectTransform)go.transform;

            // 부모가 없는 RectTransform은 앵커가 딛을 것이 없어 sizeDelta가 곧 rect 크기다.
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            rt.sizeDelta = size;

            return rt;
        }

        /// <summary>
        /// 부모→자식 순서로 완전히 구성한다. 나중에 부모를 바꾸지 않으므로
        /// 자식 localPosition이 낡은 부모 rect로 계산되는 일이 없다.
        /// </summary>
        private RectTransform BuildUnityChain(RectTransform root, RectNodeState[] chain)
        {
            RectTransform parent = root;

            for (int i = 0; i < chain.Length; i++)
            {
                GameObject go = new GameObject($"Node{i}", typeof(RectTransform));
                RectTransform rt = (RectTransform)go.transform;
                rt.SetParent(parent, worldPositionStays: false);

                RectNodeState s = chain[i];
                rt.anchorMin = new Vector2(s.AnchorMin.X, s.AnchorMin.Y);
                rt.anchorMax = new Vector2(s.AnchorMax.X, s.AnchorMax.Y);
                rt.pivot = new Vector2(s.Pivot.X, s.Pivot.Y);
                rt.sizeDelta = new Vector2(s.SizeDelta.X, s.SizeDelta.Y);
                rt.anchoredPosition = new Vector2(s.AnchoredPosition.X, s.AnchoredPosition.Y);
                rt.localScale = new Vector3(s.LocalScale.X, s.LocalScale.Y, s.LocalScale.Z);
                rt.localEulerAngles = new Vector3(
                    s.LocalEulerAngles.X, s.LocalEulerAngles.Y, s.LocalEulerAngles.Z);

                parent = rt;
            }

            parent.ForceUpdateRectTransforms();
            return parent;
        }

        private static void AssertNear(Vector3 unity, Vec3 core, string context)
        {
            Assert.That(core.X, Is.EqualTo(unity.x).Within(Eps), $"{context} X");
            Assert.That(core.Y, Is.EqualTo(unity.y).Within(Eps), $"{context} Y");
            Assert.That(core.Z, Is.EqualTo(unity.z).Within(Eps), $"{context} Z");
        }
    }
}
