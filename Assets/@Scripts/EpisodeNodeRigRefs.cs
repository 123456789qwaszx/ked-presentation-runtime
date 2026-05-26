using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeNodeRigRefs
{
    public readonly RectTransform RigRoot;

    public RectTransform MainCard_Root;
    public Image MainCardBG_Image;

    public RectTransform MainCardIndex_Root;
    public TMP_Text MainCardIndexText_Text;
    public Image MainCardIndexIcon_Image;

    public RectTransform MainCardTitle_Root;
    public TMP_Text MainCardTitle_Text;

    public Button MainCardHit_Button;

    public EpisodeNodeRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }
}