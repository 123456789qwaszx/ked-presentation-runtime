using UnityEngine;

// Identifies which visual style of option box to present.
// Extend as new box variants are authored (e.g. corner list, character-anchored bubble, full-screen).
public enum VNOptionsBoxStyle
{
    Default = 0,
}

// Inbound request for an option-box transition. This is the option-side counterpart to
// DialogueBoxPresentationOptions, and is the place to grow future presentation intent
// (fade timing, box variant, character anchoring) without touching the flow.
public sealed class VNOptionsBoxPresentationOptions
{
    public bool UseImmediateTransition { get; set; }
    public VNOptionsBoxStyle Style { get; set; }

    // When set, the box anchors next to the named character's slot rather than its default position.
    // Null means "use the default placement".
    public string AnchorCharacterName { get; set; }
}

// Outcome of an option-box transition. Mirrors DialogueBoxPresentationResult:
// an invalid result signals that the run became stale and the flow must abort without committing.
public sealed class VNOptionsBoxPresentationResult
{
    public bool IsValid { get; private set; }

    // The container the presenter should parent option items under for this box.
    public RectTransform ItemContainer { get; private set; }

    public static VNOptionsBoxPresentationResult Invalid()
    {
        return new VNOptionsBoxPresentationResult
        {
            IsValid = false,
        };
    }

    public static VNOptionsBoxPresentationResult Ready(RectTransform itemContainer)
    {
        return new VNOptionsBoxPresentationResult
        {
            IsValid = true,
            ItemContainer = itemContainer,
        };
    }
}