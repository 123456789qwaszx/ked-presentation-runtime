using System;
using UnityEngine;

public sealed class PresentationViewAccess
{
    public PresentationViewRefs BuildRefs(PresentationUIRoot root, bool strict = true)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        PresentationViewRefs refs = new PresentationViewRefs
        {
            FullscreenFade_Root = root.ResolveRect(PresentationUIRoot.Refs.FullscreenFade_Root),
            Letterbox_Root = root.ResolveRect(PresentationUIRoot.Refs.Letterbox_Root),
            Flash_Root = root.ResolveRect(PresentationUIRoot.Refs.Flash_Root),
            ScreenOverlay_Root = root.ResolveRect(PresentationUIRoot.Refs.ScreenOverlay_Root),

            StageShot_Root = root.ResolveRect(PresentationUIRoot.Refs.StageShot_Root),
            StagePan_Root = root.ResolveRect(PresentationUIRoot.Refs.StagePan_Root),
            StageZoom_Root = root.ResolveRect(PresentationUIRoot.Refs.StageZoom_Root),
            Stage_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage_Root),

            BackgroundSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.BackgroundSystem_Root),
            BGShot_Root = root.ResolveRect(PresentationUIRoot.Refs.BGShot_Root),
            BGContent_Root = root.ResolveRect(PresentationUIRoot.Refs.BGContent_Root),
            BGOverlay_Root = root.ResolveRect(PresentationUIRoot.Refs.BGOverlay_Root),

            CharacterSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.CharacterSystem_Root),

            CharSlotLeft_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotLeft_Root),
            CharSlotLeftFocus_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotLeftFocus_Root),
            CharSlotLeftRig_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotLeftRig_Root),

            CharSlotCenter_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotCenter_Root),
            CharSlotCenterFocus_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotCenterFocus_Root),
            CharSlotCenterRig_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotCenterRig_Root),

            CharSlotRight_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotRight_Root),
            CharSlotRightFocus_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotRightFocus_Root),
            CharSlotRightRig_Root = root.ResolveRect(PresentationUIRoot.Refs.CharSlotRightRig_Root),

            Foreground_Root = root.ResolveRect(PresentationUIRoot.Refs.Foreground_Root),

            DialogueUI_Root = root.ResolveRect(PresentationUIRoot.Refs.DialogueUI_Root),
            DialogueBox_Root = root.ResolveRect(PresentationUIRoot.Refs.DialogueBox_Root),
            NameBox_Root = root.ResolveRect(PresentationUIRoot.Refs.NameBox_Root),
            NarrationBox_Root = root.ResolveRect(PresentationUIRoot.Refs.NarrationBox_Root),

            Choice_Root = root.ResolveRect(PresentationUIRoot.Refs.Choice_Root),
            SystemUI_Root = root.ResolveRect(PresentationUIRoot.Refs.SystemUI_Root),
        };

        if (strict)
            Validate(refs, root);

        return refs;
    }

    public void Validate(PresentationViewRefs refs, UnityEngine.Object context = null)
    {
        if (refs == null)
            throw new ArgumentNullException(nameof(refs));

        Require(refs.FullscreenFade_Root, nameof(refs.FullscreenFade_Root), context);
        Require(refs.Letterbox_Root, nameof(refs.Letterbox_Root), context);
        Require(refs.Flash_Root, nameof(refs.Flash_Root), context);
        Require(refs.ScreenOverlay_Root, nameof(refs.ScreenOverlay_Root), context);

        Require(refs.StageShot_Root, nameof(refs.StageShot_Root), context);
        Require(refs.StagePan_Root, nameof(refs.StagePan_Root), context);
        Require(refs.StageZoom_Root, nameof(refs.StageZoom_Root), context);
        Require(refs.Stage_Root, nameof(refs.Stage_Root), context);

        Require(refs.BackgroundSystem_Root, nameof(refs.BackgroundSystem_Root), context);
        Require(refs.BGShot_Root, nameof(refs.BGShot_Root), context);
        Require(refs.BGContent_Root, nameof(refs.BGContent_Root), context);
        Require(refs.BGOverlay_Root, nameof(refs.BGOverlay_Root), context);

        Require(refs.CharacterSystem_Root, nameof(refs.CharacterSystem_Root), context);

        Require(refs.CharSlotLeft_Root, nameof(refs.CharSlotLeft_Root), context);
        Require(refs.CharSlotLeftFocus_Root, nameof(refs.CharSlotLeftFocus_Root), context);
        Require(refs.CharSlotLeftRig_Root, nameof(refs.CharSlotLeftRig_Root), context);

        Require(refs.CharSlotCenter_Root, nameof(refs.CharSlotCenter_Root), context);
        Require(refs.CharSlotCenterFocus_Root, nameof(refs.CharSlotCenterFocus_Root), context);
        Require(refs.CharSlotCenterRig_Root, nameof(refs.CharSlotCenterRig_Root), context);

        Require(refs.CharSlotRight_Root, nameof(refs.CharSlotRight_Root), context);
        Require(refs.CharSlotRightFocus_Root, nameof(refs.CharSlotRightFocus_Root), context);
        Require(refs.CharSlotRightRig_Root, nameof(refs.CharSlotRightRig_Root), context);

        Require(refs.Foreground_Root, nameof(refs.Foreground_Root), context);

        Require(refs.DialogueUI_Root, nameof(refs.DialogueUI_Root), context);
        Require(refs.DialogueBox_Root, nameof(refs.DialogueBox_Root), context);
        Require(refs.NameBox_Root, nameof(refs.NameBox_Root), context);
        Require(refs.NarrationBox_Root, nameof(refs.NarrationBox_Root), context);

        Require(refs.Choice_Root, nameof(refs.Choice_Root), context);
        Require(refs.SystemUI_Root, nameof(refs.SystemUI_Root), context);

        ValidateHierarchy(refs, context);
    }

    private void ValidateHierarchy(PresentationViewRefs refs, UnityEngine.Object context)
    {
        RequireChildOf(refs.StagePan_Root, refs.StageShot_Root, nameof(refs.StagePan_Root), nameof(refs.StageShot_Root), context);
        RequireChildOf(refs.StageZoom_Root, refs.StagePan_Root, nameof(refs.StageZoom_Root), nameof(refs.StagePan_Root), context);
        RequireChildOf(refs.Stage_Root, refs.StageZoom_Root, nameof(refs.Stage_Root), nameof(refs.StageZoom_Root), context);

        RequireChildOf(refs.BGShot_Root, refs.BackgroundSystem_Root, nameof(refs.BGShot_Root), nameof(refs.BackgroundSystem_Root), context);
        RequireChildOf(refs.BGContent_Root, refs.BGShot_Root, nameof(refs.BGContent_Root), nameof(refs.BGShot_Root), context);
        RequireChildOf(refs.BGOverlay_Root, refs.BackgroundSystem_Root, nameof(refs.BGOverlay_Root), nameof(refs.BackgroundSystem_Root), context);

        RequireChildOf(refs.CharSlotLeft_Root, refs.CharacterSystem_Root, nameof(refs.CharSlotLeft_Root), nameof(refs.CharacterSystem_Root), context);
        RequireChildOf(refs.CharSlotLeftFocus_Root, refs.CharSlotLeft_Root, nameof(refs.CharSlotLeftFocus_Root), nameof(refs.CharSlotLeft_Root), context);
        RequireChildOf(refs.CharSlotLeftRig_Root, refs.CharSlotLeftFocus_Root, nameof(refs.CharSlotLeftRig_Root), nameof(refs.CharSlotLeftFocus_Root), context);

        RequireChildOf(refs.CharSlotCenter_Root, refs.CharacterSystem_Root, nameof(refs.CharSlotCenter_Root), nameof(refs.CharacterSystem_Root), context);
        RequireChildOf(refs.CharSlotCenterFocus_Root, refs.CharSlotCenter_Root, nameof(refs.CharSlotCenterFocus_Root), nameof(refs.CharSlotCenter_Root), context);
        RequireChildOf(refs.CharSlotCenterRig_Root, refs.CharSlotCenterFocus_Root, nameof(refs.CharSlotCenterRig_Root), nameof(refs.CharSlotCenterFocus_Root), context);

        RequireChildOf(refs.CharSlotRight_Root, refs.CharacterSystem_Root, nameof(refs.CharSlotRight_Root), nameof(refs.CharacterSystem_Root), context);
        RequireChildOf(refs.CharSlotRightFocus_Root, refs.CharSlotRight_Root, nameof(refs.CharSlotRightFocus_Root), nameof(refs.CharSlotRight_Root), context);
        RequireChildOf(refs.CharSlotRightRig_Root, refs.CharSlotRightFocus_Root, nameof(refs.CharSlotRightRig_Root), nameof(refs.CharSlotRightFocus_Root), context);
    }

    private static void Require(UnityEngine.Object obj, string name, UnityEngine.Object context)
    {
        if (obj != null)
            return;

        throw new InvalidOperationException($"[PresentationViewAccess] Missing required ref '{name}'.");
    }

    private static void RequireChildOf(Transform child, Transform expectedParent, string childName, string parentName, UnityEngine.Object context)
    {
        if (child == null || expectedParent == null)
            return;

        if (child.parent == expectedParent)
            return;

        throw new InvalidOperationException(
            $"[PresentationViewAccess] Invalid hierarchy. '{childName}' must be a direct child of '{parentName}'.");
    }
}