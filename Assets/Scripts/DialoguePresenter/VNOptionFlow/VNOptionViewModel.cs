using Yarn.Unity;

public sealed class VNOptionViewModel
{
    public DialogueOption SourceOption { get; private set; }

    // 원본 dialogueOptions 배열 기준 index.
    // UI 표시 index가 아니라 replay에 사용할 원본 option index.
    public int SourceOptionIndex { get; private set; }

    // 장면 시작 이후 몇 번째 option set인지.
    public int ChoiceSequence { get; private set; }

    public string Label { get; private set; }
    public bool IsAvailable { get; private set; }
    
    public VNOptionViewModel(
        DialogueOption sourceOption,
        int sourceOptionIndex,
        int choiceSequence,
        string label,
        bool isAvailable)
    {
        SourceOption = sourceOption;
        SourceOptionIndex = sourceOptionIndex;
        ChoiceSequence = choiceSequence;

        Label = label ?? string.Empty;
        IsAvailable = isAvailable;
    }
}