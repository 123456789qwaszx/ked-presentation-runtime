public sealed class DialogueBoxMetadataResolver
{
    public bool TryResolveBoxKind(
        string[] metadata,
        out DialogueBoxKind kind)
    {
        kind = default(DialogueBoxKind);

        if (metadata == null || metadata.Length == 0)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            string tag = NormalizeMetadataTag(metadata[i]);

            if (string.IsNullOrEmpty(tag))
                continue;

            if (TryResolveBoxKindTag(tag, out kind))
                return true;
        }

        return false;
    }

    public bool TryResolveTransitionKind(
        string[] metadata,
        out DialogueBoxTransitionKind transition)
    {
        transition = default(DialogueBoxTransitionKind);

        if (metadata == null || metadata.Length == 0)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            string tag = NormalizeMetadataTag(metadata[i]);

            if (string.IsNullOrEmpty(tag))
                continue;

            if (TryResolveTransitionTag(tag, out transition))
                return true;
        }

        return false;
    }

    // ---- Presentation beat detection (Phase 3) ----
    // A presentation beat is a line that plays staging only: no dialogue box/typewriter,
    // but it still consumes a sub advance and is recorded to backlog/rollback (handled by
    // the shared line front-matter). It auto-advances when the staging completes.
    // Marker tags: #beat / #stage / #stage_only / #present
    public static bool IsPresentationBeat(string[] metadata)
    {
        if (metadata == null)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            switch (NormalizeMetadataTag(metadata[i]))
            {
                case "beat":
                case "stage":
                case "stage_only":
                case "present":
                    return true;
            }
        }

        return false;
    }

    // #stay modifier on a beat: after the staging completes, wait for the player to advance
    // instead of auto-advancing. (Distinct from the sub-lane "hold", which is wait=true driven.)
    // Marker tags: #stay / #beat_stay / #no_auto
    public static bool IsBeatStay(string[] metadata)
    {
        if (metadata == null)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            switch (NormalizeMetadataTag(metadata[i]))
            {
                case "stay":
                case "beat_stay":
                case "no_auto":
                    return true;
            }
        }

        return false;
    }

    private static bool TryResolveBoxKindTag(
        string tag,
        out DialogueBoxKind kind)
    {
        kind = default(DialogueBoxKind);

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

            case "blackbook":
            case "black_book":
            case "box:blackbook":
            case "box=blackbook":
                kind = DialogueBoxKind.BlackBook;
                return true;
        }

        return false;
    }

    private static bool TryResolveTransitionTag(
        string tag,
        out DialogueBoxTransitionKind transition)
    {
        transition = default(DialogueBoxTransitionKind);

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

        return false;
    }

    private static string NormalizeMetadataTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        return tag.Trim().ToLowerInvariant();
    }
}