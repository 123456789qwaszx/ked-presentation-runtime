using System;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 스토리 화면의 챕터 카드 한 장. StoryPanel이 숨겨 둔 견본(ChapterCardTemplate)을 챕터 수만큼 복제해 쓴다.
    /// UIPF 위젯 — 등록하지 않는다. 지금은 고정된 네모 상자에 글자만 든다(번호 · 이름 · 장면/에피소드 수).
    /// 그림 창(ChapterArt)은 자리만 잡아 두었다 — 카드 그림 · 버튼 자리를 편집하는 도구가 생기면 여기에 꽂는다.
    /// </summary>
    [UIViewKey("Ked.StoryChapterCard")]
    public sealed class StoryChapterCard : UIBase<StoryChapterCard.Refs>
    {
        public enum Refs
        {
            ChapterButton,
            ChapterSurface,
            ChapterLine,
            ChapterIndex,
            ChapterArt,
            ChapterArtNumber,
            ChapterName,
            ChapterInfo,
        }

        public sealed class Model
        {
            public int Number;      // 1부터 — 카드가 놓인 순서
            public string Name;     // 챕터 이름(콘텐츠 글자)
            public string Info;     // 한 줄 정보 — 글자는 흐름이 표에서 만들어 넘긴다
        }

        public event Action<StoryChapterCard> Clicked;

        public int Index { get; set; }

        public Button Button => View.Button(Refs.ChapterButton);

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.ChapterButton), () => Clicked?.Invoke(this));
        }

        public void Present(Model model)
        {
            string number = KedUIUtil.TwoDigits(model.Number);

            KedUIUtil.SetText(View.Text(Refs.ChapterIndex), number);
            KedUIUtil.SetText(View.Text(Refs.ChapterArtNumber), number);
            KedUIUtil.SetText(View.Text(Refs.ChapterName), model.Name);
            KedUIUtil.SetText(View.Text(Refs.ChapterInfo), model.Info);
        }
    }
}
