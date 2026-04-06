using UnityEngine;

public sealed class SoundCommandFactory : INodeCommandFactory
{
    private readonly AudioSystem _audio;
    private readonly IAudioClipResolver _clipResolver;

    public SoundCommandFactory(
        AudioSystem audio,
        IAudioClipResolver clipResolver)
    {
        _audio = audio;
        _clipResolver = clipResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            PlayBgmCommandSpec s => new PlayBgmCommand(_audio, ResolveClip(s.directClip, s.clipKey), s.fadeDuration),
            StopBgmCommandSpec s => new StopBgmCommand(_audio, s.fadeDuration),
            PlayVoiceCommandSpec s => new PlayVoiceCommand(_audio, ResolveClip(s.directClip, s.clipKey)),
            StopVoiceCommandSpec _ => new StopVoiceCommand(_audio),
            PlaySfxCommandSpec s => new PlaySfxCommand(_audio, ResolveClip(s.directClip, s.clipKey)),
            StopAllSfxCommandSpec _ => new StopAllSfxCommand(_audio),

            _ => null
        };

        return command != null;
    }

    private AudioClip ResolveClip(AudioClip directClip, string clipKey)
    {
        if (directClip != null)
            return directClip;

        if (_clipResolver != null &&
            _clipResolver.TryResolve(clipKey, out AudioClip resolved))
            return resolved;

        Debug.LogWarning($"[SoundCommandFactory] Failed to resolve AudioClip. clipKey={clipKey}");
        return null;
    }
}