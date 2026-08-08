using System;

namespace Ked.Presentation.Core
{
    public enum StageNodeClaimKind
    {
        AnchoredPosition,
        LocalScaleXY,
        LocalEulerAngles,
    }

    /// <summary>
    /// "스펙 → 목표 상태" 변환의 출력 단위 — 커맨드 하나가 노드 하나에 거는 목표값.
    ///
    /// 57곳에 흩어져 있던 "dest 계산" 관습을 타입 경계로 승격한 것.
    /// 이 값이 세 갈래로 흐르고, 셋이 같은 값을 보므로
    /// "재생 결과 = 정착 예약 = 정지 프레임"이 한 곳에서 갈라진다:
    ///
    ///   1. 장부 게시  : PlacementTargetLedger.Publish(claim)
    ///   2. 트윈 종점  : 호스트가 claim.Value를 DOTween 종점으로 (트윈은 시간의 세계라 호스트 몫)
    ///   3. 상태 폴드  : claim.ApplyTo(tree) — 정지 프레임 계산
    /// </summary>
    public readonly struct StageNodeClaim
    {
        public readonly string NodeKey;
        public readonly StageNodeClaimKind Kind;
        public readonly Vec3 Value;

        private StageNodeClaim(string nodeKey, StageNodeClaimKind kind, Vec3 value)
        {
            if (string.IsNullOrEmpty(nodeKey))
                throw new ArgumentException("클레임의 노드 키가 비어 있다.", nameof(nodeKey));

            NodeKey = nodeKey;
            Kind = kind;
            Value = value;
        }

        public static StageNodeClaim AnchoredPosition(string nodeKey, Vec2 value)
            => new(nodeKey, StageNodeClaimKind.AnchoredPosition, new Vec3(value, 0f));

        public static StageNodeClaim LocalScaleXY(string nodeKey, Vec2 value)
            => new(nodeKey, StageNodeClaimKind.LocalScaleXY, new Vec3(value, 0f));

        public static StageNodeClaim LocalEuler(string nodeKey, Vec3 value)
            => new(nodeKey, StageNodeClaimKind.LocalEulerAngles, value);

        /// <summary>클레임을 상태 값에 적용한다. 스케일 z 보존 규약이 여기 산다.</summary>
        public RectNodeState ApplyTo(in RectNodeState state)
        {
            switch (Kind)
            {
                case StageNodeClaimKind.AnchoredPosition:
                    return state.WithAnchoredPosition(Value.XY);

                case StageNodeClaimKind.LocalScaleXY:
                    return state.WithLocalScale(new Vec3(Value.X, Value.Y, state.LocalScale.Z));

                case StageNodeClaimKind.LocalEulerAngles:
                    return state.WithLocalEuler(Value);

                default:
                    throw new InvalidOperationException($"모르는 클레임 종류: {Kind}");
            }
        }

        /// <summary>트리의 해당 노드에 적용한다. 노드가 없으면 트리가 예외를 낸다(침묵 금지).</summary>
        public void ApplyTo(RectNodeTree tree)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));

            tree.SetState(NodeKey, ApplyTo(tree.GetState(NodeKey)));
        }

        public override string ToString() => $"{NodeKey}.{Kind} = {Value}";
    }
}