using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터별 접객 템플릿 저작 에셋.
/// 비트는 선언 순서가 곧 선형 진행 순서이며, 옵션의 nextBeatKey 로 분기한다.
/// </summary>
[CreateAssetMenu(
    fileName = "ServiceScenarioDB",
    menuName = "Guesthouse/Service Scenario DB")]
public sealed class ServiceScenarioDBSO : ScriptableObject
{
    [Serializable]
    public sealed class OptionEntry
    {
        public string optionKey;

        [TextArea(1, 3)]
        [Tooltip("승인 버튼에 표시되는 메이드의 제안 문구.")]
        public string proposalText;

        public string approvalNodeName;

        [Tooltip("대응력 완화 전 원본 부하.")]
        public AxisTriple load;

        public MonsterReactionGrade reaction = MonsterReactionGrade.Satisfied;
        public int satisfactionBonus;

        [Tooltip("비우면 선언 순서상 다음 비트로 진행한다.")]
        public string nextBeatKey;

        public BurdenAxis requiredAptitudeAxis = BurdenAxis.Physical;
        public int requiredAptitude;

        public string preferredTraitKey;
        public int riskWeight;
        public bool isTerminalAction;
    }

    [Serializable]
    public sealed class BeatEntry
    {
        public string beatKey;
        public string situationNodeName;
        public bool isTerminal;

        [Tooltip("이 비트에서 메이드가 제안할 행동 수.")]
        public int offerCount = ServiceBeat.DefaultOfferCount;

        public OptionEntry[] options = Array.Empty<OptionEntry>();
    }

    [Serializable]
    public sealed class ScenarioEntry
    {
        public string scenarioKey;
        public string monsterId;
        public string briefingNodeName;
        public string completionNodeName;

        [Tooltip("소화할 비트 수 상한. 0이면 비트 개수를 그대로 쓴다.")]
        public int beatBudget;

        public BeatEntry[] beats = Array.Empty<BeatEntry>();
    }

    [SerializeField] private ScenarioEntry[] entries = Array.Empty<ScenarioEntry>();

    private readonly List<ServiceScenario> _scenarios = new();

    private void OnEnable()
    {
        RebuildIndex();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildIndex();
    }
#endif

    private void RebuildIndex()
    {
        _scenarios.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            ScenarioEntry entry = entries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.scenarioKey))
                continue;

            _scenarios.Add(BuildScenario(entry));
        }
    }

    private static ServiceScenario BuildScenario(ScenarioEntry entry)
    {
        List<ServiceBeat> beats = new(entry.beats.Length);

        for (int i = 0; i < entry.beats.Length; i++)
        {
            BeatEntry beatEntry = entry.beats[i];

            if (beatEntry == null || string.IsNullOrWhiteSpace(beatEntry.beatKey))
                continue;

            beats.Add(new ServiceBeat(
                beatEntry.beatKey.Trim(),
                beatEntry.situationNodeName,
                BuildOptions(beatEntry.options),
                beatEntry.isTerminal,
                beatEntry.offerCount));
        }

        return new ServiceScenario(
            entry.scenarioKey.Trim(),
            entry.monsterId,
            entry.briefingNodeName,
            entry.completionNodeName,
            beats,
            entry.beatBudget);
    }

    private static List<ServiceActionOption> BuildOptions(OptionEntry[] source)
    {
        List<ServiceActionOption> options = new(source.Length);

        for (int i = 0; i < source.Length; i++)
        {
            OptionEntry option = source[i];

            if (option == null || string.IsNullOrWhiteSpace(option.optionKey))
                continue;

            options.Add(new ServiceActionOption(
                option.optionKey.Trim(),
                option.proposalText,
                option.approvalNodeName,
                option.load,
                option.reaction,
                option.satisfactionBonus,
                option.nextBeatKey,
                option.requiredAptitudeAxis,
                option.requiredAptitude,
                option.preferredTraitKey,
                option.riskWeight,
                option.isTerminalAction));
        }

        return options;
    }

    public IReadOnlyList<ServiceScenario> BuildScenarios()
    {
        if (_scenarios.Count != entries.Length)
            RebuildIndex();

        return _scenarios;
    }
}
