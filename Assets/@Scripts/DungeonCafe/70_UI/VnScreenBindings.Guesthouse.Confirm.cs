using Yarn.Unity;

/// <summary>
/// 범용 확인 창. 게스트하우스 루프의 특정 단계에 묶이지 않는다.
///
/// 현재 호출부가 없다. 중간 확인이 필요해지면 여기서 가져다 쓰고,
/// 끝까지 쓰이지 않으면 파일째 지워도 다른 화면에 영향이 없다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasConfirmResult;

    private async YarnTask PresentConfirmAsync(string title, string body, string confirmLabel)
    {
        _hasConfirmResult = false;

        UI.PushPanel<ConfirmPanel>(panel =>
        {
            BindPanel(panel, ApplyConfirmBindings);

            panel.Present(
                title: title,
                body: body,
                confirmLabel: confirmLabel,
                cancelLabel: string.Empty);
        });

        await YarnWait.UntilAsync(() => _hasConfirmResult);

        ClosePanel();
    }

    private void ApplyConfirmBindings(ConfirmPanel panel)
    {
        AddBinding(panel,
            p => p.ConfirmClicked += HandleConfirmClicked,
            p => p.ConfirmClicked -= HandleConfirmClicked);
    }

    private void HandleConfirmClicked() => _hasConfirmResult = true;
}
