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

        UpperAttachment_Root,
        UpperAttachmentBG_Image,
        UpperAttachmentTitle_Root,
        UpperAttachmentTitle_Text,
        UpperAttachmentHit_Button,

        LowerAttachment_Root,
        LowerAttachmentBG_Image,
        LowerAttachmentTitle_Root,
        LowerAttachmentTitle_Text,
        LowerAttachmentHit_Button,

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
        public bool NeedsBottomPivot;

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

        new() { Id = Refs.UpperAttachment_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.UpperAttachmentBG_Image, Parent = Refs.UpperAttachment_Root, NeedsImage = true },
        new() { Id = Refs.UpperAttachmentTitle_Root, Parent = Refs.UpperAttachment_Root },
        new() { Id = Refs.UpperAttachmentTitle_Text, Parent = Refs.UpperAttachmentTitle_Root, NeedsText = true },
        new() { Id = Refs.UpperAttachmentHit_Button, Parent = Refs.UpperAttachment_Root, NeedsButton = true },

        new() { Id = Refs.LowerAttachment_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.LowerAttachmentBG_Image, Parent = Refs.LowerAttachment_Root, NeedsImage = true },
        new() { Id = Refs.LowerAttachmentTitle_Root, Parent = Refs.LowerAttachment_Root },
        new() { Id = Refs.LowerAttachmentTitle_Text, Parent = Refs.LowerAttachmentTitle_Root, NeedsText = true },
        new() { Id = Refs.LowerAttachmentHit_Button, Parent = Refs.LowerAttachment_Root, NeedsButton = true },

        new() { Id = Refs.StateRoot_Selected, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.StateRoot_Current, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.StateRoot_Completed, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.StateRoot_Locked, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },

        new() { Id = Refs.EndingBadge_Root, Parent = Refs.NodeRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.EndingBadge_Text, Parent = Refs.EndingBadge_Root, NeedsText = true },
    };
}