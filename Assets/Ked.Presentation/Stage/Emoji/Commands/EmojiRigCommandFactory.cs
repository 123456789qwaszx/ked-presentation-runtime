using System;

public sealed class EmojiRigCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly EmojiRigBuilder _builder;
    private readonly EmojiLibrary _library;
    private readonly EmojiPlaybackSystem _playback;
    private readonly EmojiLocatorSystem _locators;

    /// <param name="locators">로케이터의 자리를 푸는 곳 — 캐릭터 + 포즈별 표와 기본값을 든다.</param>
    public EmojiRigCommandFactory(
        PresentationStage stage,
        EmojiRigBuilder builder,
        EmojiLibrary library,
        EmojiPlaybackSystem playback,
        EmojiLocatorSystem locators)
    {
        _playback = playback ?? throw new ArgumentNullException(nameof(playback));
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _library = library ?? EmojiLibrary.Empty;
        _locators = locators ?? throw new ArgumentNullException(nameof(locators));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            SetEmojiCommandSpec s => new SetEmojiCommand(_stage, _builder, _library, _playback, _locators, s),
            ClearEmojiCommandSpec s => new ClearEmojiCommand(_stage, _playback, s),

            _ => null
        };

        return command != null;
    }
}
