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

    Stage00_Root,
    Stage00BackgroundSystem_Root,
    Stage00BGShot_Root,
    Stage00BGContent_Root,
    Stage00BGOverlay_Root,
    Stage00CharacterSystem_Root,
    Stage00CharSlot_Root,
    Stage00CharSlotFocus_Root,
    Stage00CharSlotRig_Root,
    Stage00Foreground_Root,

    Stage01_Root,
    Stage01BackgroundSystem_Root,
    Stage01BGShot_Root,
    Stage01BGContent_Root,
    Stage01BGOverlay_Root,
    Stage01CharacterSystem_Root,
    Stage01CharSlot_Root,
    Stage01CharSlotFocus_Root,
    Stage01CharSlotRig_Root,
    Stage01Foreground_Root,

    Stage02_Root,
    Stage02BackgroundSystem_Root,
    Stage02BGShot_Root,
    Stage02BGContent_Root,
    Stage02BGOverlay_Root,
    Stage02CharacterSystem_Root,
    Stage02CharSlot_Root,
    Stage02CharSlotFocus_Root,
    Stage02CharSlotRig_Root,
    Stage02Foreground_Root,

    DialogueUI_Root,
    DialogueBox_Root,
    NameBox_Root,
    NarrationBox_Root,

    Choice_Root,
    SystemUI_Root,
    
    VerticalStripWipe,
    SlantedShutter
}

public static class PresentationViewRefsExtensions
{
    public static RectTransform GetRect(this PresentationViewRefs refs, PresentationTarget target)
    {
        if (refs == null)
            return null;

        return target switch
        {
            PresentationTarget.FullscreenFade_Root => refs.FullscreenFade_Root,
            PresentationTarget.Letterbox_Root => refs.Letterbox_Root,
            PresentationTarget.Flash_Root => refs.Flash_Root,
            PresentationTarget.ScreenOverlay_Root => refs.ScreenOverlay_Root,

            PresentationTarget.StageShot_Root => refs.StageShot_Root,
            PresentationTarget.StagePan_Root => refs.StagePan_Root,
            PresentationTarget.StageZoom_Root => refs.StageZoom_Root,

            PresentationTarget.Stage00_Root => refs.Stage00_Root,
            PresentationTarget.Stage00BackgroundSystem_Root => refs.Stage00BackgroundSystem_Root,
            PresentationTarget.Stage00BGShot_Root => refs.Stage00BGShot_Root,
            PresentationTarget.Stage00BGContent_Root => refs.Stage00BGContent_Root,
            PresentationTarget.Stage00BGOverlay_Root => refs.Stage00BGOverlay_Root,
            PresentationTarget.Stage00CharacterSystem_Root => refs.Stage00CharacterSystem_Root,
            PresentationTarget.Stage00CharSlot_Root => refs.Stage00CharSlot_Root,
            PresentationTarget.Stage00CharSlotFocus_Root => refs.Stage00CharSlotFocus_Root,
            PresentationTarget.Stage00CharSlotRig_Root => refs.Stage00CharSlotRig_Root,
            PresentationTarget.Stage00Foreground_Root => refs.Stage00Foreground_Root,

            PresentationTarget.Stage01_Root => refs.Stage01_Root,
            PresentationTarget.Stage01BackgroundSystem_Root => refs.Stage01BackgroundSystem_Root,
            PresentationTarget.Stage01BGShot_Root => refs.Stage01BGShot_Root,
            PresentationTarget.Stage01BGContent_Root => refs.Stage01BGContent_Root,
            PresentationTarget.Stage01BGOverlay_Root => refs.Stage01BGOverlay_Root,
            PresentationTarget.Stage01CharacterSystem_Root => refs.Stage01CharacterSystem_Root,
            PresentationTarget.Stage01CharSlot_Root => refs.Stage01CharSlot_Root,
            PresentationTarget.Stage01CharSlotFocus_Root => refs.Stage01CharSlotFocus_Root,
            PresentationTarget.Stage01CharSlotRig_Root => refs.Stage01CharSlotRig_Root,
            PresentationTarget.Stage01Foreground_Root => refs.Stage01Foreground_Root,

            PresentationTarget.Stage02_Root => refs.Stage02_Root,
            PresentationTarget.Stage02BackgroundSystem_Root => refs.Stage02BackgroundSystem_Root,
            PresentationTarget.Stage02BGShot_Root => refs.Stage02BGShot_Root,
            PresentationTarget.Stage02BGContent_Root => refs.Stage02BGContent_Root,
            PresentationTarget.Stage02BGOverlay_Root => refs.Stage02BGOverlay_Root,
            PresentationTarget.Stage02CharacterSystem_Root => refs.Stage02CharacterSystem_Root,
            PresentationTarget.Stage02CharSlot_Root => refs.Stage02CharSlot_Root,
            PresentationTarget.Stage02CharSlotFocus_Root => refs.Stage02CharSlotFocus_Root,
            PresentationTarget.Stage02CharSlotRig_Root => refs.Stage02CharSlotRig_Root,
            PresentationTarget.Stage02Foreground_Root => refs.Stage02Foreground_Root,

            PresentationTarget.DialogueUI_Root => refs.DialogueUI_Root,
            PresentationTarget.DialogueBox_Root => refs.DialogueBox_Root,
            PresentationTarget.NameBox_Root => refs.NameBox_Root,
            PresentationTarget.NarrationBox_Root => refs.NarrationBox_Root,

            PresentationTarget.Choice_Root => refs.Choice_Root,
            PresentationTarget.SystemUI_Root => refs.SystemUI_Root,
            
            PresentationTarget.VerticalStripWipe => refs.VerticalStripWipe,
            PresentationTarget.SlantedShutter => refs.SlantedShutter,

            _ => null
        };
    }
}