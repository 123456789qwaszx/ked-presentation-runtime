using UnityEngine;
using UnityEngine.UI;

public static class ScreenEffectRigSchema
{
    public enum Refs
    {
        ScreenOverlay_Root,

        Cover_Image,
        Vignette_Image,
        Noise_Image,
        Flash_Image,
    }

    public enum ControllerKind
    {
        None,
        Cover,
        Vignette,
        Noise,
        Flash,
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsImage;
        public bool StretchFull = true;

        public ControllerKind Controller;
        public string MaterialResourcesPath;

        public Color InitialImageColor = Color.white;
        public bool RaycastTarget = false;
    }

    public static readonly NodeDef[] Nodes =
    {
        new()
        {
            Id = Refs.ScreenOverlay_Root,
            Parent = null,
        },

        // Node array order is the layer order under the same parent.
        // Bottom -> top.

        // 덮개 — 무대를 가리는 뚜껑이라 화면 효과의 맨 아래다. 비네트 · 노이즈 · 플래시는 덮인 화면 위에도 얹힌다
        // (docs/PLAN_screen-cover.md §2.1 · D2). 단색이라 전용 머티리얼이 없다.
        new()
        {
            Id = Refs.Cover_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Cover,
            InitialImageColor = new Color(0f, 0f, 0f, 0f),
            RaycastTarget = false,
        },

        new()
        {
            Id = Refs.Vignette_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Vignette,
            MaterialResourcesPath = "VisualEffects/M_UIScreenVignette",
            RaycastTarget = false,
        },

        new()
        {
            Id = Refs.Noise_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Noise,
            MaterialResourcesPath = "VisualEffects/M_UIScreenNoise",
            RaycastTarget = false,
        },

        new()
        {
            Id = Refs.Flash_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Flash,
            MaterialResourcesPath = "VisualEffects/M_UIScreenFlash",
            RaycastTarget = false,
        },
    };
}

public sealed class ScreenEffectRigRefs
{
    public RectTransform RigRoot { get; }

    public RectTransform ScreenOverlay_Root;

    public Image Cover_Image;
    public Image Vignette_Image;
    public Image Noise_Image;
    public Image Flash_Image;

    public ScreenCoverEffectController Cover;
    public ScreenVignetteEffectController Vignette;
    public ScreenNoiseEffectController Noise;
    public ScreenFlashEffectController Flash;

    public ScreenEffectRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }
}