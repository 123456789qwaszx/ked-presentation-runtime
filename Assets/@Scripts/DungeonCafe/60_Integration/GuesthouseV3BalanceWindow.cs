#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// v3 밸런스 창: 헤드리스 §17 지표를 에디터에서 즉시 굴린다.
/// 플레이 모드 불필요 — 헤드리스 봇이 동기 완료되기 때문.
/// 메뉴: Guesthouse/V3 Balance Window
/// </summary>
public sealed class GuesthouseV3BalanceWindow : EditorWindow
{
    private int _seedCount = 100;
    private ulong _seedBase = 1000;
    private HeadlessPolicyV3 _policy = HeadlessPolicyV3.Ideal;
    private string _report = "아직 실행하지 않음.";
    private Vector2 _scroll;

    [MenuItem("Guesthouse/V3 Balance Window")]
    private static void Open() => GetWindow<GuesthouseV3BalanceWindow>("V3 Balance");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("헤드리스 §17 회귀", EditorStyles.boldLabel);
        _seedCount = EditorGUILayout.IntSlider("시드 개수", _seedCount, 1, 500);
        _seedBase = (ulong)EditorGUILayout.LongField("시드 베이스", (long)_seedBase);
        _policy = (HeadlessPolicyV3)EditorGUILayout.EnumPopup("정책", _policy);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("실행", GUILayout.Height(28)))
            {
                var report = GuesthouseV3HeadlessValidator.Run(_policy, _seedCount, _seedBase);
                _report = report.ToText();
            }
            if (GUILayout.Button("규칙 자가 검증", GUILayout.Height(28)))
            {
                _report = GuesthouseV3RuleSelfCheck.RunAll(out _);
            }
            if (GUILayout.Button("캠페인 1회 상세", GUILayout.Height(28)))
            {
                RunVerboseOnce();
            }
        }

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "지표가 목표를 벗어나면 코드가 아니라 GuesthouseV3TuningSO 를 조정한다. " +
            "조정 순서: 부하 범위(§2.2) → 심층 가산 계수(§4.3) → 할당 곡선(§1) → 가게 임계(§8).",
            MessageType.Info);
    }

    private void RunVerboseOnce()
    {
        var campaign = new CampaignStateV3(
            GuesthouseV3Content.Build(), GuesthouseTuningV3.CreateStandard(), _seedBase);
        var screens = new HeadlessV3Screens(_policy);
        var nodes = new HeadlessNodePlayerV3();
        _ = new CampaignFlowV3(campaign, screens, nodes).RunAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"엔딩: {campaign.Ending}  누적 욕구: {campaign.Ledger.Lifetime}  가게 Lv{campaign.ShopLevel}");
        sb.AppendLine($"접객 {screens.ServiceCount} / 착지 {screens.LandingCount} / 심층 {screens.DepthEntryCount} / 완전붕괴 {screens.TotalCollapseCount}");
        sb.AppendLine($"판정 커밋 {campaign.CommitLog.Entries.Count}건");
        for (int i = 0; i < campaign.Maids.Count; i++)
        {
            MaidStateV3 m = campaign.Maids[i];
            int stage = RelationRule.ResolveStage(m.RelationPoints, campaign.Tuning);
            sb.AppendLine($"  {m.DisplayName}: 게이지 {m.Gauge.Snapshot()} 관계 {m.RelationPoints}pt(단계{stage}) " +
                          $"기벽 {m.QuirkIds.Count} 후유증 {m.Aftereffects.Count} 생환권 {(m.HasRescueTicket ? "보유" : "소모")}" +
                          (m.IsLost ? " [이탈]" : ""));
        }
        sb.AppendLine();
        sb.AppendLine("재생 노드 (앞 60건):");
        for (int i = 0; i < nodes.PlayedNodes.Count && i < 60; i++)
            sb.AppendLine($"  {nodes.PlayedNodes[i]}");

        _report = sb.ToString();
        campaign.ReleaseCounters();
    }
}
#endif
