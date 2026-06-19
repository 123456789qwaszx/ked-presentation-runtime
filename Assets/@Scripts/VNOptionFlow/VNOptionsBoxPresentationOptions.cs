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
}