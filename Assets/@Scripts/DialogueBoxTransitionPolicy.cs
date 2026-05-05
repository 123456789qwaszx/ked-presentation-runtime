using System;

public enum DialogueBoxTransitionKind
{
    Keep = 0,
    Cut = 1,
    FadeIn = 2,
    FadeOutIn = 3,
    Hide = 4
}

[Serializable]
public sealed class DialogueBoxTransitionPolicy
{
    public DialogueBoxTransitionKind Resolve(
        DialogueBoxKind? currentBoxKind,
        bool isBoxVisible,
        DialogueBoxKind nextBoxKind,
        string[] metadata,
        bool consumeSilently)
    {
        if (consumeSilently)
            return DialogueBoxTransitionKind.Cut;

        if (TryResolveTransitionFromMetadata(metadata, out DialogueBoxTransitionKind metadataTransition))
            return metadataTransition;

        if (!isBoxVisible || currentBoxKind.HasValue == false)
            return DialogueBoxTransitionKind.FadeIn;

        if (currentBoxKind.Value == nextBoxKind)
            return DialogueBoxTransitionKind.Keep;

        return DialogueBoxTransitionKind.FadeOutIn;
    }

    private static bool TryResolveTransitionFromMetadata(
        string[] metadata,
        out DialogueBoxTransitionKind transition)
    {
        transition = default;

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
                case "boxtransition=keep":
                case "boxtransition:keep":
                case "box_transition=keep":
                case "box_transition:keep":
                case "boxkeep":
                case "box_keep":
                    transition = DialogueBoxTransitionKind.Keep;
                    return true;

                case "boxtransition=cut":
                case "boxtransition:cut":
                case "box_transition=cut":
                case "box_transition:cut":
                case "boxcut":
                case "box_cut":
                    transition = DialogueBoxTransitionKind.Cut;
                    return true;

                case "boxtransition=fade":
                case "boxtransition:fade":
                case "box_transition=fade":
                case "box_transition:fade":
                case "boxfade":
                case "box_fade":
                    transition = DialogueBoxTransitionKind.FadeOutIn;
                    return true;

                case "boxtransition=fadein":
                case "boxtransition:fadein":
                case "box_transition=fadein":
                case "box_transition:fadein":
                case "boxfadein":
                case "box_fadein":
                case "box_fade_in":
                    transition = DialogueBoxTransitionKind.FadeIn;
                    return true;

                case "boxtransition=hide":
                case "boxtransition:hide":
                case "box_transition=hide":
                case "box_transition:hide":
                case "boxhide":
                case "box_hide":
                    transition = DialogueBoxTransitionKind.Hide;
                    return true;
            }
        }

        return false;
    }
}