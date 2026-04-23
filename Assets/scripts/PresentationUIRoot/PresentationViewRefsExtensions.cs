using UnityEngine;

public enum PresentationTarget
{
    FullscreenFade_Root,
    Letterbox_Root,
    Flash_Root,
    ScreenOverlay_Root,

    StageShot_Root,
    StagePan_Root,
    StageZoom_Root,
    Stage_Root,

    BackgroundSystem_Root,
    BGShot_Root,
    BGContent_Root,
    BGOverlay_Root,

    CharacterSystem_Root,
    Foreground_Root,

    DialogueUI_Root,
    DialogueBox_Root,
    NameBox_Root,
    NarrationBox_Root,

    Choice_Root,
    SystemUI_Root
}

public static class PresentationViewRefsExtensions
{
    public static RectTransform GetRect(this PresentationViewRefs refs, PresentationTarget target)
    {
        if (refs == null) return null;

        return target switch
        {
            PresentationTarget.FullscreenFade_Root => refs.FullscreenFade_Root,
            PresentationTarget.Letterbox_Root => refs.Letterbox_Root,
            PresentationTarget.Flash_Root => refs.Flash_Root,
            PresentationTarget.ScreenOverlay_Root => refs.ScreenOverlay_Root,

            PresentationTarget.StageShot_Root => refs.StageShot_Root,
            PresentationTarget.StagePan_Root => refs.StagePan_Root,
            PresentationTarget.StageZoom_Root => refs.StageZoom_Root,
            PresentationTarget.Stage_Root => refs.Stage_Root,

            PresentationTarget.BackgroundSystem_Root => refs.BackgroundSystem_Root,
            PresentationTarget.BGShot_Root => refs.BGShot_Root,
            PresentationTarget.BGContent_Root => refs.BGContent_Root,
            PresentationTarget.BGOverlay_Root => refs.BGOverlay_Root,

            PresentationTarget.CharacterSystem_Root => refs.CharacterSystem_Root,
            PresentationTarget.Foreground_Root => refs.Foreground_Root,

            PresentationTarget.DialogueUI_Root => refs.DialogueUI_Root,
            PresentationTarget.DialogueBox_Root => refs.DialogueBox_Root,
            PresentationTarget.NameBox_Root => refs.NameBox_Root,
            PresentationTarget.NarrationBox_Root => refs.NarrationBox_Root,

            PresentationTarget.Choice_Root => refs.Choice_Root,
            PresentationTarget.SystemUI_Root => refs.SystemUI_Root,
            _ => null
        };
    }
}