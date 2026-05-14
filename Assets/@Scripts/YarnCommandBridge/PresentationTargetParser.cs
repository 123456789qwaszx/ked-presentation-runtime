using System;

public static class PresentationTargetParser
{
    public static bool TryParse(string raw, out PresentationTarget target)
    {
        target = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = Normalize(raw);

        if (TryParseGlobalTarget(s, out target))
            return true;

        if (TryParseStageTarget(s, out target))
            return true;

        if (TryParseDialogueTarget(s, out target))
            return true;

        return Enum.TryParse(raw.Trim(), true, out target);
    }

    public static bool TryParseStageRoot(string raw, out PresentationTarget target)
    {
        target = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = Normalize(raw);

        switch (s)
        {
            case "stage":
            case "stage_root":
            case "s0":
            case "0":
            case "stage0":
            case "stage00":
            case "stage0_root":
            case "stage00_root":
            case "stage00root":
            case "s0_root":
                target = PresentationTarget.Stage00_Root;
                return true;

            case "s1":
            case "1":
            case "stage1":
            case "stage01":
            case "stage1_root":
            case "stage01_root":
            case "stage01root":
            case "s1_root":
                target = PresentationTarget.Stage01_Root;
                return true;

            case "s2":
            case "2":
            case "stage2":
            case "stage02":
            case "stage2_root":
            case "stage02_root":
            case "stage02root":
            case "s2_root":
                target = PresentationTarget.Stage02_Root;
                return true;
        }

        return false;
    }

    public static bool TryParseBackgroundStageContent(string raw, out PresentationTarget target)
    {
        target = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = Normalize(raw);

        switch (s)
        {
            case "s0":
            case "0":
            case "stage0":
            case "stage00":
            case "s0_bgcontent":
            case "s0_bgcontent_root":
            case "s0_bg_content":
            case "s0_bg_content_root":
            case "stage0_bgcontent":
            case "stage0_bgcontent_root":
            case "stage00_bgcontent":
            case "stage00_bgcontent_root":
            case "stage0_bg_content":
            case "stage00_bg_content":
                target = PresentationTarget.Stage00BGContent_Root;
                return true;

            case "s1":
            case "1":
            case "stage1":
            case "stage01":
            case "s1_bgcontent":
            case "s1_bgcontent_root":
            case "s1_bg_content":
            case "s1_bg_content_root":
            case "stage1_bgcontent":
            case "stage1_bgcontent_root":
            case "stage01_bgcontent":
            case "stage01_bgcontent_root":
            case "stage1_bg_content":
            case "stage01_bg_content":
                target = PresentationTarget.Stage01BGContent_Root;
                return true;

            case "s2":
            case "2":
            case "stage2":
            case "stage02":
            case "s2_bgcontent":
            case "s2_bgcontent_root":
            case "s2_bg_content":
            case "s2_bg_content_root":
            case "stage2_bgcontent":
            case "stage2_bgcontent_root":
            case "stage02_bgcontent":
            case "stage02_bgcontent_root":
            case "stage2_bg_content":
            case "stage02_bg_content":
                target = PresentationTarget.Stage02BGContent_Root;
                return true;
        }

        return false;
    }

    private static bool TryParseGlobalTarget(string s, out PresentationTarget target)
    {
        target = default;

        switch (s)
        {
            case "lightsweep":
            case "LightSweep":
            case "ls":
                target = PresentationTarget.LightSweep;
                return true;
            
            case "focusblurcurtain":
            case "FocusBlurCurtain":
            case "fbc":
                target = PresentationTarget.FocusBlurCurtain;
                return true;
            
            case "focusblurfade":
            case "FocusBlurFade":
            case "fbf":
                target = PresentationTarget.FocusBlurFade;
                return true;
            
            case "transition":
            case "SlantedShutter":
            case "slantedshutter":
            case "ss":
                target = PresentationTarget.SlantedShutter;
                return true;
            
            case "VerticalStripWipe":
            case "verticalstripwipe":
            case "vsw":
                target = PresentationTarget.VerticalStripWipe;
                return true;
            
            case "fullscreenfade":
            case "fullscreenfade_root":
            case "fullscreen_fade":
            case "fullscreen_fade_root":
            case "fade":
            case "black":
                target = PresentationTarget.FullscreenFade_Root;
                return true;

            case "letterbox":
            case "letterbox_root":
                target = PresentationTarget.Letterbox_Root;
                return true;

            case "flash":
            case "flash_root":
                target = PresentationTarget.Flash_Root;
                return true;

            case "screenoverlay":
            case "screenoverlay_root":
            case "screen_overlay":
            case "screen_overlay_root":
            case "overlay":
                target = PresentationTarget.ScreenOverlay_Root;
                return true;

            case "stageshot":
            case "stageshot_root":
            case "stage_shot":
            case "stage_shot_root":
                target = PresentationTarget.StageShot_Root;
                return true;

            case "stagepan":
            case "stagepan_root":
            case "stage_pan":
            case "stage_pan_root":
            case "pan":
                target = PresentationTarget.StagePan_Root;
                return true;

            case "stagezoom":
            case "stagezoom_root":
            case "stage_zoom":
            case "stage_zoom_root":
            case "zoom":
                target = PresentationTarget.StageZoom_Root;
                return true;
        }

        return false;
    }

    private static bool TryParseStageTarget(string s, out PresentationTarget target)
    {
        target = default;

        switch (s)
        {
            // Stage00
            case "stage":
            case "stage_root":
            case "s0":
            case "0":
            case "stage0":
            case "stage00":
            case "stage0_root":
            case "stage00_root":
            case "stage00root":
            case "s0_root":
                target = PresentationTarget.Stage00_Root;
                return true;

            case "backgroundsystem":
            case "backgroundsystem_root":
            case "background_system":
            case "background_system_root":
            case "s0_backgroundsystem":
            case "s0_backgroundsystem_root":
            case "s0_background_system":
            case "s0_background_system_root":
            case "stage0_backgroundsystem":
            case "stage0_backgroundsystem_root":
            case "stage00_backgroundsystem":
            case "stage00_backgroundsystem_root":
            case "stage0_background_system":
            case "stage00_background_system":
                target = PresentationTarget.Stage00BackgroundSystem_Root;
                return true;

            case "bgshot":
            case "bgshot_root":
            case "bg":
            case "s0_bgshot":
            case "s0_bgshot_root":
            case "s0_bg":
            case "s0_bg_root":
            case "stage0_bgshot":
            case "stage0_bgshot_root":
            case "stage00_bgshot":
            case "stage00_bgshot_root":
            case "stage0_bg":
            case "stage00_bg":
                target = PresentationTarget.Stage00BGShot_Root;
                return true;

            case "bgcontent":
            case "bgcontent_root":
            case "bg_content":
            case "bg_content_root":
            case "s0_bgcontent":
            case "s0_bgcontent_root":
            case "s0_bg_content":
            case "s0_bg_content_root":
            case "stage0_bgcontent":
            case "stage0_bgcontent_root":
            case "stage00_bgcontent":
            case "stage00_bgcontent_root":
            case "stage0_bg_content":
            case "stage00_bg_content":
                target = PresentationTarget.Stage00BGContent_Root;
                return true;

            case "bgoverlay":
            case "bgoverlay_root":
            case "bg_overlay":
            case "bg_overlay_root":
            case "s0_bgoverlay":
            case "s0_bgoverlay_root":
            case "s0_bg_overlay":
            case "s0_bg_overlay_root":
            case "stage0_bgoverlay":
            case "stage0_bgoverlay_root":
            case "stage00_bgoverlay":
            case "stage00_bgoverlay_root":
            case "stage0_bg_overlay":
            case "stage00_bg_overlay":
                target = PresentationTarget.Stage00BGOverlay_Root;
                return true;

            case "charactersystem":
            case "charactersystem_root":
            case "character_system":
            case "character_system_root":
            case "s0_charactersystem":
            case "s0_charactersystem_root":
            case "s0_character_system":
            case "s0_character_system_root":
            case "stage0_charactersystem":
            case "stage0_charactersystem_root":
            case "stage00_charactersystem":
            case "stage00_charactersystem_root":
            case "stage0_character_system":
            case "stage00_character_system":
                target = PresentationTarget.Stage00CharacterSystem_Root;
                return true;

            case "charslot":
            case "charslot_root":
            case "char_slot":
            case "char_slot_root":
            case "s0_charslot":
            case "s0_charslot_root":
            case "s0_char_slot":
            case "s0_char_slot_root":
            case "stage0_charslot":
            case "stage0_charslot_root":
            case "stage00_charslot":
            case "stage00_charslot_root":
            case "stage0_char_slot":
            case "stage00_char_slot":
                target = PresentationTarget.Stage00CharSlot_Root;
                return true;

            case "charslotfocus":
            case "charslotfocus_root":
            case "char_slot_focus":
            case "char_slot_focus_root":
            case "s0_charslotfocus":
            case "s0_charslotfocus_root":
            case "s0_char_slot_focus":
            case "s0_char_slot_focus_root":
            case "stage0_charslotfocus":
            case "stage0_charslotfocus_root":
            case "stage00_charslotfocus":
            case "stage00_charslotfocus_root":
            case "stage0_char_slot_focus":
            case "stage00_char_slot_focus":
                target = PresentationTarget.Stage00CharSlotFocus_Root;
                return true;

            case "charslotrig":
            case "charslotrig_root":
            case "char_slot_rig":
            case "char_slot_rig_root":
            case "s0_charslotrig":
            case "s0_charslotrig_root":
            case "s0_char_slot_rig":
            case "s0_char_slot_rig_root":
            case "stage0_charslotrig":
            case "stage0_charslotrig_root":
            case "stage00_charslotrig":
            case "stage00_charslotrig_root":
            case "stage0_char_slot_rig":
            case "stage00_char_slot_rig":
                target = PresentationTarget.Stage00CharSlotRig_Root;
                return true;

            case "foreground":
            case "foreground_root":
            case "fg":
            case "fg_root":
            case "s0_foreground":
            case "s0_foreground_root":
            case "s0_fg":
            case "s0_fg_root":
            case "stage0_foreground":
            case "stage0_foreground_root":
            case "stage00_foreground":
            case "stage00_foreground_root":
            case "stage0_fg":
            case "stage00_fg":
                target = PresentationTarget.Stage00Foreground_Root;
                return true;

            // Stage01
            case "s1":
            case "1":
            case "stage1":
            case "stage01":
            case "stage1_root":
            case "stage01_root":
            case "stage01root":
            case "s1_root":
                target = PresentationTarget.Stage01_Root;
                return true;

            case "s1_backgroundsystem":
            case "s1_backgroundsystem_root":
            case "s1_background_system":
            case "s1_background_system_root":
            case "stage1_backgroundsystem":
            case "stage1_backgroundsystem_root":
            case "stage01_backgroundsystem":
            case "stage01_backgroundsystem_root":
            case "stage1_background_system":
            case "stage01_background_system":
                target = PresentationTarget.Stage01BackgroundSystem_Root;
                return true;

            case "s1_bgshot":
            case "s1_bgshot_root":
            case "s1_bg":
            case "s1_bg_root":
            case "stage1_bgshot":
            case "stage1_bgshot_root":
            case "stage01_bgshot":
            case "stage01_bgshot_root":
            case "stage1_bg":
            case "stage01_bg":
                target = PresentationTarget.Stage01BGShot_Root;
                return true;

            case "s1_bgcontent":
            case "s1_bgcontent_root":
            case "s1_bg_content":
            case "s1_bg_content_root":
            case "stage1_bgcontent":
            case "stage1_bgcontent_root":
            case "stage01_bgcontent":
            case "stage01_bgcontent_root":
            case "stage1_bg_content":
            case "stage01_bg_content":
                target = PresentationTarget.Stage01BGContent_Root;
                return true;

            case "s1_bgoverlay":
            case "s1_bgoverlay_root":
            case "s1_bg_overlay":
            case "s1_bg_overlay_root":
            case "stage1_bgoverlay":
            case "stage1_bgoverlay_root":
            case "stage01_bgoverlay":
            case "stage01_bgoverlay_root":
            case "stage1_bg_overlay":
            case "stage01_bg_overlay":
                target = PresentationTarget.Stage01BGOverlay_Root;
                return true;

            case "s1_charactersystem":
            case "s1_charactersystem_root":
            case "s1_character_system":
            case "s1_character_system_root":
            case "stage1_charactersystem":
            case "stage1_charactersystem_root":
            case "stage01_charactersystem":
            case "stage01_charactersystem_root":
            case "stage1_character_system":
            case "stage01_character_system":
                target = PresentationTarget.Stage01CharacterSystem_Root;
                return true;

            case "s1_charslot":
            case "s1_charslot_root":
            case "s1_char_slot":
            case "s1_char_slot_root":
            case "stage1_charslot":
            case "stage1_charslot_root":
            case "stage01_charslot":
            case "stage01_charslot_root":
            case "stage1_char_slot":
            case "stage01_char_slot":
                target = PresentationTarget.Stage01CharSlot_Root;
                return true;

            case "s1_charslotfocus":
            case "s1_charslotfocus_root":
            case "s1_char_slot_focus":
            case "s1_char_slot_focus_root":
            case "stage1_charslotfocus":
            case "stage1_charslotfocus_root":
            case "stage01_charslotfocus":
            case "stage01_charslotfocus_root":
            case "stage1_char_slot_focus":
            case "stage01_char_slot_focus":
                target = PresentationTarget.Stage01CharSlotFocus_Root;
                return true;

            case "s1_charslotrig":
            case "s1_charslotrig_root":
            case "s1_char_slot_rig":
            case "s1_char_slot_rig_root":
            case "stage1_charslotrig":
            case "stage1_charslotrig_root":
            case "stage01_charslotrig":
            case "stage01_charslotrig_root":
            case "stage1_char_slot_rig":
            case "stage01_char_slot_rig":
                target = PresentationTarget.Stage01CharSlotRig_Root;
                return true;

            case "s1_foreground":
            case "s1_foreground_root":
            case "s1_fg":
            case "s1_fg_root":
            case "stage1_foreground":
            case "stage1_foreground_root":
            case "stage01_foreground":
            case "stage01_foreground_root":
            case "stage1_fg":
            case "stage01_fg":
                target = PresentationTarget.Stage01Foreground_Root;
                return true;

            // Stage02
            case "s2":
            case "2":
            case "stage2":
            case "stage02":
            case "stage2_root":
            case "stage02_root":
            case "stage02root":
            case "s2_root":
                target = PresentationTarget.Stage02_Root;
                return true;

            case "s2_backgroundsystem":
            case "s2_backgroundsystem_root":
            case "s2_background_system":
            case "s2_background_system_root":
            case "stage2_backgroundsystem":
            case "stage2_backgroundsystem_root":
            case "stage02_backgroundsystem":
            case "stage02_backgroundsystem_root":
            case "stage2_background_system":
            case "stage02_background_system":
                target = PresentationTarget.Stage02BackgroundSystem_Root;
                return true;

            case "s2_bgshot":
            case "s2_bgshot_root":
            case "s2_bg":
            case "s2_bg_root":
            case "stage2_bgshot":
            case "stage2_bgshot_root":
            case "stage02_bgshot":
            case "stage02_bgshot_root":
            case "stage2_bg":
            case "stage02_bg":
                target = PresentationTarget.Stage02BGShot_Root;
                return true;

            case "s2_bgcontent":
            case "s2_bgcontent_root":
            case "s2_bg_content":
            case "s2_bg_content_root":
            case "stage2_bgcontent":
            case "stage2_bgcontent_root":
            case "stage02_bgcontent":
            case "stage02_bgcontent_root":
            case "stage2_bg_content":
            case "stage02_bg_content":
                target = PresentationTarget.Stage02BGContent_Root;
                return true;

            case "s2_bgoverlay":
            case "s2_bgoverlay_root":
            case "s2_bg_overlay":
            case "s2_bg_overlay_root":
            case "stage2_bgoverlay":
            case "stage2_bgoverlay_root":
            case "stage02_bgoverlay":
            case "stage02_bgoverlay_root":
            case "stage2_bg_overlay":
            case "stage02_bg_overlay":
                target = PresentationTarget.Stage02BGOverlay_Root;
                return true;

            case "s2_charactersystem":
            case "s2_charactersystem_root":
            case "s2_character_system":
            case "s2_character_system_root":
            case "stage2_charactersystem":
            case "stage2_charactersystem_root":
            case "stage02_charactersystem":
            case "stage02_charactersystem_root":
            case "stage2_character_system":
            case "stage02_character_system":
                target = PresentationTarget.Stage02CharacterSystem_Root;
                return true;

            case "s2_charslot":
            case "s2_charslot_root":
            case "s2_char_slot":
            case "s2_char_slot_root":
            case "stage2_charslot":
            case "stage2_charslot_root":
            case "stage02_charslot":
            case "stage02_charslot_root":
            case "stage2_char_slot":
            case "stage02_char_slot":
                target = PresentationTarget.Stage02CharSlot_Root;
                return true;

            case "s2_charslotfocus":
            case "s2_charslotfocus_root":
            case "s2_char_slot_focus":
            case "s2_char_slot_focus_root":
            case "stage2_charslotfocus":
            case "stage2_charslotfocus_root":
            case "stage02_charslotfocus":
            case "stage02_charslotfocus_root":
            case "stage2_char_slot_focus":
            case "stage02_char_slot_focus":
                target = PresentationTarget.Stage02CharSlotFocus_Root;
                return true;

            case "s2_charslotrig":
            case "s2_charslotrig_root":
            case "s2_char_slot_rig":
            case "s2_char_slot_rig_root":
            case "stage2_charslotrig":
            case "stage2_charslotrig_root":
            case "stage02_charslotrig":
            case "stage02_charslotrig_root":
            case "stage2_char_slot_rig":
            case "stage02_char_slot_rig":
                target = PresentationTarget.Stage02CharSlotRig_Root;
                return true;

            case "s2_foreground":
            case "s2_foreground_root":
            case "s2_fg":
            case "s2_fg_root":
            case "stage2_foreground":
            case "stage2_foreground_root":
            case "stage02_foreground":
            case "stage02_foreground_root":
            case "stage2_fg":
            case "stage02_fg":
                target = PresentationTarget.Stage02Foreground_Root;
                return true;
        }

        return false;
    }

    private static bool TryParseDialogueTarget(string s, out PresentationTarget target)
    {
        target = default;

        switch (s)
        {
            case "dialogueui":
            case "dialogueui_root":
            case "dialogue_ui":
            case "dialogue_ui_root":
                target = PresentationTarget.DialogueUI_Root;
                return true;

            case "dialoguebox":
            case "dialoguebox_root":
            case "dialogue_box":
            case "dialogue_box_root":
                target = PresentationTarget.DialogueBox_Root;
                return true;

            case "namebox":
            case "namebox_root":
            case "name_box":
            case "name_box_root":
                target = PresentationTarget.NameBox_Root;
                return true;

            case "narrationbox":
            case "narrationbox_root":
            case "narration_box":
            case "narration_box_root":
                target = PresentationTarget.NarrationBox_Root;
                return true;

            case "choice":
            case "choice_root":
                target = PresentationTarget.Choice_Root;
                return true;

            case "systemui":
            case "systemui_root":
            case "system_ui":
            case "system_ui_root":
                target = PresentationTarget.SystemUI_Root;
                return true;
        }

        return false;
    }

    private static string Normalize(string raw)
    {
        string s = raw.Trim().ToLowerInvariant();
        s = s.Replace("-", "_");
        s = s.Replace(".", "_");
        return s;
    }
}