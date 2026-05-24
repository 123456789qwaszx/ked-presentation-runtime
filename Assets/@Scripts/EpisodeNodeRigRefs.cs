using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeNodeRigRefs
{
    public RectTransform RigRoot { get; private set; }

    public EpisodeNodeRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }

    public RectTransform NodeRoot;

    public RectTransform Timeline_Root;
    public Image TimelineBG_Image;
    public TMP_Text TimelineEra_Text;
    public Image TimelineCursorIcon_Image;

    public RectTransform SelectZone_Root;
    public Image SelectZoneBG_Image;

    public RectTransform MainCard_Root;
    public Image MainCardBG_Image;
    public RectTransform MainCardIndex_Root;
    public TMP_Text MainCardIndexText_Text;
    public Image MainCardIndexIcon_Image;
    public RectTransform MainCardTitle_Root;
    public TMP_Text MainCardTitle_Text;
    public Button MainCardHit_Button;

    public RectTransform UpperAttachment_Root;
    public Image UpperAttachmentBG_Image;
    public RectTransform UpperAttachmentTitle_Root;
    public TMP_Text UpperAttachmentTitle_Text;
    public Button UpperAttachmentHit_Button;

    public RectTransform LowerAttachment_Root;
    public Image LowerAttachmentBG_Image;
    public RectTransform LowerAttachmentTitle_Root;
    public TMP_Text LowerAttachmentTitle_Text;
    public Button LowerAttachmentHit_Button;

    public CanvasGroup StateRoot_Selected;
    public CanvasGroup StateRoot_Current;
    public CanvasGroup StateRoot_Completed;
    public CanvasGroup StateRoot_Locked;

    public CanvasGroup EndingBadge_Root;
    public TMP_Text EndingBadge_Text;
}