using System;
using UnityEngine;

public sealed class EmojiRigCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly EmojiRigBuilder _builder;
    private readonly EmojiLibrary _library;
    private readonly Vector2 _defaultRootSpace;

    /// <param name="defaultRootSpace">
    /// 로케이터의 자리 — 발(CharSlot_Scale의 아래 가운데)에서 잰 px, y 위. 캐릭터 + 바리에이션별 표가 서기 전의 기본값 하나다.
    /// </param>
    public EmojiRigCommandFactory(
        PresentationStage stage,
        EmojiRigBuilder builder,
        EmojiLibrary library,
        Vector2 defaultRootSpace)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _library = library ?? EmojiLibrary.Empty;
        _defaultRootSpace = defaultRootSpace;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            SetEmojiCommandSpec s => new SetEmojiCommand(_stage, _builder, _library, _defaultRootSpace, s),
            ClearEmojiCommandSpec s => new ClearEmojiCommand(_stage, _builder, s),

            _ => null
        };

        return command != null;
    }
}
