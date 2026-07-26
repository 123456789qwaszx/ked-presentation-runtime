/// <summary>
/// 3일간의 접객이 끝난 뒤 확정되는 엔딩.
/// 개별 몬스터가 아니라 메이드의 상태와 상대한 종족 구성으로 결정된다.
/// </summary>
public sealed class CampaignEndingResult
{
    public string EndingKey { get; private set; }
    public string Title { get; private set; }
    public string NodeName { get; private set; }
    public string Reason { get; private set; }
    public bool IsBadEnding { get; private set; }

    /// <summary>배드엔딩이 종족 단위로 수렴한 경우 그 종족.</summary>
    public MonsterSpecies CollapseSpecies { get; private set; }

    public static CampaignEndingResult Create(
        string endingKey,
        string title,
        string nodeName,
        string reason,
        bool isBadEnding,
        MonsterSpecies collapseSpecies = MonsterSpecies.None)
    {
        return new CampaignEndingResult
        {
            EndingKey = endingKey,
            Title = title,
            NodeName = nodeName,
            Reason = reason,
            IsBadEnding = isBadEnding,
            CollapseSpecies = collapseSpecies,
        };
    }
}
