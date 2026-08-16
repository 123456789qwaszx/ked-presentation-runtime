// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public enum StageOverlayRigRootKind
// {
//     Sprite = 0,
//     Text = 1,
// }
//
// public static class OverlayRigSchema
// {
//     public enum Refs
//     {
//         Overlay_Root,
//
//         Overlay_Anchor,
//
//         // Screen-space / overlay-space track.
//         // This is intentionally above BaseRotation, so it is not affected by
//         // the rotated movement basis below.
//         Overlay_Track,
//
//         // BaseRotation defines the local basis for Track_Move / Track_X / Track_Y.
//         // Commands that should move along a rotated basis should target Track_Move
//         // or the axis nodes below it.
//         Overlay_BaseRotation,
//
//         Overlay_Track_Move,
//         Overlay_Track_X,
//         Overlay_Track_X_Offset,
//         Overlay_Track_Y,
//         Overlay_Track_Y_Offset,
//
//         // Final visual rotation.
//         // This does not affect Track_Move / Track_X / Track_Y movement direction.
//         Overlay_Rotation,
//
//         Overlay_Size,
//         Overlay_Scale,
//
//         Overlay_ActingScale,
//         Overlay_ActingScale_X,
//         Overlay_ActingScale_Y,
//
//         Overlay_Content,
//
//         Overlay_ImageBox,
//         Overlay_ImagePad,
//         Overlay_Image,
//
//         Overlay_TextBox,
//         Overlay_TextPad,
//         Overlay_Text,
//     }
//
//     public sealed class NodeDef
//     {
//         public Refs Id;
//         public Refs? Parent;
//
//         public bool NeedsImage;
//         public bool NeedsText;
//         public bool NeedsCanvasGroup;
//
//         public bool StretchFull;
//         public bool NeedsCenterPivot = true;
//         public bool NeedsBottomPivot;
//
//         public float InitialCanvasGroupAlpha = 1f;
//         public Color InitialGraphicColor = Color.white;
//         public bool RaycastTarget = false;
//     }
//
//     public static readonly NodeDef[] Nodes =
//     {
//         new()
//         {
//             Id = Refs.Overlay_Root,
//             Parent = null,
//             StretchFull = true,
//             NeedsCanvasGroup = true,
//             InitialCanvasGroupAlpha = 0f,
//         },
//
//         // Point-based screen overlay spine.
//         // Root is full screen, but from Anchor to Rotation these nodes behave
//         // like lightweight transform points.
//         new() { Id = Refs.Overlay_Anchor, Parent = Refs.Overlay_Root },
//
//         // Screen-space / overlay-space track.
//         // Unaffected by BaseRotation.
//         new() { Id = Refs.Overlay_Track, Parent = Refs.Overlay_Anchor },
//
//         // Rotated movement basis root.
//         new() { Id = Refs.Overlay_BaseRotation, Parent = Refs.Overlay_Track },
//
//         new() { Id = Refs.Overlay_Track_Move, Parent = Refs.Overlay_BaseRotation },
//         new() { Id = Refs.Overlay_Track_X, Parent = Refs.Overlay_Track_Move },
//         new() { Id = Refs.Overlay_Track_X_Offset, Parent = Refs.Overlay_Track_X },
//         new() { Id = Refs.Overlay_Track_Y, Parent = Refs.Overlay_Track_X_Offset },
//         new() { Id = Refs.Overlay_Track_Y_Offset, Parent = Refs.Overlay_Track_Y },
//
//         // Final visual rotation.
//         // This does not affect Track_Move / Track_X / Track_Y movement direction.
//         new() { Id = Refs.Overlay_Rotation, Parent = Refs.Overlay_Track_Y_Offset },
//
//         // Explicit content rect.
//         // This is not StretchFull. Commands or setup operations decide its size.
//         new() { Id = Refs.Overlay_Size, Parent = Refs.Overlay_Rotation },
//
//         // From here downward, the rig should inherit Overlay_Size's rect.
//         new()
//         {
//             Id = Refs.Overlay_Scale,
//             Parent = Refs.Overlay_Size,
//             StretchFull = true,
//         },
//
//         new()
//         {
//             Id = Refs.Overlay_ActingScale,
//             Parent = Refs.Overlay_Scale,
//             StretchFull = true,
//         },
//
//         new()
//         {
//             Id = Refs.Overlay_ActingScale_X,
//             Parent = Refs.Overlay_ActingScale,
//             StretchFull = true,
//         },
//
//         new()
//         {
//             Id = Refs.Overlay_ActingScale_Y,
//             Parent = Refs.Overlay_ActingScale_X,
//             StretchFull = true,
//         },
//
//         new()
//         {
//             Id = Refs.Overlay_Content,
//             Parent = Refs.Overlay_ActingScale_Y,
//             StretchFull = true,
//         },
//
//         new()
//         {
//             Id = Refs.Overlay_ImageBox,
//             Parent = Refs.Overlay_Content,
//             StretchFull = true,
//         },
//         new()
//         {
//             Id = Refs.Overlay_ImagePad,
//             Parent = Refs.Overlay_ImageBox,
//             StretchFull = true,
//         },
//         new()
//         {
//             Id = Refs.Overlay_Image,
//             Parent = Refs.Overlay_ImagePad,
//             StretchFull = true,
//             NeedsImage = true,
//             InitialGraphicColor = Color.white,
//             RaycastTarget = false,
//         },
//
//         new()
//         {
//             Id = Refs.Overlay_TextBox,
//             Parent = Refs.Overlay_Content,
//             StretchFull = true,
//         },
//         new()
//         {
//             Id = Refs.Overlay_TextPad,
//             Parent = Refs.Overlay_TextBox,
//             StretchFull = true,
//         },
//         new()
//         {
//             Id = Refs.Overlay_Text,
//             Parent = Refs.Overlay_TextPad,
//             StretchFull = true,
//             NeedsText = true,
//             InitialGraphicColor = Color.white,
//             RaycastTarget = false,
//         },
//     };
// }
//
// public enum OverlayRigTarget
// {
//     Overlay_Root,
//
//     Overlay_Anchor,
//
//     // Screen-space / overlay-space track, unaffected by Overlay_BaseRotation.
//     Overlay_Track,
//
//     // Rotates the local movement basis below.
//     Overlay_BaseRotation,
//
//     Overlay_Track_Move,
//     Overlay_Track_X,
//     Overlay_Track_X_Offset,
//     Overlay_Track_Y,
//     Overlay_Track_Y_Offset,
//
//     Overlay_Rotation,
//
//     Overlay_Size,
//     Overlay_Scale,
//
//     Overlay_ActingScale,
//     Overlay_ActingScale_X,
//     Overlay_ActingScale_Y,
//
//     Overlay_Content,
//
//     Overlay_ImageBox,
//     Overlay_ImagePad,
//     Overlay_Image,
//
//     Overlay_TextBox,
//     Overlay_TextPad,
//     Overlay_Text,
// }
//
// public sealed class OverlayRigRefs
// {
//     public RectTransform RigRoot { get; }
//
//     public CanvasGroup Overlay_RootCanvasGroup;
//     public RectTransform Overlay_Root;
//
//     public RectTransform Overlay_Anchor;
//
//     public RectTransform Overlay_Track;
//
//     public RectTransform Overlay_BaseRotation;
//
//     public RectTransform Overlay_Track_Move;
//     public RectTransform Overlay_Track_X;
//     public RectTransform Overlay_Track_X_Offset;
//     public RectTransform Overlay_Track_Y;
//     public RectTransform Overlay_Track_Y_Offset;
//
//     public RectTransform Overlay_Rotation;
//
//     public RectTransform Overlay_Size;
//     public RectTransform Overlay_Scale;
//
//     public RectTransform Overlay_ActingScale;
//     public RectTransform Overlay_ActingScale_X;
//     public RectTransform Overlay_ActingScale_Y;
//
//     public RectTransform Overlay_Content;
//
//     public RectTransform Overlay_ImageBox;
//     public RectTransform Overlay_ImagePad;
//     public Image Overlay_Image;
//
//     public RectTransform Overlay_TextBox;
//     public RectTransform Overlay_TextPad;
//     public TextMeshProUGUI Overlay_Text;
//
//     public OverlayRigRefs(RectTransform rigRoot)
//     {
//         RigRoot = rigRoot;
//     }
// }
//
// public static class OverlayRigRefsExtensions
// {
//     public static RectTransform GetRect(
//         this OverlayRigRefs refs,
//         OverlayRigTarget target)
//     {
//         Component component = refs.GetComponent(target);
//         return component != null ? component.transform as RectTransform : null;
//     }
//
//     public static Image GetImage(
//         this OverlayRigRefs refs,
//         OverlayRigTarget target)
//     {
//         return refs.GetComponent(target) as Image;
//     }
//
//     public static TextMeshProUGUI GetText(
//         this OverlayRigRefs refs,
//         OverlayRigTarget target)
//     {
//         return refs.GetComponent(target) as TextMeshProUGUI;
//     }
//
//     public static Graphic GetGraphic(
//         this OverlayRigRefs refs,
//         OverlayRigTarget target)
//     {
//         return refs.GetComponent(target) as Graphic;
//     }
//
//     private static Component GetComponent(
//         this OverlayRigRefs refs,
//         OverlayRigTarget target)
//     {
//         if (refs == null)
//             return null;
//
//         return target switch
//         {
//             OverlayRigTarget.Overlay_Root => refs.Overlay_Root,
//
//             OverlayRigTarget.Overlay_Anchor => refs.Overlay_Anchor,
//
//             OverlayRigTarget.Overlay_Track => refs.Overlay_Track,
//
//             OverlayRigTarget.Overlay_BaseRotation => refs.Overlay_BaseRotation,
//
//             OverlayRigTarget.Overlay_Track_Move => refs.Overlay_Track_Move,
//             OverlayRigTarget.Overlay_Track_X => refs.Overlay_Track_X,
//             OverlayRigTarget.Overlay_Track_X_Offset => refs.Overlay_Track_X_Offset,
//             OverlayRigTarget.Overlay_Track_Y => refs.Overlay_Track_Y,
//             OverlayRigTarget.Overlay_Track_Y_Offset => refs.Overlay_Track_Y_Offset,
//
//             OverlayRigTarget.Overlay_Rotation => refs.Overlay_Rotation,
//
//             OverlayRigTarget.Overlay_Size => refs.Overlay_Size,
//             OverlayRigTarget.Overlay_Scale => refs.Overlay_Scale,
//
//             OverlayRigTarget.Overlay_ActingScale => refs.Overlay_ActingScale,
//             OverlayRigTarget.Overlay_ActingScale_X => refs.Overlay_ActingScale_X,
//             OverlayRigTarget.Overlay_ActingScale_Y => refs.Overlay_ActingScale_Y,
//
//             OverlayRigTarget.Overlay_Content => refs.Overlay_Content,
//
//             OverlayRigTarget.Overlay_ImageBox => refs.Overlay_ImageBox,
//             OverlayRigTarget.Overlay_ImagePad => refs.Overlay_ImagePad,
//             OverlayRigTarget.Overlay_Image => refs.Overlay_Image,
//
//             OverlayRigTarget.Overlay_TextBox => refs.Overlay_TextBox,
//             OverlayRigTarget.Overlay_TextPad => refs.Overlay_TextPad,
//             OverlayRigTarget.Overlay_Text => refs.Overlay_Text,
//
//             _ => null,
//         };
//     }
// }