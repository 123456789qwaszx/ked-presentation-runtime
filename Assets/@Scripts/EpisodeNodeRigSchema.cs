using UnityEngine;

public static class EpisodeNodeRigSchema
{
    public enum Refs
    {
        MainCard_Root,
        MainCardBG_Image,
        MainCardIndex_Root,
        MainCardIndexText_Text,
        MainCardIndexIcon_Image,
        MainCardTitle_Root,
        MainCardTitle_Text,
        MainCardHit_Button,
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
        new() { Id = Refs.MainCard_Root, Parent = null, NeedsCanvasGroup = true },
        new() { Id = Refs.MainCardBG_Image, Parent = Refs.MainCard_Root, NeedsImage = true },

        new() { Id = Refs.MainCardIndex_Root, Parent = Refs.MainCard_Root },
        new() { Id = Refs.MainCardIndexText_Text, Parent = Refs.MainCardIndex_Root, NeedsText = true },
        new() { Id = Refs.MainCardIndexIcon_Image, Parent = Refs.MainCardIndex_Root, NeedsImage = true },

        new() { Id = Refs.MainCardTitle_Root, Parent = Refs.MainCard_Root },
        new() { Id = Refs.MainCardTitle_Text, Parent = Refs.MainCardTitle_Root, NeedsText = true },

        new() { Id = Refs.MainCardHit_Button, Parent = Refs.MainCard_Root, NeedsButton = true },
    };
}