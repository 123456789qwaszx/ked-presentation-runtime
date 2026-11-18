public enum DialogueBoxKind
{
    Portrait = 0,
    Speaker = 1,
    LetterBox = 2,
    OnlyText = 3,
    BlackBook = 4,
    Surface = 5,

    // 속말 — 줄 태그 #thought. 종류는 "어느 배치인가"다: 초상 칸을 켜는지 · 창 색 · 글자색은 그 배치 항목의 값이다
    // (DialogueSurfaceLayoutPresetDBSO.Entry — docs/PLAN_portrait-capture.md §1.5).
    Thought = 6,
}