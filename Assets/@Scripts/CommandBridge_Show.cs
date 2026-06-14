using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultShowFaceToken = "e2";
    private const string DefaultShowDurationToken = "14fr";

    private void RegisterShowCommands(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "show_at_left", EnqueueShowAtLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "show_at_center", EnqueueShowAtCenterSpec);

        runner.AddCommandHandler<string, string, string>(
            "show_at_right", EnqueueShowAtRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "show_at_dl", EnqueueShowAtDuoLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "show_at_dr", EnqueueShowAtDuoRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "show", EnqueueShowSpec);
    }

    private void EnqueueShowAtLeftSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
    {
        EnqueueShowAtSpec(
            roleKey,
            faceToken,
            "left",
            durationToken);
    }

    private void EnqueueShowAtCenterSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
    {
        EnqueueShowAtSpec(
            roleKey,
            faceToken,
            "center",
            durationToken);
    }

    private void EnqueueShowAtRightSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
    {
        EnqueueShowAtSpec(
            roleKey,
            faceToken,
            "right",
            durationToken);
    }

    private void EnqueueShowAtDuoLeftSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
    {
        EnqueueShowAtSpec(
            roleKey,
            faceToken,
            "duoleft",
            durationToken);
    }

    private void EnqueueShowAtDuoRightSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
    {
        EnqueueShowAtSpec(
            roleKey,
            faceToken,
            "duoright",
            durationToken);
    }

    private void EnqueueShowSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string positionPreset = "center",
        string durationToken = DefaultShowDurationToken)
    {
        EnqueueShowAtSpec(
            roleKey,
            faceToken,
            positionPreset,
            durationToken);
    }

    private void EnqueueShowAtSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string positionPreset = "center",
        string durationToken = DefaultShowDurationToken)
    {
        float duration = YarnDurationParser.Parse(
            durationToken,
            YarnDurationParser.FramesToSeconds(14f));
        
        string emotionKey = ShowFaceAliasParser.Parse(faceToken);

        EnqueueSetAnchorSpecs(
            roleKey,
            positionPreset,
            resetSlotPos: true,
            resetCharPos: true);

        EnqueueSetPortraitFaceSpec(
            roleKey,
            emotionKey);
        
        var spec = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }
}