using System;

public enum VNStoryNodeKind
{
    Main,
    Attachment
}

public enum VNStoryAttachmentSlot
{
    Up,
    Right,
    Down
}

public enum VNStoryAttachmentKind
{
    None,
    BadEnding,
    IfRoute,
    SideEpisode,
    Bonus,
    Secret,
    ExtraEnding
}

public enum VNStoryEndingKind
{
    None,
    ChapterClear,
    NormalEnding,
    BadEnding,
    IfEnding,
    TrueEnding,
    HiddenEnding
}

public enum VNStoryValidationSeverity
{
    Info,
    Warning,
    Error
}