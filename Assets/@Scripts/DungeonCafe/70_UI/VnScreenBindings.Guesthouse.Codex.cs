using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 업무 수첩. 전화로 확정된 개체만 열람할 수 있다.
///
/// 현재 어떤 플로우도 호출하지 않는다. 열람 진입점(게시판 버튼 등)을 붙일 때 연결한다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasCodexResult;

    public async YarnTask PresentCodexAsync(IReadOnlyList<ServiceBookingState> bookings)
    {
        _hasCodexResult = false;

        UI.PushPanel<MonsterCodexPanel>(panel =>
        {
            BindPanel(panel, ApplyCodexBindings);
            panel.Present(bookings);
        });

        await YarnWait.UntilAsync(() => _hasCodexResult);

        ClosePanel();
    }

    private void ApplyCodexBindings(MonsterCodexPanel panel)
    {
        AddBinding(panel,
            p => p.OnCloseRequested += HandleCodexClosed,
            p => p.OnCloseRequested -= HandleCodexClosed);
    }

    private void HandleCodexClosed() => _hasCodexResult = true;
}
