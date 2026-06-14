using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultNudgeDurationToken = "8fr";

    private void RegisterDirectionalNudgeCommands(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "left",
            EnqueueNudgeLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "right",
            EnqueueNudgeRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "up",
            EnqueueNudgeUpSpec);

        runner.AddCommandHandler<string, string, string>(
            "down",
            EnqueueNudgeDownSpec);
    }

    private void EnqueueNudgeLeftSpec(string roleKey, string unitToken, string durationToken = DefaultNudgeDurationToken)
    {
        EnqueueDirectionalNudgeSpec(
            roleKey,
            -1f,
            0f,
            unitToken,
            durationToken,
            CharacterRigTarget.CharSlot_Track_X);
    }

    private void EnqueueNudgeRightSpec(string roleKey, string unitToken, string durationToken = DefaultNudgeDurationToken)
    {
        EnqueueDirectionalNudgeSpec(
            roleKey,
            1f,
            0f,
            unitToken,
            durationToken,
            CharacterRigTarget.CharSlot_Track_X);
    }

    private void EnqueueNudgeUpSpec(string roleKey, string unitToken, string durationToken = DefaultNudgeDurationToken)
    {
        EnqueueDirectionalNudgeSpec(
            roleKey,
            0f,
            1f,
            unitToken,
            durationToken,
            CharacterRigTarget.CharSlot_Track_Y);
    }

    private void EnqueueNudgeDownSpec(string roleKey, string unitToken, string durationToken = DefaultNudgeDurationToken)
    {
        EnqueueDirectionalNudgeSpec(
            roleKey,
            0f,
            -1f,
            unitToken,
            durationToken,
            CharacterRigTarget.CharSlot_Track_Y);
    }

    private void EnqueueDirectionalNudgeSpec(
        string roleKey,
        float xSign,
        float ySign,
        string unitToken,
        string durationToken,
        CharacterRigTarget target)
    {
        float pixels = YarnUnitParser.Parse(unitToken);
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = target,
            useAbsolutePosition = false,
            delta = new Vector2(pixels * xSign, pixels * ySign),
            duration = duration,
            ease = Ease.OutCubic
        };

        Collect(spec);
    }
}