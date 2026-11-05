using System;
using UnityEngine;

/// <summary>
/// 연출 무대의 뿌리 — 무대가 서는 뷰포트 층, 무대 위 리그 층, 화면 효과 층.
///
/// 무대 자체(세 무대 · 카메라 · 깊이 층 · 마스크)는 여기 없다. 조립(SampleBootstrap)이 StageRigBuilder로
/// 뷰포트 층 아래에 세운다 — 계층은 StageRigSchema가 소유하고, 씬은 뷰포트 층만 가진다(2026-11-05).
/// 종전엔 76개 노드를 이름으로 찾아 세 인터페이스로 내줬다. 그 노드는 전부 항등이었고, 이름 목록이 곧 두 번째 스키마였다.
///
/// 화면(타이틀·대사창·선택지·메뉴)은 갖지 않는다 — 새 예제 UI(Samples/KedUI, UIPresentationFlow 위)가 그 위 캔버스에서 그린다.
/// 칸은 자식 오브젝트의 이름(Refs)으로 찾는다. 같은 이름이 둘이면 앞(깊이 우선)의 것.
/// 클래스·스크립트 GUID는 예전 UI 화면이던 때 그대로다 — 씬의 컴포넌트가 이어진다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PresentationUIRoot : MonoBehaviour, IStageMount
{
    public enum Refs
    {
        StageViewportLayer,

        StageOverlayLayer,
        SpriteRig_Root,
        TextRig_Root,

        ScreenEffectLayer,
    }

    private RectTransform _stageViewportLayer;
    private RectTransform _screenEffectLayer;

    private bool _initialized;

    /// <summary>무대가 서는 자리(StageViewportLayer). StageRigBuilder의 mount.</summary>
    public RectTransform StageMount => _stageViewportLayer;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 칸을 찾아 둔다. 조립(SampleBootstrap.AssemblePresentation)이 무대를 세우기 전에 부른다. 여러 번 불러도 한 번만 한다.
    /// 빠진 칸은 예외다 — 씬이 깨진 것이고, 경고로 넘어가면 무대가 엉뚱한 곳에 선다.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _stageViewportLayer = Require(Refs.StageViewportLayer);
        _screenEffectLayer = Require(Refs.ScreenEffectLayer);

        _initialized = true;
    }

    private RectTransform Require(Refs key)
    {
        string name = key.ToString();

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return (RectTransform)t;
        }

        throw new InvalidOperationException($"[PresentationUIRoot] 칸이 없다: {name}");
    }
}
