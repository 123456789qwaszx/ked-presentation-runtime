public static class EpisodeNodeRigSchema
{
    public enum Refs
    {
        NodeRoot,

        Timeline_Root,
        TimelineBG_Image,
        TimelineEra_Text,
        TimelineCursorIcon_Image,

        SelectZone_Root,
        SelectZoneBG_Image,

        MainCard_Root,
        MainCardBG_Image,
        MainCardIndex_Root,
        MainCardIndexText_Text,
        MainCardIndexIcon_Image,
        MainCardTitle_Root,
        MainCardTitle_Text,
        MainCardHit_Button,

        UpperLink_Root,
        UpperLinkBG_Image,
        UpperLinkTitle_Root,
        UpperLinkTitle_Text,
        UpperLinkHit_Button,

        LowerLink_Root,
        LowerLinkBG_Image,
        LowerLinkTitle_Root,
        LowerLinkTitle_Text,
        LowerLinkHit_Button,

        StateRoot_Selected,
        StateRoot_Current,
        StateRoot_Completed,
        StateRoot_Locked,

        EndingBadge_Root,
        EndingBadge_Text
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsImage;
        public bool NeedsButton;
        public bool NeedsText;
        public bool NeedsCanvasGroup;

        public float InitialCanvasGroupAlpha = 1f;
    }

    public static readonly NodeDef[] Nodes =
    {
        new() { Id = Refs.NodeRoot, Parent = null },

        new() { Id = Refs.Timeline_Root, Parent = Refs.NodeRoot },
        new() { Id = Refs.TimelineBG_Image, Parent = Refs.Timeline_Root, NeedsImage = true },
        new() { Id = Refs.TimelineEra_Text, Parent = Refs.Timeline_Root, NeedsText = true },
        new() { Id = Refs.TimelineCursorIcon_Image, Parent = Refs.Timeline_Root, NeedsImage = true },

        new() { Id = Refs.SelectZone_Root, Parent = Refs.NodeRoot },
        new() { Id = Refs.SelectZoneBG_Image, Parent = Refs.SelectZone_Root, NeedsImage = true },

        new() { Id = Refs.MainCard_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true },
        new() { Id = Refs.MainCardBG_Image, Parent = Refs.MainCard_Root, NeedsImage = true },

        new() { Id = Refs.MainCardIndex_Root, Parent = Refs.MainCard_Root },
        new() { Id = Refs.MainCardIndexText_Text, Parent = Refs.MainCardIndex_Root, NeedsText = true },
        new() { Id = Refs.MainCardIndexIcon_Image, Parent = Refs.MainCardIndex_Root, NeedsImage = true },

        new() { Id = Refs.MainCardTitle_Root, Parent = Refs.MainCard_Root },
        new() { Id = Refs.MainCardTitle_Text, Parent = Refs.MainCardTitle_Root, NeedsText = true },

        new() { Id = Refs.MainCardHit_Button, Parent = Refs.MainCard_Root, NeedsButton = true },

        new() { Id = Refs.UpperLink_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.UpperLinkBG_Image, Parent = Refs.UpperLink_Root, NeedsImage = true },
        new() { Id = Refs.UpperLinkTitle_Root, Parent = Refs.UpperLink_Root },
        new() { Id = Refs.UpperLinkTitle_Text, Parent = Refs.UpperLinkTitle_Root, NeedsText = true },
        new() { Id = Refs.UpperLinkHit_Button, Parent = Refs.UpperLink_Root, NeedsButton = true },

        new() { Id = Refs.LowerLink_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.LowerLinkBG_Image, Parent = Refs.LowerLink_Root, NeedsImage = true },
        new() { Id = Refs.LowerLinkTitle_Root, Parent = Refs.LowerLink_Root },
        new() { Id = Refs.LowerLinkTitle_Text, Parent = Refs.LowerLinkTitle_Root, NeedsText = true },
        new() { Id = Refs.LowerLinkHit_Button, Parent = Refs.LowerLink_Root, NeedsButton = true },

        new() { Id = Refs.StateRoot_Selected, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.StateRoot_Current, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.StateRoot_Completed, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.StateRoot_Locked, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },

        new() { Id = Refs.EndingBadge_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.EndingBadge_Text, Parent = Refs.EndingBadge_Root, NeedsText = true },
    };
}