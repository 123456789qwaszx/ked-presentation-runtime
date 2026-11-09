public enum DialogueBoxKind
{
    Portrait = 0,
    Speaker = 1,
    LetterBox = 2,
    OnlyText = 3,
    BlackBook = 4,
    Surface = 5,

    // 메신저 구간(messenger_open ~ messenger_close)의 줄. 줄 태그로 고르지 않는다 — 구간이 정한다.
    Messenger = 6,
}