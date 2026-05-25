using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ChapterButtonCardSchema
{
    public enum Refs
    {
        // Root
        Card_Root,

        // Size / motion / command axes
        Card_LayoutRoot,
        Card_MotionRoot,
        Card_ShakeRoot,
        Card_ScaleRoot,

        // Background
        Bg_Root,
        Bg_Pad,
        Bg_Image,

        BgOverlay_Root,
        BgOverlay_Pad,
        BgOverlay_Image,

        // Index
        Index_Root,
        Index_Anchor,
        Index_Text,

        // Heading block
        HeadingBlock_Root,

        ChapterIndexLabel_Root,
        ChapterIndexLabel_Image,
        ChapterIndexLabel_Text,

        ChapterTitleLabel_Root,
        ChapterTitleLabelBG_Image,
        ChapterTitleLabelIcon_Image,
        ChapterTitleLabel_Text,

        EpisodeHeadingLabel_Root,
        EpisodeHeadingLabel_Image,
        EpisodeHeadingLabel_Text,

        // Interaction
        Hit_Root,
        Hit_Button,

        // State visuals
        Selected_Root,
        Locked_Root,

        // Extension
        ExtensionsRoot
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsImage;
        public bool NeedsButton;
        public bool NeedsCanvasGroup;
        public bool NeedsText;

        public bool NeedsCenterPivot;
        public bool NeedsTopLeftPivot;
        public bool NeedsBottomPivot;

        public float InitialCanvasGroupAlpha = 1f;
    }

    public static readonly NodeDef[] Nodes =
    {
        // Root
        new() { Id = Refs.Card_Root, Parent = null, NeedsCanvasGroup = true },

        // Command / animation axis
        new() { Id = Refs.Card_LayoutRoot, Parent = Refs.Card_Root },
        new() { Id = Refs.Card_MotionRoot, Parent = Refs.Card_LayoutRoot },
        new() { Id = Refs.Card_ShakeRoot, Parent = Refs.Card_MotionRoot },
        new() { Id = Refs.Card_ScaleRoot, Parent = Refs.Card_ShakeRoot },

        // Background
        new() { Id = Refs.Bg_Root, Parent = Refs.Card_ScaleRoot },
        new() { Id = Refs.Bg_Pad, Parent = Refs.Bg_Root },
        new() { Id = Refs.Bg_Image, Parent = Refs.Bg_Pad, NeedsImage = true },

        new() { Id = Refs.BgOverlay_Root, Parent = Refs.Card_ScaleRoot },
        new() { Id = Refs.BgOverlay_Pad, Parent = Refs.BgOverlay_Root },
        new() { Id = Refs.BgOverlay_Image, Parent = Refs.BgOverlay_Pad, NeedsImage = true },

        // Index
        new() { Id = Refs.Index_Root, Parent = Refs.Card_ScaleRoot },
        new() { Id = Refs.Index_Anchor, Parent = Refs.Index_Root, NeedsTopLeftPivot = true },
        new() { Id = Refs.Index_Text, Parent = Refs.Index_Anchor, NeedsText = true },

        // Heading block
        new() { Id = Refs.HeadingBlock_Root, Parent = Refs.Card_ScaleRoot },

        new() { Id = Refs.ChapterIndexLabel_Root, Parent = Refs.HeadingBlock_Root },
        new() { Id = Refs.ChapterIndexLabel_Image, Parent = Refs.ChapterIndexLabel_Root, NeedsImage = true },
        new() { Id = Refs.ChapterIndexLabel_Text, Parent = Refs.ChapterIndexLabel_Root, NeedsText = true },

        new() { Id = Refs.ChapterTitleLabel_Root, Parent = Refs.HeadingBlock_Root },
        new() { Id = Refs.ChapterTitleLabelBG_Image, Parent = Refs.ChapterTitleLabel_Root, NeedsImage = true },
        new() { Id = Refs.ChapterTitleLabelIcon_Image, Parent = Refs.ChapterTitleLabel_Root, NeedsImage = true },
        new() { Id = Refs.ChapterTitleLabel_Text, Parent = Refs.ChapterTitleLabel_Root, NeedsText = true },

        new() { Id = Refs.EpisodeHeadingLabel_Root, Parent = Refs.HeadingBlock_Root },
        new() { Id = Refs.EpisodeHeadingLabel_Image, Parent = Refs.EpisodeHeadingLabel_Root, NeedsImage = true },
        new() { Id = Refs.EpisodeHeadingLabel_Text, Parent = Refs.EpisodeHeadingLabel_Root, NeedsText = true },

        // Interaction
        new() { Id = Refs.Hit_Root, Parent = Refs.Card_Root },
        new() { Id = Refs.Hit_Button, Parent = Refs.Hit_Root, NeedsButton = true },

        // State visuals
        new() { Id = Refs.Selected_Root, Parent = Refs.Card_Root, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.Locked_Root, Parent = Refs.Card_Root, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },

        // Extension
        new() { Id = Refs.ExtensionsRoot, Parent = Refs.Card_Root },
    };
}


public sealed class ChapterButtonCardRefs
{
    public RectTransform RigRoot { get; private set; }

    public ChapterButtonCardRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }

    // Root
    public RectTransform Card_Root;
    public CanvasGroup Card_Root_CanvasGroup;

    // Size / motion / command axes
    public RectTransform Card_LayoutRoot;
    public RectTransform Card_MotionRoot;
    public RectTransform Card_ShakeRoot;
    public RectTransform Card_ScaleRoot;

    // Background
    public RectTransform Bg_Root;
    public RectTransform Bg_Pad;
    public Image Bg_Image;

    public RectTransform BgOverlay_Root;
    public RectTransform BgOverlay_Pad;
    public Image BgOverlay_Image;

    // Index
    public RectTransform Index_Root;
    public RectTransform Index_Anchor;
    public TMP_Text Index_Text;

    // Heading block
    public RectTransform HeadingBlock_Root;

    public RectTransform ChapterIndexLabel_Root;
    public Image ChapterIndexLabel_Image;
    public TMP_Text ChapterIndexLabel_Text;

    public RectTransform ChapterTitleLabel_Root;
    public Image ChapterTitleLabelBG_Image;
    public Image ChapterTitleLabelIcon_Image;
    public TMP_Text ChapterTitleLabel_Text;

    public RectTransform EpisodeHeadingLabel_Root;
    public Image EpisodeHeadingLabel_Image;
    public TMP_Text EpisodeHeadingLabel_Text;

    // Interaction
    public RectTransform Hit_Root;
    public Button Hit_Button;

    // State visuals
    public RectTransform Selected_Root;
    public CanvasGroup Selected_Root_CanvasGroup;

    public RectTransform Locked_Root;
    public CanvasGroup Locked_Root_CanvasGroup;

    // Extension
    public RectTransform ExtensionsRoot;
}

public enum ChapterButtonCardTarget
{
    Card_Root,
    Card_LayoutRoot,
    Card_MotionRoot,
    Card_ShakeRoot,
    Card_ScaleRoot,

    Bg_Root,
    Bg_Pad,
    Bg_Image,

    BgOverlay_Root,
    BgOverlay_Pad,
    BgOverlay_Image,

    Index_Root,
    Index_Anchor,
    Index_Text,

    HeadingBlock_Root,

    ChapterIndexLabel_Root,
    ChapterIndexLabel_Image,
    ChapterIndexLabel_Text,

    ChapterTitleLabel_Root,
    ChapterTitleLabelBG_Image,
    ChapterTitleLabelIcon_Image,
    ChapterTitleLabel_Text,

    EpisodeHeadingLabel_Root,
    EpisodeHeadingLabel_Image,
    EpisodeHeadingLabel_Text,

    Hit_Root,
    Hit_Button,

    Selected_Root,
    Locked_Root,

    ExtensionsRoot
}

public static class ChapterButtonCardRefsExtensions
{
    public static RectTransform GetRect(this ChapterButtonCardRefs refs, ChapterButtonCardTarget target)
    {
        Component component = refs.GetComponent(target);

        if (component == null)
            return null;

        return component.transform as RectTransform;
    }

    public static Image GetImage(this ChapterButtonCardRefs refs, ChapterButtonCardTarget target)
    {
        return refs.GetComponent(target) as Image;
    }

    public static TMP_Text GetText(this ChapterButtonCardRefs refs, ChapterButtonCardTarget target)
    {
        return refs.GetComponent(target) as TMP_Text;
    }

    public static Button GetButton(this ChapterButtonCardRefs refs, ChapterButtonCardTarget target)
    {
        return refs.GetComponent(target) as Button;
    }

    public static CanvasGroup GetCanvasGroup(this ChapterButtonCardRefs refs, ChapterButtonCardTarget target)
    {
        return refs.GetComponent(target) as CanvasGroup;
    }

    private static Component GetComponent(this ChapterButtonCardRefs refs, ChapterButtonCardTarget target)
    {
        if (refs == null)
            return null;

        return target switch
        {
            ChapterButtonCardTarget.Card_Root => refs.Card_Root,
            ChapterButtonCardTarget.Card_LayoutRoot => refs.Card_LayoutRoot,
            ChapterButtonCardTarget.Card_MotionRoot => refs.Card_MotionRoot,
            ChapterButtonCardTarget.Card_ShakeRoot => refs.Card_ShakeRoot,
            ChapterButtonCardTarget.Card_ScaleRoot => refs.Card_ScaleRoot,

            ChapterButtonCardTarget.Bg_Root => refs.Bg_Root,
            ChapterButtonCardTarget.Bg_Pad => refs.Bg_Pad,
            ChapterButtonCardTarget.Bg_Image => refs.Bg_Image,

            ChapterButtonCardTarget.BgOverlay_Root => refs.BgOverlay_Root,
            ChapterButtonCardTarget.BgOverlay_Pad => refs.BgOverlay_Pad,
            ChapterButtonCardTarget.BgOverlay_Image => refs.BgOverlay_Image,

            ChapterButtonCardTarget.Index_Root => refs.Index_Root,
            ChapterButtonCardTarget.Index_Anchor => refs.Index_Anchor,
            ChapterButtonCardTarget.Index_Text => refs.Index_Text,

            ChapterButtonCardTarget.HeadingBlock_Root => refs.HeadingBlock_Root,

            ChapterButtonCardTarget.ChapterIndexLabel_Root => refs.ChapterIndexLabel_Root,
            ChapterButtonCardTarget.ChapterIndexLabel_Image => refs.ChapterIndexLabel_Image,
            ChapterButtonCardTarget.ChapterIndexLabel_Text => refs.ChapterIndexLabel_Text,

            ChapterButtonCardTarget.ChapterTitleLabel_Root => refs.ChapterTitleLabel_Root,
            ChapterButtonCardTarget.ChapterTitleLabelBG_Image => refs.ChapterTitleLabelBG_Image,
            ChapterButtonCardTarget.ChapterTitleLabelIcon_Image => refs.ChapterTitleLabelIcon_Image,
            ChapterButtonCardTarget.ChapterTitleLabel_Text => refs.ChapterTitleLabel_Text,

            ChapterButtonCardTarget.EpisodeHeadingLabel_Root => refs.EpisodeHeadingLabel_Root,
            ChapterButtonCardTarget.EpisodeHeadingLabel_Image => refs.EpisodeHeadingLabel_Image,
            ChapterButtonCardTarget.EpisodeHeadingLabel_Text => refs.EpisodeHeadingLabel_Text,

            ChapterButtonCardTarget.Hit_Root => refs.Hit_Root,
            ChapterButtonCardTarget.Hit_Button => refs.Hit_Button,

            ChapterButtonCardTarget.Selected_Root => refs.Selected_Root,
            ChapterButtonCardTarget.Locked_Root => refs.Locked_Root,

            ChapterButtonCardTarget.ExtensionsRoot => refs.ExtensionsRoot,

            _ => null
        };
    }
}