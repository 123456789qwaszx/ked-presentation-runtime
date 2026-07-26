using System.Collections.Generic;

/// <summary>
/// 관리자에게 올라가는 행동 승인 요청.
/// 승인 UI 는 이 객체 하나만 받아서 그린다.
/// </summary>
public sealed class ServiceApprovalRequest
{
    public ServiceSessionState Session { get; }
    public ServiceBeat Beat { get; }
    public IReadOnlyList<ServiceActionOption> Options { get; }
    public ProgressionTuning Tuning { get; }

    public ServiceApprovalRequest(
        ServiceSessionState session,
        ServiceBeat beat,
        IReadOnlyList<ServiceActionOption> options,
        ProgressionTuning tuning)
    {
        Session = session;
        Beat = beat;
        Options = options;
        Tuning = tuning;
    }

    public MaidRuntimeState Maid => Session.Maid;
    public ControlAuthorityStatus ControlStatus => Session.ControlStatus;

    /// <summary>해당 후보가 메이드의 대응력 범위를 벗어나는지.</summary>
    public bool IsBeyondAptitude(int optionIndex)
        => !ServiceOptionSelector.IsWithinAptitude(Options[optionIndex], Maid);

    /// <summary>해당 후보를 승인하면 붕괴 한계를 넘길 가능성이 있는지.</summary>
    public bool WouldBreachLimit(int optionIndex)
        => ServiceOptionSelector.WouldBreachLimit(Options[optionIndex], Maid, Tuning);
}
