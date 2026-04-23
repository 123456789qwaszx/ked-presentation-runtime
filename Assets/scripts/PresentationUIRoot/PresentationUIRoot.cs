using UnityEngine;
using UnityEngine.UI;

public class PresentationUIRoot : UIRoot<PresentationUIRoot.Refs>
{
    public enum Refs
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
        CharSlotLeft_Root,
        CharSlotLeftFocus_Root,
        CharSlotLeftRig_Root,

        CharSlotCenter_Root,
        CharSlotCenterFocus_Root,
        CharSlotCenterRig_Root,

        CharSlotRight_Root,
        CharSlotRightFocus_Root,
        CharSlotRightRig_Root,

        Foreground_Root,

        DialogueUI_Root,
        DialogueBox_Root,
        NameBox_Root,
        NarrationBox_Root,

        Choice_Root,
        SystemUI_Root
    }

    public RectTransform ResolveRect(Refs key)
    {
        return View.Rect(key);
    }

    public CanvasGroup ResolveCanvasGroup(Refs key)
    {
        return View.CanvasGroup(key);
    }

    public Image ResolveImage(Refs key)
    {
        return View.Image(key);
    }
    
}