public sealed partial class YarnCommandBridge
{
    private const string DefaultShowFaceToken = "e2";
    private const string DefaultShowDurationToken = "14fr";
    
    private void EnqueueShowAtSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string positionPreset = "center",
        string durationToken = DefaultShowDurationToken)
    {
        var spec0SetAnchor = new SetAnchorCommandSpecCharR
        {
            slotKey = roleKey,
            preset = CharAnchorPresetParser.Parse(positionPreset),
            resetSlotPos = true,
            resetCharacterPos = true
        };
        
        var spec1SetPortraitSprite = new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = new PortraitIdentity { emotion = ShowFaceAliasParser.Parse(faceToken) }
        };
        
        var spec2FadeInPortraitSprite = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        };

        Collect(spec0SetAnchor);
        Collect(spec1SetPortraitSprite);
        Collect(spec2FadeInPortraitSprite);
    }

    private void EnqueueShowAtLeftSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "left", durationToken);

    private void EnqueueShowAtCenterSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "center", durationToken);

    private void EnqueueShowAtRightSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "right", durationToken);

    private void EnqueueShowAtDuoLeftSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "duoleft", durationToken);

    private void EnqueueShowAtDuoRightSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "duoright", durationToken);

    private void EnqueueShowSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string positionPreset = "center",
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, positionPreset, durationToken);
}