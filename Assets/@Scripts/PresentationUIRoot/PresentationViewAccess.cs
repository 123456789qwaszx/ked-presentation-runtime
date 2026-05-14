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
            
            Stage00_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00_Root),
            Stage00BackgroundSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00BackgroundSystem_Root),
            Stage00BGShot_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00BGShot_Root),
            Stage00BGContent_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00BGContent_Root),
            Stage00BGOverlay_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00BGOverlay_Root),
            Stage00CharacterSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00CharacterSystem_Root),
            Stage00CharSlot_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00CharSlot_Root),
            Stage00CharSlotFocus_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00CharSlotFocus_Root),
            Stage00CharSlotRig_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00CharSlotRig_Root),
            Stage00Foreground_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage00Foreground_Root),

            Stage01_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01_Root),
            Stage01BackgroundSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01BackgroundSystem_Root),
            Stage01BGShot_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01BGShot_Root),
            Stage01BGContent_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01BGContent_Root),
            Stage01BGOverlay_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01BGOverlay_Root),
            Stage01CharacterSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01CharacterSystem_Root),
            Stage01CharSlot_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01CharSlot_Root),
            Stage01CharSlotFocus_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01CharSlotFocus_Root),
            Stage01CharSlotRig_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01CharSlotRig_Root),
            Stage01Foreground_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage01Foreground_Root),

            Stage02_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02_Root),
            Stage02BackgroundSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02BackgroundSystem_Root),
            Stage02BGShot_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02BGShot_Root),
            Stage02BGContent_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02BGContent_Root),
            Stage02BGOverlay_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02BGOverlay_Root),
            Stage02CharacterSystem_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02CharacterSystem_Root),
            Stage02CharSlot_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02CharSlot_Root),
            Stage02CharSlotFocus_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02CharSlotFocus_Root),
            Stage02CharSlotRig_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02CharSlotRig_Root),
            Stage02Foreground_Root = root.ResolveRect(PresentationUIRoot.Refs.Stage02Foreground_Root),

            DialogueUI_Root = root.ResolveRect(PresentationUIRoot.Refs.DialogueUI_Root),
            DialogueBox_Root = root.ResolveRect(PresentationUIRoot.Refs.DialogueBox_Root),
            NameBox_Root = root.ResolveRect(PresentationUIRoot.Refs.NameBox_Root),
            NarrationBox_Root = root.ResolveRect(PresentationUIRoot.Refs.NarrationBox_Root),

            Choice_Root = root.ResolveRect(PresentationUIRoot.Refs.Choice_Root),
            SystemUI_Root = root.ResolveRect(PresentationUIRoot.Refs.SystemUI_Root),
            
            VerticalStripWipe = root.ResolveRect(PresentationUIRoot.Refs.VerticalStripWipe)
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

        RequireStage00(refs, context);
        RequireStage01(refs, context);
        RequireStage02(refs, context);

        Require(refs.DialogueUI_Root, nameof(refs.DialogueUI_Root), context);
        Require(refs.DialogueBox_Root, nameof(refs.DialogueBox_Root), context);
        Require(refs.NameBox_Root, nameof(refs.NameBox_Root), context);
        Require(refs.NarrationBox_Root, nameof(refs.NarrationBox_Root), context);

        Require(refs.Choice_Root, nameof(refs.Choice_Root), context);
        Require(refs.SystemUI_Root, nameof(refs.SystemUI_Root), context);

        ValidateHierarchy(refs, context);
    }

    private void RequireStage00(PresentationViewRefs refs, UnityEngine.Object context)
    {
        Require(refs.Stage00_Root, nameof(refs.Stage00_Root), context);

        Require(refs.Stage00BackgroundSystem_Root, nameof(refs.Stage00BackgroundSystem_Root), context);
        Require(refs.Stage00BGShot_Root, nameof(refs.Stage00BGShot_Root), context);
        Require(refs.Stage00BGContent_Root, nameof(refs.Stage00BGContent_Root), context);
        Require(refs.Stage00BGOverlay_Root, nameof(refs.Stage00BGOverlay_Root), context);

        Require(refs.Stage00CharacterSystem_Root, nameof(refs.Stage00CharacterSystem_Root), context);

        Require(refs.Stage00CharSlot_Root, nameof(refs.Stage00CharSlot_Root), context);
        Require(refs.Stage00CharSlotFocus_Root, nameof(refs.Stage00CharSlotFocus_Root), context);
        Require(refs.Stage00CharSlotRig_Root, nameof(refs.Stage00CharSlotRig_Root), context);

        Require(refs.Stage00Foreground_Root, nameof(refs.Stage00Foreground_Root), context);
    }

    private void RequireStage01(PresentationViewRefs refs, UnityEngine.Object context)
    {
        Require(refs.Stage01_Root, nameof(refs.Stage01_Root), context);

        Require(refs.Stage01BackgroundSystem_Root, nameof(refs.Stage01BackgroundSystem_Root), context);
        Require(refs.Stage01BGShot_Root, nameof(refs.Stage01BGShot_Root), context);
        Require(refs.Stage01BGContent_Root, nameof(refs.Stage01BGContent_Root), context);
        Require(refs.Stage01BGOverlay_Root, nameof(refs.Stage01BGOverlay_Root), context);

        Require(refs.Stage01CharacterSystem_Root, nameof(refs.Stage01CharacterSystem_Root), context);

        Require(refs.Stage01CharSlot_Root, nameof(refs.Stage01CharSlot_Root), context);
        Require(refs.Stage01CharSlotFocus_Root, nameof(refs.Stage01CharSlotFocus_Root), context);
        Require(refs.Stage01CharSlotRig_Root, nameof(refs.Stage01CharSlotRig_Root), context);

        Require(refs.Stage01Foreground_Root, nameof(refs.Stage01Foreground_Root), context);
    }

    private void RequireStage02(PresentationViewRefs refs, UnityEngine.Object context)
    {
        Require(refs.Stage02_Root, nameof(refs.Stage02_Root), context);

        Require(refs.Stage02BackgroundSystem_Root, nameof(refs.Stage02BackgroundSystem_Root), context);
        Require(refs.Stage02BGShot_Root, nameof(refs.Stage02BGShot_Root), context);
        Require(refs.Stage02BGContent_Root, nameof(refs.Stage02BGContent_Root), context);
        Require(refs.Stage02BGOverlay_Root, nameof(refs.Stage02BGOverlay_Root), context);

        Require(refs.Stage02CharacterSystem_Root, nameof(refs.Stage02CharacterSystem_Root), context);

        Require(refs.Stage02CharSlot_Root, nameof(refs.Stage02CharSlot_Root), context);
        Require(refs.Stage02CharSlotFocus_Root, nameof(refs.Stage02CharSlotFocus_Root), context);
        Require(refs.Stage02CharSlotRig_Root, nameof(refs.Stage02CharSlotRig_Root), context);

        Require(refs.Stage02Foreground_Root, nameof(refs.Stage02Foreground_Root), context);
    }

    private void ValidateHierarchy(PresentationViewRefs refs, UnityEngine.Object context)
    {
        RequireChildOf(refs.StagePan_Root, refs.StageShot_Root, nameof(refs.StagePan_Root), nameof(refs.StageShot_Root), context);
        RequireChildOf(refs.StageZoom_Root, refs.StagePan_Root, nameof(refs.StageZoom_Root), nameof(refs.StagePan_Root), context);

        ValidateStage00Hierarchy(refs, context);
        ValidateStage01Hierarchy(refs, context);
        ValidateStage02Hierarchy(refs, context);
    }

    private void ValidateStage00Hierarchy(PresentationViewRefs refs, UnityEngine.Object context)
    {
        RequireChildOf(refs.Stage00_Root, refs.StageZoom_Root, nameof(refs.Stage00_Root), nameof(refs.StageZoom_Root), context);

        RequireChildOf(refs.Stage00BackgroundSystem_Root, refs.Stage00_Root, nameof(refs.Stage00BackgroundSystem_Root), nameof(refs.Stage00_Root), context);
        RequireChildOf(refs.Stage00BGShot_Root, refs.Stage00BackgroundSystem_Root, nameof(refs.Stage00BGShot_Root), nameof(refs.Stage00BackgroundSystem_Root), context);
        RequireChildOf(refs.Stage00BGContent_Root, refs.Stage00BGShot_Root, nameof(refs.Stage00BGContent_Root), nameof(refs.Stage00BGShot_Root), context);
        RequireChildOf(refs.Stage00BGOverlay_Root, refs.Stage00BackgroundSystem_Root, nameof(refs.Stage00BGOverlay_Root), nameof(refs.Stage00BackgroundSystem_Root), context);

        RequireChildOf(refs.Stage00CharacterSystem_Root, refs.Stage00_Root, nameof(refs.Stage00CharacterSystem_Root), nameof(refs.Stage00_Root), context);
        RequireChildOf(refs.Stage00CharSlot_Root, refs.Stage00CharacterSystem_Root, nameof(refs.Stage00CharSlot_Root), nameof(refs.Stage00CharacterSystem_Root), context);
        RequireChildOf(refs.Stage00CharSlotFocus_Root, refs.Stage00CharSlot_Root, nameof(refs.Stage00CharSlotFocus_Root), nameof(refs.Stage00CharSlot_Root), context);
        RequireChildOf(refs.Stage00CharSlotRig_Root, refs.Stage00CharSlotFocus_Root, nameof(refs.Stage00CharSlotRig_Root), nameof(refs.Stage00CharSlotFocus_Root), context);

        RequireChildOf(refs.Stage00Foreground_Root, refs.Stage00_Root, nameof(refs.Stage00Foreground_Root), nameof(refs.Stage00_Root), context);
    }

    private void ValidateStage01Hierarchy(PresentationViewRefs refs, UnityEngine.Object context)
    {
        RequireChildOf(refs.Stage01_Root, refs.StageZoom_Root, nameof(refs.Stage01_Root), nameof(refs.StageZoom_Root), context);

        RequireChildOf(refs.Stage01BackgroundSystem_Root, refs.Stage01_Root, nameof(refs.Stage01BackgroundSystem_Root), nameof(refs.Stage01_Root), context);
        RequireChildOf(refs.Stage01BGShot_Root, refs.Stage01BackgroundSystem_Root, nameof(refs.Stage01BGShot_Root), nameof(refs.Stage01BackgroundSystem_Root), context);
        RequireChildOf(refs.Stage01BGContent_Root, refs.Stage01BGShot_Root, nameof(refs.Stage01BGContent_Root), nameof(refs.Stage01BGShot_Root), context);
        RequireChildOf(refs.Stage01BGOverlay_Root, refs.Stage01BackgroundSystem_Root, nameof(refs.Stage01BGOverlay_Root), nameof(refs.Stage01BackgroundSystem_Root), context);

        RequireChildOf(refs.Stage01CharacterSystem_Root, refs.Stage01_Root, nameof(refs.Stage01CharacterSystem_Root), nameof(refs.Stage01_Root), context);
        RequireChildOf(refs.Stage01CharSlot_Root, refs.Stage01CharacterSystem_Root, nameof(refs.Stage01CharSlot_Root), nameof(refs.Stage01CharacterSystem_Root), context);
        RequireChildOf(refs.Stage01CharSlotFocus_Root, refs.Stage01CharSlot_Root, nameof(refs.Stage01CharSlotFocus_Root), nameof(refs.Stage01CharSlot_Root), context);
        RequireChildOf(refs.Stage01CharSlotRig_Root, refs.Stage01CharSlotFocus_Root, nameof(refs.Stage01CharSlotRig_Root), nameof(refs.Stage01CharSlotFocus_Root), context);

        RequireChildOf(refs.Stage01Foreground_Root, refs.Stage01_Root, nameof(refs.Stage01Foreground_Root), nameof(refs.Stage01_Root), context);
    }

    private void ValidateStage02Hierarchy(PresentationViewRefs refs, UnityEngine.Object context)
    {
        RequireChildOf(refs.Stage02_Root, refs.StageZoom_Root, nameof(refs.Stage02_Root), nameof(refs.StageZoom_Root), context);

        RequireChildOf(refs.Stage02BackgroundSystem_Root, refs.Stage02_Root, nameof(refs.Stage02BackgroundSystem_Root), nameof(refs.Stage02_Root), context);
        RequireChildOf(refs.Stage02BGShot_Root, refs.Stage02BackgroundSystem_Root, nameof(refs.Stage02BGShot_Root), nameof(refs.Stage02BackgroundSystem_Root), context);
        RequireChildOf(refs.Stage02BGContent_Root, refs.Stage02BGShot_Root, nameof(refs.Stage02BGContent_Root), nameof(refs.Stage02BGShot_Root), context);
        RequireChildOf(refs.Stage02BGOverlay_Root, refs.Stage02BackgroundSystem_Root, nameof(refs.Stage02BGOverlay_Root), nameof(refs.Stage02BackgroundSystem_Root), context);

        RequireChildOf(refs.Stage02CharacterSystem_Root, refs.Stage02_Root, nameof(refs.Stage02CharacterSystem_Root), nameof(refs.Stage02_Root), context);
        RequireChildOf(refs.Stage02CharSlot_Root, refs.Stage02CharacterSystem_Root, nameof(refs.Stage02CharSlot_Root), nameof(refs.Stage02CharacterSystem_Root), context);
        RequireChildOf(refs.Stage02CharSlotFocus_Root, refs.Stage02CharSlot_Root, nameof(refs.Stage02CharSlotFocus_Root), nameof(refs.Stage02CharSlot_Root), context);
        RequireChildOf(refs.Stage02CharSlotRig_Root, refs.Stage02CharSlotFocus_Root, nameof(refs.Stage02CharSlotRig_Root), nameof(refs.Stage02CharSlotFocus_Root), context);

        RequireChildOf(refs.Stage02Foreground_Root, refs.Stage02_Root, nameof(refs.Stage02Foreground_Root), nameof(refs.Stage02_Root), context);
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