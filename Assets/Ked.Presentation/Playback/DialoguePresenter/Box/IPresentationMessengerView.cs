using System.Collections.Generic;

// 메신저 패널 — 메신저 기록(MessengerLog)을 말풍선으로 그린다 (docs/PLAN_messenger.md §4.1).
public interface IPresentationMessengerView : IPresentationLineSurfaceView
{
    // 기록 전체를 그린다. animateLast면 마지막 말풍선만 등장 연출, 나머지는 선 채로.
    // 같은 기록이면 몇 번을 불러도 같은 화면이어야 한다 — 되감기 도착 줄은 쌓인 기록을 한 번에 받는다.
    // entries는 살아 있는 목록이다. 이 호출 안에서 읽고, 들고 있지 않는다.
    void Render(IReadOnlyList<MessengerEntry> entries, bool animateLast);
}
