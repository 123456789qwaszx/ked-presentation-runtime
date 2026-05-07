using System;
using Yarn.Unity;


[Serializable]
public sealed class DialogueBoxLineRoutingPolicy
{
    private const DialogueBoxKind DefaultProtagonistLineBoxKind = DialogueBoxKind.Portrait;
    private const DialogueBoxKind DefaultNamedLineBoxKind = DialogueBoxKind.Speaker;

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    public DialogueBoxKind Resolve(bool hasCharacterName)
    {
        return hasCharacterName
            ? _namedLineBoxKind
            : _protagonistLineBoxKind;
    }

    public void SetProtagonistLineBoxKind(DialogueBoxKind kind)
    {
        _protagonistLineBoxKind = kind;
    }

    public void SetNamedLineBoxKind(DialogueBoxKind kind)
    {
        _namedLineBoxKind = kind;
    }

    public void ResetToDefaults()
    {
        _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
        _namedLineBoxKind = DefaultNamedLineBoxKind;
    }
    
    
    public bool TryResolveBoxKindFromMetadata(
        string[] metadata,
        out DialogueBoxKind kind)
    {
        kind = default(DialogueBoxKind);

        if (metadata == null || metadata.Length == 0)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            string tag = metadata[i];

            if (string.IsNullOrWhiteSpace(tag))
                continue;

            tag = tag.Trim().ToLowerInvariant();

            switch (tag)
            {
                case "portrait":
                case "box:portrait":
                case "box=portrait":
                    kind = DialogueBoxKind.Portrait;
                    return true;

                case "speaker":
                case "box:speaker":
                case "box=speaker":
                    kind = DialogueBoxKind.Speaker;
                    return true;

                case "letterbox":
                case "letter_box":
                case "box:letterbox":
                case "box=letterbox":
                    kind = DialogueBoxKind.LetterBox;
                    return true;

                case "onlytext":
                case "only_text":
                case "box:onlytext":
                case "box=onlytext":
                    kind = DialogueBoxKind.OnlyText;
                    return true;
            }
        }

        return false;
    }
}