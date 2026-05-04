using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class SequenceTextImporter
{
    private readonly YarnCommandBridge _bridge;
    private readonly RecipeTextParser _parser = new();

    public SequenceTextImporter(YarnCommandBridge bridge)
    {
        _bridge = bridge;
    }

    public ImportResult ImportToSequence(
        string text,
        SequenceSpecSO target,
        bool replaceCurrentNodes)
    {
        if (_bridge == null)
            throw new InvalidOperationException("YarnCommandBridge is null.");

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        var result = new ImportResult();
        List<RecipeCommandLine> lines = _parser.Parse(text);
        result.parsedLineCount = lines.Count;

        var sink = new SequenceImportSink();
        _bridge.Import_SetSink(sink);

        try
        {
            for (int i = 0; i < lines.Count; i++)
            {
                RecipeCommandLine line = lines[i];

                try
                {
                    if (Dispatch(line, result, sink))
                        result.importedCommandCount++;
                }
                catch (Exception e)
                {
                    result.errors.Add(
                        $"Line {line.lineNumber}: {line.rawText}\n{e.Message}");
                }
            }

            // end_hold를 빼먹은 경우도 마지막에 정리
            sink.EndHold();

            ApplyImportedSteps(target, sink.Steps, replaceCurrentNodes);
        }
        finally
        {
            _bridge.Import_ClearSink();
        }

        return result;
    }

    private bool Dispatch(
        RecipeCommandLine line,
        ImportResult result,
        SequenceImportSink sink)
    {
        switch (line.commandName)
        {
            // ------------------------------------------------------------
            // meta commands
            // ------------------------------------------------------------

            case "begin_hold":
                sink.BeginHold();
                return false;

            case "end_hold":
                sink.EndHold();
                return false;

            case "step_label":
                RequireArgs(line, 1);
                sink.SetStepLabel(line.args[0]);
                return false;

            case "gate":
                ApplyGate(line, sink);
                return false;

            // ------------------------------------------------------------
            // importable commands
            // ------------------------------------------------------------

            case "slot":
                RequireArgs(line, 1);
                _bridge.Import_Slot(line.args[0]);
                return true;

            case "slot_boxside":
                RequireArgs(line, 1);
                _bridge.Import_SlotBoxside(line.args[0]);
                return true;

            case "place":
                RequireArgs(line, 2);
                _bridge.Import_Place(line.args[0], line.args[1]);
                return true;

            case "place_offset":
                RequireArgs(line, 3);
                _bridge.Import_PlaceOffset(
                    line.args[0],
                    ParseInt(line.args[1], "x"),
                    ParseInt(line.args[2], "y"));
                return true;

            case "size":
                RequireArgs(line, 2);
                _bridge.Import_Size(
                    line.args[0],
                    line.args[1]);
                return true;

            case "to_scale":
                RequireArgs(line, 2);
                _bridge.Import_ToScale(
                    line.args[0],
                    ParseFloat(line.args[1], "scale"));
                return true;

            case "cast":
                RequireArgs(line, 2);
                _bridge.Import_Cast(line.args[0], line.args[1]);
                return true;

            case "uncast":
                RequireArgs(line, 1);
                _bridge.Import_Uncast(line.args[0]);
                return true;

            case "slide_in":
                RequireArgs(line, 1);
                _bridge.Import_SlideIn(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "left");
                return true;

            case "slide_out":
                RequireArgs(line, 1);
                _bridge.Import_SlideOut(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "right");
                return true;

            case "fade_in":
                RequireArgs(line, 1);
                _bridge.Import_FadeIn(line.args[0]);
                return true;

            case "fade_out":
                RequireArgs(line, 1);
                _bridge.Import_FadeOut(line.args[0]);
                return true;

            case "move_by":
                RequireArgs(line, 3);
                _bridge.Import_MoveBy(
                    line.args[0],
                    ParseFloat(line.args[1], "x"),
                    ParseFloat(line.args[2], "y"));
                return true;

            case "dip":
                RequireArgs(line, 1);
                _bridge.Import_Dip(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "down");
                return true;

            case "hop_in":
                RequireArgs(line, 1);

                _bridge.Import_HopIn(
                    line.args[0],
                    line.args.Count >= 2 ? ParseFloat(line.args[1], "distance") : 80f,
                    line.args.Count >= 3 ? line.args[2] : "left");

                return true;
            case "jolt":
                RequireArgs(line, 1);
                _bridge.Import_Jolt(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "right");
                return true;

            case "shake":
                RequireArgs(line, 1);
                _bridge.Import_Shake(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "right");
                return true;

            case "nudge":
                RequireArgs(line, 1);
                _bridge.Import_Nudge(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "right");
                return true;

            case "nudge_hard":
                RequireArgs(line, 1);
                _bridge.Import_NudgeHard(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "down");
                return true;

            case "slide_in_nudge":
                RequireArgs(line, 1);
                _bridge.Import_SlideInNudge(
                    line.args[0],
                    line.args.Count >= 2 ? line.args[1] : "right");
                return true;

            case "sway":
                RequireArgs(line, 1);
                _bridge.Import_Sway(line.args[0]);
                return true;

            case "sway_hard":
                RequireArgs(line, 1);
                _bridge.Import_SwayHard(line.args[0]);
                return true;

            case "sway_fast":
                RequireArgs(line, 1);
                _bridge.Import_SwayFast(line.args[0]);
                return true;

            case "sway_away":
                RequireArgs(line, 1);
                _bridge.Import_SwayAway(line.args[0]);
                return true;

            case "sway_to":
                RequireArgs(line, 2);
                _bridge.Import_SwayTo(
                    line.args[0],
                    ParseInt(line.args[1], "angle"));
                return true;

            case "slide_in_sway":
                RequireArgs(line, 1);
                _bridge.Import_SlideInSway(line.args[0]);
                return true;

            case "portrait_cross":
                RequireArgs(line, 2);
                _bridge.Import_PortraitCross(line.args[0], line.args[1]);
                return true;

            case "portrait_swap":
                RequireArgs(line, 2);
                _bridge.Import_PortraitSwap(line.args[0], line.args[1]);
                return true;

            case "emotion_wipe":
                RequireArgs(line, 4);
                _bridge.Import_EmotionWipe(
                    line.args[0],
                    line.args[1],
                    line.args[2],
                    line.args[3]);
                return true;

            case "blackout":
                RequireArgs(line, 1);
                _bridge.Import_Blackout(line.args[0]);
                return true;

            case "uipatch":
                RequireArgs(line, 1);
                _bridge.Import_UIPatch(line.args[0]);
                return true;

            case "bgm":
                RequireArgs(line, 1);
                _bridge.Import_Bgm(
                    line.args[0],
                    line.args.Count >= 2 ? ParseFloat(line.args[1], "fadeDuration") : 1f);
                return true;

            case "stop_bgm":
                _bridge.Import_StopBgm(
                    line.args.Count >= 1 ? ParseFloat(line.args[0], "fadeDuration") : 1f);
                return true;

            case "voice":
                RequireArgs(line, 1);
                _bridge.Import_Voice(line.args[0]);
                return true;

            case "stop_voice":
                _bridge.Import_StopVoice();
                return true;

            case "sfx":
                RequireArgs(line, 1);
                _bridge.Import_Sfx(line.args[0]);
                return true;

            case "stop_all_sfx":
                _bridge.Import_StopAllSfx();
                return true;

            case "destroy":
                RequireArgs(line, 1);
                _bridge.Import_Destroy(line.args[0]);
                return true;

            case "emoji":
                RequireArgs(line, 2);
                _bridge.Import_Emoji(
                    line.args[0],
                    line.args[1]);
                return true;

            case "emoji_hide":
                RequireArgs(line, 1);
                _bridge.Import_EmojiHide(line.args[0]);
                return true;
            
            default:
                result.warnings.Add(
                    $"Line {line.lineNumber}: unsupported command '{line.commandName}'");
                return false;
        }
    }

    private static void ApplyGate(RecipeCommandLine line, SequenceImportSink sink)
    {
        RequireArgs(line, 1);

        string mode = line.args[0].ToLowerInvariant();

        switch (mode)
        {
            case "immediately":
            case "none":
                sink.SetGate(GateToken.Immediately());
                return;

            case "input":
                sink.SetGate(GateToken.Input());
                return;

            case "delay":
                if (line.args.Count < 2)
                {
                    throw new InvalidOperationException(
                        "Command 'gate delay' requires seconds.");
                }

                sink.SetGate(GateToken.Delay(ParseFloat(line.args[1], "seconds")));
                return;

            case "signal":
                if (line.args.Count < 2)
                {
                    throw new InvalidOperationException(
                        "Command 'gate signal' requires signalKey.");
                }

                sink.SetGate(GateToken.Signal(line.args[1]));
                return;

            default:
                throw new InvalidOperationException(
                    $"Unsupported gate mode '{mode}'.");
        }
    }

    private void ApplyImportedSteps(
        SequenceSpecSO target,
        IReadOnlyList<ImportedStepDraft> importedSteps,
        bool replaceCurrentNodes)
    {
        if (replaceCurrentNodes)
            target.nodes.Clear();

        var node = new NodeSpec
        {
            editorName = $"Imported Node {target.nodes.Count}"
        };

        for (int i = 0; i < importedSteps.Count; i++)
        {
            ImportedStepDraft src = importedSteps[i];

            var step = new StepSpec
            {
                editorName = string.IsNullOrWhiteSpace(src.editorName)
                    ? $"Imported Step {i}"
                    : src.editorName,
                gate = src.gate,
                compiled = new List<CommandSpecBase>(src.commands)
            };

            node.steps.Add(step);
        }

        target.nodes.Add(node);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(target);
#endif
    }

    private static void RequireArgs(RecipeCommandLine line, int count)
    {
        if (line.args.Count < count)
        {
            throw new InvalidOperationException(
                $"Command '{line.commandName}' requires at least {count} args.");
        }
    }

    private static float ParseFloat(string raw, string fieldName)
    {
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            throw new InvalidOperationException(
                $"Failed to parse float '{raw}' for {fieldName}.");
        }

        return value;
    }

    private static int ParseInt(string raw, string fieldName)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException(
                $"Failed to parse int '{raw}' for {fieldName}.");
        }

        return value;
    }
}