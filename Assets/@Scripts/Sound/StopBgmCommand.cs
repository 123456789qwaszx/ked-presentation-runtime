using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint("Sound", "Stop BGM", Order = -899)]
public sealed class StopBgmCommandSpec : CommandSpecBase
{
    [Min(0f)]
    public float fadeDuration = 1f;
}

public sealed class StopBgmCommand : CommandBase
{
    private readonly AudioSystem _audio;
    private readonly float       _fadeDuration;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;
    public override bool WaitForCompletion => false;

    public StopBgmCommand(AudioSystem audio, float fadeDuration = 1f)
    {
        _audio        = audio;
        _fadeDuration = fadeDuration;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _audio.Bgm.Stop(_fadeDuration, isSkipping: false);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _audio.Bgm.Stop(_fadeDuration, isSkipping: true);
    }
}