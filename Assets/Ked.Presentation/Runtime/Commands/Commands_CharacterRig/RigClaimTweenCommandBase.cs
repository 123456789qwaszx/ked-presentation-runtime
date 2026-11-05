using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 캐릭터 리그 위에서 목표값을 향해 트윈하는 커맨드의 공통 수명 —
/// ClaimTweenCommandBase 위에 "정착 장부"를 얹은 것.
///
/// 장부 게시·Clear는 여기가 한다. 파생은 목표를 <see cref="Claim"/>으로 기록만 한다.
/// 2026-08에 게시를 커맨드마다 손으로 넣었더니 둘(회전 · 슬라이드)이 석 달 동안 빠져 있었다 —
/// 같은 줄의 place가 그 노드의 트윈 중간값을 읽어 일반 재생과 롤백이 다른 자리를 냈다.
/// 그래서 "게시를 잊는다"를 구조적으로 불가능하게 만든다.
///
/// 게시 대상인지는 판정하지 않는다. 측정 체인 아래 노드도 게시한다 — 아무도 안 읽을 뿐 틀리지 않는다.
///
/// 리그는 언제나 있다 — 슬롯 선언 없이 리그 커맨드가 오는 상태는 대본 단계에서 막힌다.
/// 그래서 리그·rect의 null은 방어하지 않는다. 깨졌다면 예외로 드러나야 한다.
///
/// 파생이 채우는 것:
///   ResolveRects    — RigRefs에서 rect를 꺼낸다 (리그 해석은 여기가 했다)
///   ClaimTarget     — DOKill → (from-override) → 목표 계산 → Claim(rect, claim)
///                     목표를 낼 수 없는 커맨드(place의 해 실패)는 Claim을 부르지 않고 돌아온다 — 그게 실패다
///   WriteFinalState — 최종 쓰기만. Clear는 여기가 한다
/// </summary>
public abstract class RigClaimTweenCommandBase : ClaimTweenCommandBase
{
    private readonly PresentationStage _stage;
    private readonly string _slotKey;

    private readonly List<(RectTransform rect, StageNodeClaim claim)> _claims = new(4);

    protected CharacterRigRefs RigRefs { get; private set; }

    protected RigClaimTweenCommandBase(PresentationStage stage, string slotKey)
    {
        _stage = stage;
        _slotKey = slotKey;
    }

    protected abstract void ResolveRects();

    // 실행기의 ClaimTarget(scope)와 같은 단계다. 리그 커맨드는 scope를 읽지 않으므로(무대는 생성자 주입) 인자를 뺐다.
    protected abstract void ClaimTarget();

    protected abstract void WriteFinalState();

    /// <summary>목표 하나를 기록한다. 게시는 ClaimTarget이 끝난 뒤 기반이 한꺼번에 한다.</summary>
    protected void Claim(RectTransform rect, in StageNodeClaim claim)
        => _claims.Add((rect, claim));

    protected sealed override void ResolveTargets(CommandRunScope scope)
    {
        RigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _slotKey)
            ?? throw new System.InvalidOperationException(
                $"[{GetType().Name}] '{_slotKey}'의 리그가 없다. 리그 커맨드는 슬롯 선언 뒤에만 온다.");

        ResolveRects();
    }

    protected sealed override bool ClaimTarget(CommandRunScope scope)
    {
        _claims.Clear();

        ClaimTarget();

        // 클레임 없이 트윈하는 클레임-트윈 커맨드는 없다 — 클레임이 없으면 목표를 못 낸 것이다.
        if (_claims.Count == 0)
            return false;

        for (int i = 0; i < _claims.Count; i++)
            RigRefs.PlacementTargets.Publish(_claims[i].rect, _claims[i].claim);

        return true;
    }

    protected sealed override void OnCommitFinalState()
    {
        WriteFinalState();

        for (int i = 0; i < _claims.Count; i++)
            RigRefs.PlacementTargets.Clear(_claims[i].rect);

        _claims.Clear();
    }
}
