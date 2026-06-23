using UnityEngine;

public sealed class AudioCommandFactory : INodeCommandFactory
{
    private readonly AudioSystem _audio;
    private readonly IAudioClipResolver _clipResolver;

    public AudioCommandFactory(AudioSystem audio, IAudioClipResolver clipResolver)
    {
        _audio = audio;
        _clipResolver = clipResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            PlayBgmCommandSpec s => new PlayBgmCommand(_audio, ResolveClip(s.directClip, s.clipKey), s.fadeDuration),
            StopBgmCommandSpec s => new StopBgmCommand(_audio, s.fadeDuration),

            PlaySfxCommandSpec s => new PlaySfxCommand(_audio, ResolveClip(s.directClip, s.clipKey)),
            StopAllSfxCommandSpec _ => new StopAllSfxCommand(_audio),
            
            PlayVoiceCommandSpec s => new PlayVoiceCommand(_audio, ResolveClip(s.directClip, s.clipKey)),
            StopVoiceCommandSpec _ => new StopVoiceCommand(_audio),

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

        Debug.LogWarning($"[AudioCommandFactory] Failed to resolve AudioClip. clipKey={clipKey}");
        return null;
    }
}