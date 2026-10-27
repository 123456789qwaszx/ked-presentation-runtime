using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression.Save;
using UnityEngine;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    // 새 예제 UI가 호스트(Assembly-CSharp의 SampleBootstrap 쪽)에 묻는 것들.
    //
    // UI 어셈블리는 기본 어셈블리를 참조할 수 없다 — 그래서 호스트가 가진 기능(회차 열기·수동 저장·예제 전환·무대)은
    // 여기 계약으로 받고, 호스트가 어댑터로 구현해 넘긴다. 재생 쪽(본체 Playback)은 UI가 직접 참조한다.
    // 본체는 이 계약도, UI도 모른다.

    /// <summary>타이틀 화면이 쓰는 회차 입구와 예제 정보.</summary>
    public interface IKedTitlePort
    {
        /// <summary>지금 예제의 소개(타이틀 오른쪽 카드).</summary>
        KedExampleCard Example { get; }

        /// <summary>이어할 기록이 있는가. 가벼운 확인이다 — 있어도 읽다 실패하면 Resume이 처음부터로 떨어진다.</summary>
        bool HasResumePoint { get; }

        bool CanSwitchExample { get; }

        void StartNew();
        void Resume();
        void SwitchExample();
        void Quit();
    }

    /// <summary>
    /// 수동 저장 슬롯. 진행·저장 예제에만 있다 — 없으면(null) 메뉴의 저장·불러오기와 백로그의 갈라지기가 없다.
    /// 기존 저장 유즈케이스(ManualSaveFlow · SaveSlotService)를 그대로 감싼다. 저장 포맷은 바꾸지 않는다.
    /// </summary>
    public interface IKedSaveSlotPort
    {
        /// <summary>지금 저장할 수 있는가(진행 중인가).</summary>
        bool CanSave { get; }

        /// <summary>슬롯 목록. 읽지 못하면 false와 빈 목록(옛 형식·손상).</summary>
        bool TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);

        /// <summary>그 슬롯의 본문을 지금 콘텐츠로 읽을 수 있는가(콘텐츠 버전·파일 상태).</summary>
        bool CanLoad(string slotId);

        bool TryCreate();
        bool TryOverwrite(string slotId);

        /// <summary>불러온다. 회차 전환이 시작되는 순간 onTransitionStarted를 부른다(화면을 닫을 자리).</summary>
        Task LoadAsync(string slotId, Action onTransitionStarted);

        bool CanForkFrom(DialogueLogEntry entry);
        Task ForkFromBacklogAsync(DialogueLogEntry entry, Action onTransitionStarted);
    }

    /// <summary>
    /// 재생을 시작할 때 연출 무대(호스트의 PresentationUIRoot)를 보이게 하는 자리. 새 UI는 무대 위 캔버스에 투명한 층으로 얹힌다.
    /// </summary>
    public interface IKedStagePort
    {
        void ShowStage();
    }

    /// <summary>재생 쪽에서 UI가 직접 쓰는 것. 조립(PresentationAssembler)이 만든 것을 호스트가 모아 넘긴다.</summary>
    public sealed class KedPlaybackBindings
    {
        public VNFeatureController Features;
        public DialogueAdvanceDispatcher AdvanceDispatcher;
        public IPlaybackHost Host;
        public VNLinePresentationFlow LineFlow;
        public VNAdvanceInputPoller InputPoller;

        // 상태 읽기(퀵메뉴의 켜짐 표시)와 설정(자동 대기·빠르게 배속).
        public VNPlaybackRuntimeState PlaybackState;
        public EllipsisBreathTypewriter Typewriter;
        public AudioSystem Audio;

        // 백로그의 화자 이름(대본의 "이름: 대사"에서 읽는다).
        public DialogueRunner DialogueRunner;

        // 지금 장면 이름(대사창 위 꼬리표). 없으면 예제 이름만.
        public Func<string> CurrentUnitLabel;
    }
}
