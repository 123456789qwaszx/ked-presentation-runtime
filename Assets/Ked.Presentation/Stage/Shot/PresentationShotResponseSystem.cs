using System;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

// 샷은 장면 전역 의도다 — 커맨드가 의도(Intent)를 쓰고, 화면(Presented)은 이 시스템이 따라간다.
//
// 전환은 축마다 하나다(ShotAxes — 줌 · 팬 · 롤 · 달리 · 트랙). 커맨드는 제가 적은 축만 다시 조준하고,
// 적지 않은 축은 하던 전환을 제 길이로 마친다 — 느린 달리 도중에 줌을 넣어도 달리는 제 길이로 간다.
// 새 의도가 오면 그 축은 지금 보이는 값에서 다시 조준한다(앞 전환을 완주시키지 않는다).
// 의도 · 화면은 다섯 축의 값을 합친 한 벌이다. 화면에 쓸 때는 그 한 벌을 통째로 다시 푼다(전환은 의도를 보간한다).
//
// 무대에 쓰는 값은 전부 코어 ShotLayoutMath의 출력이다. 이 클래스는 그 출력을 노드에 쓰는 applier이고,
// 중립(IStageViewNeutral)은 같은 식에 Default를 넣은 값이다 — 따로 적은 정의가 없다.
// 어느 노드가 샷 소유인지는 StageRigRefs(빌더 출력)가 안다. 참조 비교 분기를 따로 두지 않는다.
//
// 카메라 리그는 무대마다 있다(CameraRigSchema). 샷은 본 무대만 움직인다(StageRoles) — 덮는 무대(컷인 · 인서트 · 커튼)의
// 카메라와 층은 빌더가 세운 항등, 곧 중립에 고정이다(2026-11-06 D2). 무대별 intent가 필요해지면 이 클래스가 무대 키를 받으면 된다.
public sealed class PresentationShotResponseSystem : IStageViewNeutral
{
    private const float SameValueEpsilon = 0.0001f;

    private readonly StageRigRegistry _stages;
    private readonly StageRigRefs _main;
    private readonly LayerProfileCurve _profile;

    // ShotAxes의 비트 순서대로 — 줌 · 팬 · 롤 · 달리 · 트랙.
    private readonly IAxis[] _axes;
    private static readonly ShotAxes[] AxisFlags = { ShotAxes.Zoom, ShotAxes.Pan, ShotAxes.Roll, ShotAxes.Dolly, ShotAxes.Track };

    private PresentationIntentState _presented = PresentationIntentState.Default;

    // 저작된 의도 — 다음 샷 커맨드의 출발점이고, 폴드가 접는 값이다. 축마다의 목표를 합친 한 벌.
    public PresentationIntentState Intent
    {
        get
        {
            PresentationIntentState intent = PresentationIntentState.Default;

            for (int i = 0; i < _axes.Length; i++)
                _axes[i].WriteIntentTo(ref intent);

            return intent;
        }
    }

    // 지금 화면에 적용된 값. 전환 중인 축만 Intent와 다르다.
    public PresentationIntentState Presented => _presented;

    // 층이 샷에 반응하는 곡선 — Apply가 쓰는 것과 같다. 초점을 화면 지점에 놓는 팬을 풀 때(코어 ShotIntents.FocusTo) 같은 곡선을 본다.
    public LayerProfileCurve LayerProfile => _profile;

    // 화면 흔들림 — 의도가 아니라 연기다. 본 무대의 Camera_Shake에 클립을 틀고, 끝나면 항등으로 돌아온다.
    public CameraShakePlayer Shake { get; }

    public PresentationShotResponseSystem(StageRigRegistry stages, LayerProfileCurve profile)
    {
        _stages = stages;
        _main = stages.Get(StageRoles.MainStageKey);
        _profile = profile;

        Shake = new CameraShakePlayer(_main.Camera.Shake);

        _axes = new IAxis[]
        {
            // 초점은 줌에 실린다.
            new Axis<ZoomValue>(
                this,
                state => new ZoomValue(state.zoom, state.focusPointInRigSpace),
                (ref PresentationIntentState state, ZoomValue value) =>
                {
                    state.zoom = value.Zoom;
                    state.focusPointInRigSpace = value.Focus;
                },
                (from, to, t) => new ZoomValue(Mathf.Lerp(from.Zoom, to.Zoom, t), Vector2.Lerp(from.Focus, to.Focus, t)),
                (a, b) => Same(a.Zoom, b.Zoom) && Same(a.Focus, b.Focus)),

            new Axis<Vector2>(
                this,
                state => state.panInRigSpace,
                (ref PresentationIntentState state, Vector2 value) => state.panInRigSpace = value,
                Vector2.Lerp,
                Same),

            new Axis<float>(
                this,
                state => state.roll,
                (ref PresentationIntentState state, float value) => state.roll = value,
                Mathf.Lerp,
                Same),

            new Axis<float>(
                this,
                state => state.dolly,
                (ref PresentationIntentState state, float value) => state.dolly = value,
                Mathf.Lerp,
                Same),

            new Axis<Vector2>(
                this,
                state => state.track,
                (ref PresentationIntentState state, Vector2 value) => state.track = value,
                Vector2.Lerp,
                Same),
        };
    }

    /// <summary>axes 가운데 전환 중인 축이 있는가 — 기다리는 커맨드는 제 축만 기다린다.</summary>
    public bool IsTransitioning(ShotAxes axes)
    {
        for (int i = 0; i < _axes.Length; i++)
        {
            if ((axes & AxisFlags[i]) != 0 && _axes[i].IsTransitioning)
                return true;
        }

        return false;
    }

    /// <summary>
    /// intent에서 axes의 값만 읽어 그 축들을 다시 조준한다. 나머지 축은 건드리지 않는다.
    /// 이미 그 자리인 축은 전환을 만들지 않는다 — 기다리는 커맨드가 빈 전환을 기다리지 않게.
    /// </summary>
    public void Follow(in PresentationIntentState intent, ShotAxes axes, float duration, Ease ease)
    {
        for (int i = 0; i < _axes.Length; i++)
        {
            if ((axes & AxisFlags[i]) != 0)
                _axes[i].Follow(in intent, duration, ease);
        }
    }

    // 압축(빨리 넘기기·시크) — axes의 축을 의도로 바로 맞춘다.
    public void Snap(in PresentationIntentState intent, ShotAxes axes)
    {
        for (int i = 0; i < _axes.Length; i++)
        {
            if ((axes & AxisFlags[i]) != 0)
                _axes[i].Snap(in intent);
        }
    }

    // 모든 축을 의도로 바로 맞춘다.
    public void Snap(in PresentationIntentState intent) => Snap(in intent, ShotAxes.All);

    // 스텝 경계 — axes의 전환을 배율만큼 빠르게 끝까지 돌린다.
    public void AccelerateTransition(ShotAxes axes, float speedUp)
    {
        for (int i = 0; i < _axes.Length; i++)
        {
            if ((axes & AxisFlags[i]) != 0)
                _axes[i].Accelerate(speedUp);
        }
    }

    // 장면 경계 — 전환을 확정 없이 멈추고 의도·화면을 기본 샷으로. 흔들림도 끊는다.
    public void Clear()
    {
        Shake.Stop();
        Snap(PresentationIntentState.Default);
    }

    // ── 축 — 의도 한 벌의 한 조각과 그 전환 ───────────────────────────

    private static bool Same(float a, float b) => Mathf.Abs(a - b) <= SameValueEpsilon;

    private static bool Same(Vector2 a, Vector2 b) => Vector2.SqrMagnitude(a - b) <= SameValueEpsilon;

    private readonly struct ZoomValue
    {
        public readonly float Zoom;
        public readonly Vector2 Focus;

        public ZoomValue(float zoom, Vector2 focus)
        {
            Zoom = zoom;
            Focus = focus;
        }
    }

    private delegate void AxisWriter<T>(ref PresentationIntentState state, T value);

    private interface IAxis
    {
        bool IsTransitioning { get; }
        void Follow(in PresentationIntentState intent, float duration, Ease ease);
        void Snap(in PresentationIntentState intent);
        void Accelerate(float speedUp);
        void WriteIntentTo(ref PresentationIntentState state);
    }

    // 전환(IntentTransition) 하나가 의도 한 벌의 제 조각만 옮긴다. 보이는 값은 시스템의 _presented에 모이고,
    // 조각이 바뀔 때마다 한 벌을 통째로 다시 푼다.
    private sealed class Axis<T> : IAxis
    {
        private readonly PresentationShotResponseSystem _owner;
        private readonly Func<PresentationIntentState, T> _read;
        private readonly AxisWriter<T> _write;
        private readonly Func<T, T, bool> _same;
        private readonly IntentTransition<T> _transition;

        public Axis(
            PresentationShotResponseSystem owner,
            Func<PresentationIntentState, T> read,
            AxisWriter<T> write,
            Func<T, T, float, T> lerp,
            Func<T, T, bool> same)
        {
            _owner = owner;
            _read = read;
            _write = write;
            _same = same;

            _transition = new IntentTransition<T>(
                read(PresentationIntentState.Default),
                () => _read(_owner._presented),
                Present,
                lerp,
                owner);
        }

        public bool IsTransitioning => _transition.IsTransitioning;

        public void Follow(in PresentationIntentState intent, float duration, Ease ease)
        {
            T target = _read(intent);

            if (_same(_read(_owner._presented), target))
            {
                _transition.Snap(target);
                return;
            }

            _transition.Follow(target, duration, ease);
        }

        public void Snap(in PresentationIntentState intent) => _transition.Snap(_read(intent));

        public void Accelerate(float speedUp) => _transition.Accelerate(speedUp);

        public void WriteIntentTo(ref PresentationIntentState state) => _write(ref state, _transition.Intent);

        private void Present(T value)
        {
            _write(ref _owner._presented, value);
            _owner.ApplyPresented();
        }
    }

    // ── 적용 — 의도 한 벌 → 카메라 노드 · 층 ─────────────────────────

    private void ApplyPresented()
    {
        ShotIntentState intent = _presented.ToCore();
        ApplyToStage(in intent, _main);
    }

    // 코어가 노드 로컬 값을 그대로 낸다 — 여기서는 받아 쓰기만 한다. 좌표 변환도 하지 않는다.
    // Camera_Shake는 쓰지 않는다 — 의도가 아니라 연기라서, 흔들림을 트는 쪽이 따로 쓴다.
    private void ApplyToStage(in ShotIntentState intent, StageRigRefs stage)
    {
        CameraPose pose = ShotLayoutMath.SolveCamera(in intent);
        CameraRigRefs camera = stage.Camera;

        camera.Pan.anchoredPosition = pose.Pan.ToUnity();
        camera.Zoom.localScale = new Vector3(pose.ZoomScale, pose.ZoomScale, 1f);
        camera.Roll.localEulerAngles = new Vector3(0f, 0f, pose.Roll);
        camera.Dolly.localScale = new Vector3(pose.DollyScale, pose.DollyScale, 1f);
        camera.Track.anchoredPosition = pose.TrackPosition.ToUnity();

        IReadOnlyList<StageLayerRefs> layers = stage.Layers;

        for (int i = 0; i < layers.Count; i++)
        {
            StageLayerRefs layer = layers[i];
            LayerResponse response = ShotLayoutMath.SolveLayer(in intent, layer.Depth, _profile);

            layer.Rect.anchoredPosition = response.LocalPosition.ToUnity();
            layer.Rect.localScale = new Vector3(response.Scale.X, response.Scale.Y, 1f);
        }
    }

    // ── 중립 = Solve(Default) ────────────────────────────────────────
    // 샷 소유는 구조다(무대마다 카메라 리그 · 층 전부). 덮는 무대의 것도 중립을 답한다 — 샷이 쓰지 않으니 라이브도 같은 항등이다.
    // 카메라 노드의 중립은 전부 항등이다(흔들림도 — 흔들리는 중에 재도 값이 같다). 회전의 중립은 언제나 0이라 내지 않는다.

    public bool TryGetNeutral(RectTransform rect, out Vector2 anchoredPosition, out Vector3 localScale)
    {
        anchoredPosition = default;
        localScale = Vector3.one;

        IReadOnlyList<StageRigRefs> stages = _stages.All;

        for (int i = 0; i < stages.Count; i++)
        {
            StageRigRefs stage = stages[i];

            if (!stage.IsShotOwned(rect, out StageLayerRefs layer))
                continue;

            // 카메라 노드 — 항등(위에서 넣어 둔 값 그대로).
            if (layer == null)
                return true;

            ShotIntentState neutral = ShotIntentState.Default;

            LayerResponse response = ShotLayoutMath.SolveLayer(in neutral, layer.Depth, _profile);
            anchoredPosition = response.LocalPosition.ToUnity();
            localScale = new Vector3(response.Scale.X, response.Scale.Y, 1f);

            return true;
        }

        return false;
    }
}
