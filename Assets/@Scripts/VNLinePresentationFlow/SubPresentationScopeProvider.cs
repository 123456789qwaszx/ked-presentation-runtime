// 서브 레인 driver/executor가 사용할 scope 제공자.
// PresentationSession이 매 route Start마다 만드는 SubScope(메인과 같은 Stage 공유)를 라이브로 반환.
public sealed class SubPresentationScopeProvider : ICommandRunScopeProvider
{
    private readonly PresentationSessionEntry _entry;

    public SubPresentationScopeProvider(PresentationSessionEntry entry)
    {
        _entry = entry;
    }

    public CommandRunScope CurrentScope => _entry != null ? _entry.SubScope : null;
}