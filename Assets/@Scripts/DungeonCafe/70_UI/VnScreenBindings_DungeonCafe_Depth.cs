using System.Collections.Generic;
using Yarn.Unity;

// 붕괴심층 3국면 + 방치 결과 통보.
//
// 심층은 세 번 UI 과정
// 1) 굴림 전 개입 (능력 예약)
// 2) 굴림 제시 (재굴림/구간 하향/수용)
// 3) 회수 구간에서의 탈출/잔류 선택
// 판정 자체는 전부 시스템(ServiceSessionFlow)이 커밋. 이 곳에선 묻고 전달만 함.
public sealed partial class VnScreenBindings
{
    private bool _hasDepthResult;
    private IReadOnlyList<string> _pendingDepthAbilities;
    private string _pendingRollAbilityId;
    private bool _pendingRecoveryEscape;

    private bool _isDepthPanelOpen;

    // ------------------------------------------------------------
    // 국면 1: 굴림 전 개입
    // ------------------------------------------------------------
    public async YarnTask<IReadOnlyList<string>> RequestDepthInterventionAsync(DepthInterventionRequest request)
    {
        NotifyControlLost();                       // 낮 승인 패널이 남아 있으면 잠근다.
        RefreshDungeonCafeHud(request.Session, "붕괴심층");

        _hasDepthResult = false;
        _pendingDepthAbilities = null;

        OpenDepthPanel(panel => panel.PresentIntervention(request));

        await AsyncWait.UntilAsync(() => _hasDepthResult);

        // 선택이 끝나면 접는다 - 굴림/행동 노드가 화면을 쓴다. 다음 국면이 다시 연다.
        CloseDepthPanelIfOpen();

        return _pendingDepthAbilities;
    }

    // ------------------------------------------------------------
    // 국면 2: 굴림 제시 - 재굴림/하향은 능력 효과로 판별해 응답을 만든다.
    // ------------------------------------------------------------
    public async YarnTask<DepthRollDecision> PresentDepthRollAsync(
        ServiceSessionState session, DepthRollResult roll, IReadOnlyList<string> postRollAbilityIds)
    {
        RefreshDungeonCafeHud(session, "붕괴심층 - 굴림");

        _hasDepthResult = false;
        _pendingRollAbilityId = null;

        OpenDepthPanel(panel => panel.PresentRoll(session, roll, postRollAbilityIds));

        await AsyncWait.UntilAsync(() => _hasDepthResult);

        CloseDepthPanelIfOpen();

        if (string.IsNullOrEmpty(_pendingRollAbilityId) || _dungeonCafeCampaign == null)
            return DepthRollDecision.None;

        PlayerAbilityDefinition def = _dungeonCafeCampaign.Content.GetAbility(_pendingRollAbilityId);
        if (def == null)
            return DepthRollDecision.None;

        return def.EffectKind == AbilityEffectKind.DepthBandDowngrade
            ? new DepthRollDecision(reroll: null, downgrade: _pendingRollAbilityId)
            : new DepthRollDecision(reroll: _pendingRollAbilityId, downgrade: null);
    }

    // ------------------------------------------------------------
    // 국면 3: 회수 선택 - Depth_Recover 노드 재생 직후에 열린다.
    // ------------------------------------------------------------
    public async YarnTask<bool> RequestRecoveryChoiceAsync(ServiceSessionState session)
    {
        RefreshDungeonCafeHud(session, "붕괴심층 - 회수");

        _hasDepthResult = false;
        _pendingRecoveryEscape = true;

        OpenDepthPanel(panel => panel.PresentRecoveryChoice(session));

        await AsyncWait.UntilAsync(() => _hasDepthResult);

        CloseDepthPanelIfOpen();

        return _pendingRecoveryEscape;
    }

    // ------------------------------------------------------------
    // 방치 결과 통보.
    // ------------------------------------------------------------
    public async YarnTask PresentNeglectAsync(MaidState maid, NeglectJudgment judgment)
    {
        CloseDepthPanelIfOpen();

        await PresentConfirmAsync(
            title: $"{maid.DisplayName} - 방치된 밤",
            body: BuildNeglectBody(judgment),
            confirmLabel: "확인");
    }

    private static string BuildNeglectBody(in NeglectJudgment judgment)
    {
        switch (judgment.Outcome)
        {
            case NeglectCollapseOutcome.NaturalRecovery:
                return $"조용히 잠들었습니다. 붕괴 {judgment.CollapseBefore} -> {judgment.CollapseAfter}";

            case NeglectCollapseOutcome.DangerHold:
                return $"위험 구간에서 밤을 버텼습니다. 붕괴 {judgment.CollapseBefore} 유지";

            case NeglectCollapseOutcome.SelfRelease:
                return $"혼자서 어떻게든 가라앉혔습니다. 붕괴 {judgment.CollapseBefore} -> {judgment.CollapseAfter}" +
                       (judgment.GainsAccidentQuirk ? "\n…무언가가 몸에 남았습니다. (사고성 기벽)" : string.Empty);

            default: // NightIncident
                return $"심야, 방에서 소리가 났습니다. 붕괴 {judgment.CollapseBefore} - 자동 심층 {judgment.IncidentDepthBeats}비트";
        }
    }

    // ------------------------------------------------------------
    // 패널 관리 - 심층은 국면마다 같은 패널을 다시 그림.
    // ------------------------------------------------------------
    private void OpenDepthPanel(System.Action<DepthPanel> present)
    {
        if (_isDepthPanelOpen)
        {
            DepthPanel open = UI.GetUI<DepthPanel>();
            if (open != null)
            {
                present(open);
                return;
            }

            _isDepthPanelOpen = false;
        }

        UI.PushPanel<DepthPanel>(panel =>
        {
            BindPanel(panel, ApplyDepthBindings);
            present(panel);
        });

        _isDepthPanelOpen = true;
    }

    private void CloseDepthPanelIfOpen()
    {
        if (!_isDepthPanelOpen)
            return;

        _isDepthPanelOpen = false;
        ClosePanel();
    }

    private void ApplyDepthBindings(DepthPanel panel)
    {
        AddBinding(panel,
            p => p.OnInterventionConfirmed += HandleDepthIntervention,
            p => p.OnInterventionConfirmed -= HandleDepthIntervention);
        AddBinding(panel,
            p => p.OnRollDecided += HandleDepthRollDecided,
            p => p.OnRollDecided -= HandleDepthRollDecided);
        AddBinding(panel,
            p => p.OnRecoveryChosen += HandleDepthRecoveryChosen,
            p => p.OnRecoveryChosen -= HandleDepthRecoveryChosen);
    }

    private void HandleDepthIntervention(IReadOnlyList<string> abilityIds)
    {
        _pendingDepthAbilities = abilityIds;
        _hasDepthResult = true;
    }

    private void HandleDepthRollDecided(string abilityId)
    {
        _pendingRollAbilityId = abilityId;
        _hasDepthResult = true;
    }

    private void HandleDepthRecoveryChosen(bool escape)
    {
        _pendingRecoveryEscape = escape;
        _hasDepthResult = true;
    }
}