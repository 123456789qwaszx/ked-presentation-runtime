using UnityEngine;
using UnityEngine.UI;

public interface IDialogueBoxViewResolver
{
    IDialogueTextTarget Activate(DialogueBoxKind kind);
    void HideAll();
}

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
    
    [SerializeField] private DialogueBoxHost dialogueBoxHost;
    public IDialogueBoxViewPrefabProvider DialogueBoxPrefabs => dialogueBoxHost;
    
    public RectTransform ResolveRect(Refs key) => View.Rect(key);
    public CanvasGroup ResolveCanvasGroup(Refs key) => View.CanvasGroup(key);
    public Image ResolveImage(Refs key) => View.Image(key);
    
}