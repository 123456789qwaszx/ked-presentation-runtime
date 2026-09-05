// 장면 안에서 확정된 선택 하나. 서버 큐의 ChoiceUpload와 대응.
public readonly struct CommittedChoice
{
    public string FromEpisodeId { get; } // 선택지가 붙어 있던 에피소드.
    public int OptionIndex { get; }      // 원본 NextOptions에서의 서수.

    public CommittedChoice(string fromEpisodeId, int optionIndex)
    {
        FromEpisodeId = fromEpisodeId;
        OptionIndex = optionIndex;
    }
}