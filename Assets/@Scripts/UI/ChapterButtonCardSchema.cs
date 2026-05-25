using System;
using UnityEngine;

public static class ChapterButtonCardSchema
{
    public enum Node
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

    [Serializable]
    public sealed class NodeDef
    {
        public Node Id;
        public Node? Parent;

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
        new() { Id = Node.Card_Root, Parent = null, NeedsCanvasGroup = true },

        new() { Id = Node.Card_LayoutRoot, Parent = Node.Card_Root },
        new() { Id = Node.Card_MotionRoot, Parent = Node.Card_LayoutRoot },
        new() { Id = Node.Card_ShakeRoot, Parent = Node.Card_MotionRoot },
        new() { Id = Node.Card_ScaleRoot, Parent = Node.Card_ShakeRoot },

        new() { Id = Node.Bg_Root, Parent = Node.Card_ScaleRoot },
        new() { Id = Node.Bg_Pad, Parent = Node.Bg_Root },
        new() { Id = Node.Bg_Image, Parent = Node.Bg_Pad, NeedsImage = true },

        new() { Id = Node.BgOverlay_Root, Parent = Node.Card_ScaleRoot },
        new() { Id = Node.BgOverlay_Pad, Parent = Node.BgOverlay_Root },
        new() { Id = Node.BgOverlay_Image, Parent = Node.BgOverlay_Pad, NeedsImage = true },

        new() { Id = Node.Index_Root, Parent = Node.Card_ScaleRoot },
        new() { Id = Node.Index_Anchor, Parent = Node.Index_Root, NeedsTopLeftPivot = true },
        new() { Id = Node.Index_Text, Parent = Node.Index_Anchor, NeedsText = true },

        new() { Id = Node.HeadingBlock_Root, Parent = Node.Card_ScaleRoot },

        new() { Id = Node.ChapterIndexLabel_Root, Parent = Node.HeadingBlock_Root },
        new() { Id = Node.ChapterIndexLabel_Image, Parent = Node.ChapterIndexLabel_Root, NeedsImage = true },
        new() { Id = Node.ChapterIndexLabel_Text, Parent = Node.ChapterIndexLabel_Root, NeedsText = true },

        new() { Id = Node.ChapterTitleLabel_Root, Parent = Node.HeadingBlock_Root },
        new() { Id = Node.ChapterTitleLabelBG_Image, Parent = Node.ChapterTitleLabel_Root, NeedsImage = true },
        new() { Id = Node.ChapterTitleLabelIcon_Image, Parent = Node.ChapterTitleLabel_Root, NeedsImage = true },
        new() { Id = Node.ChapterTitleLabel_Text, Parent = Node.ChapterTitleLabel_Root, NeedsText = true },

        new() { Id = Node.EpisodeHeadingLabel_Root, Parent = Node.HeadingBlock_Root },
        new() { Id = Node.EpisodeHeadingLabel_Image, Parent = Node.EpisodeHeadingLabel_Root, NeedsImage = true },
        new() { Id = Node.EpisodeHeadingLabel_Text, Parent = Node.EpisodeHeadingLabel_Root, NeedsText = true },

        new() { Id = Node.Hit_Root, Parent = Node.Card_Root },
        new() { Id = Node.Hit_Button, Parent = Node.Hit_Root, NeedsButton = true },

        new() { Id = Node.Selected_Root, Parent = Node.Card_Root, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Node.Locked_Root, Parent = Node.Card_Root, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },

        new() { Id = Node.ExtensionsRoot, Parent = Node.Card_Root },
    };
}