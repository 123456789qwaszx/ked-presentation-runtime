using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeNodeRigRefs
{
    public RectTransform RigRoot { get; }

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

    public RectTransform UpperLink_Root;
    public Image UpperLinkBG_Image;
    public RectTransform UpperLinkTitle_Root;
    public TMP_Text UpperLinkTitle_Text;
    public Button UpperLinkHit_Button;

    public RectTransform LowerLink_Root;
    public Image LowerLinkBG_Image;
    public RectTransform LowerLinkTitle_Root;
    public TMP_Text LowerLinkTitle_Text;
    public Button LowerLinkHit_Button;

    public CanvasGroup StateRoot_Selected;
    public CanvasGroup StateRoot_Current;
    public CanvasGroup StateRoot_Completed;
    public CanvasGroup StateRoot_Locked;

    public CanvasGroup EndingBadge_Root;
    public TMP_Text EndingBadge_Text;
}