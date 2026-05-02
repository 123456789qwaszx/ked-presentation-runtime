using System;

public interface IPresentationResponseTarget
{
    void ApplyResponse(in PresentationResponse response);
}

/// <summary>
/// 런타임 전용 binding.
/// 완성된 target + profile을 묶고, state를 response로 번역해 target에 적용한다.
/// </summary>
public sealed class PresentationResponseBinding
{
    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IPresentationResponseTarget Target { get; }

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        IPresentationResponseTarget target)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Binding key must not be null or empty.", nameof(key));

        Key = key;
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public void Apply(in PresentationIntentState state)
    {
        PresentationResponse response = PresentationResponseSolver.Solve(state, Profile);
        Target.ApplyResponse(in response);
    }
}