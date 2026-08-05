using UnityEngine;

public sealed class AudioCommandFactory : INodeCommandFactory
{
    private readonly AudioSystem _audio;

    public AudioCommandFactory(AudioSystem audio)
    {
        _audio = audio;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            PlayBgmCommandSpec s => new PlayBgmCommand(_audio, ResolveClip(s.directClip, s.clipKey), s.clipKey, s.fadeDuration),
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

        if(!ResourcesAudioClipResolver.TryResolve(clipKey, out AudioClip resolved))
        {
            //Debug.LogWarning($"[AudioCommandFactory] Failed to resolve AudioClip. clipKey={clipKey}");
            return null;
        }
        
        return resolved;
    }
}