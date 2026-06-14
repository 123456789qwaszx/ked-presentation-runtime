using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultShowDurationToken = "14fr";

    private void RegisterShowCommands(DialogueRunner runner)
    {
        RegisterShowAtCommand(runner, "show_at_l", "left");
        RegisterShowAtCommand(runner, "show_at_c", "center");
        RegisterShowAtCommand(runner, "show_at_r", "right");

        RegisterShowAtCommand(runner, "show_at_dl", "duoleft");
        RegisterShowAtCommand(runner, "show_at_dr", "duoright");
        
        runner.AddCommandHandler<string, string, string, string>("show", EnqueueShowSpec);
    }

    private void RegisterShowAtCommand(
        DialogueRunner runner,
        string commandName,
        string positionPreset)
    {
        runner.AddCommandHandler<string, string, string>(
            commandName,
            (roleKey, faceToken, durationToken) =>
            {
                EnqueueShowAtSpec(
                    roleKey,
                    positionPreset,
                    faceToken,
                    durationToken);
            });
    }

    private void EnqueueShowAtSpec(
        string roleKey,
        string positionPreset,
        string faceToken = "face2",
        string durationToken = DefaultShowDurationToken)
    {
        string emotionKey = ShowFaceAliasParser.Parse(faceToken);

        float duration = YarnDurationParser.Parse(
            durationToken,
            YarnDurationParser.FramesToSeconds(14f));

        EnqueueSetAnchorSpecs(
            roleKey,
            positionPreset,
            resetSlotPos: true,
            resetCharPos: true);

        EnqueueSetPortraitFaceSpec(
            roleKey,
            emotionKey);

        EnqueueFadeInSpec(
            roleKey,
            duration);
    }

    private void EnqueueShowSpec(
        string roleKey,
        string positionToken = "center",
        string faceToken = "face2",
        string durationToken = DefaultShowDurationToken)
    {
        string positionPreset = ShowPositionAliasParser.Parse(positionToken);
        string emotionKey = ShowFaceAliasParser.Parse(faceToken);
        float duration = YarnDurationParser.Parse(
            durationToken,
            YarnDurationParser.FramesToSeconds(14f));

        EnqueueSetAnchorSpecs(
            roleKey,
            positionPreset,
            resetSlotPos: true,
            resetCharPos: true);

        EnqueueSetPortraitFaceSpec(
            roleKey,
            emotionKey);

        EnqueueFadeInSpec(
            roleKey,
            duration);
    }
}