using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 캔버스 루트. 빌더(Tools/Ked UI/Build Sample UI)가 만든 KedUICanvas 프리팹에 붙어 있다.
    /// UIPF를 세우는 일 — 층 셋(Root · Panel · Overlay), UIResolver(문맥·글꼴), UIManager, 화면 등록,
    /// UI 글자 표(Localizer · ViewTexts) — 과 흐름(KedUIFlow)의 수명을 갖는다.
    ///
    /// 호스트(SampleBootstrap)가 쓰는 길은 셋뿐이다 — UIPF 타입을 바깥에 내놓지 않는다(기본 어셈블리는 UIPF를 모른다).
    ///   Prepare()    조립 전: 화면을 세우고 대사창·선택지 상자 계약을 내준다.
    ///   Configure()  조립 후: 재생과 호스트 계약을 잇는다.
    ///   OpenTitle()  시작.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KedUIHost : MonoBehaviour
    {
        public const string ResourcePath = "KedUI/KedUICanvas";

        [Header("UIPF 층")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;
        [SerializeField] private RectTransform overlayLayer;
        [Tooltip("Panel 스택에서 살아 있는(보이는) 깊이. 맨 위 + 그 아래 하나.")]
        [SerializeField, Min(1)] private int panelKeepAliveDepth = 2;

        [Header("UIPF 테마")]
        [SerializeField] private UIFontSet fontSet;
        [SerializeField] private string themeId = "Light";
        [Tooltip("UI 글자 표의 언어 열(ko-KR · en-US). 긴 영어 글자 확인용으로 바꿔 볼 수 있다.")]
        [SerializeField] private string localeId = "ko-KR";
        [SerializeField] private TextAsset texts;
        [Tooltip("화면별 Presentation. presentationId = 화면 타입 이름. 없는 화면은 빈 Presentation으로 연다.")]
        [SerializeField] private UIPresentationSpec[] presentations = Array.Empty<UIPresentationSpec>();

        [Header("재생 쪽에 넘기는 것")]
        [Tooltip("선택지 항목 프리팹(본체 VNOptionItem). 새 그림으로 빌더가 만든다.")]
        [SerializeField] private VNOptionItem optionItemPrefab;
        [Tooltip("새 대사창에 맞춘 연출 프리셋 DB. 대본의 surface_layout 키는 옛 DB와 같다.")]
        [SerializeField] private DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb;

        private UIManager _ui;
        private Localizer _localizer;
        private ViewTexts _viewTexts;
        private KedUIFlow _flow;
        private readonly Dictionary<string, UIPresentationSpec> _presentationById = new(StringComparer.Ordinal);
        private readonly Dictionary<Type, UIPresentationSpec> _presentationByType = new();

        public bool IsPrepared => _flow != null;

        public VNOptionItem OptionItemPrefab => optionItemPrefab;
        public DialogueSurfaceLayoutPresetDBSO SurfaceLayoutDb => surfaceLayoutDb;

        public IPresentationDialogueBoxView DialogueBox => RequireFlow().DialogueBox;
        public IPresentationOptionsBoxView OptionsBox => RequireFlow().OptionsBox;
        public IPresentationView PresentationView => RequireFlow();

        /// <summary>화면을 세운다. 조립(PresentationAssembler)보다 먼저.</summary>
        public void Prepare()
        {
            if (_flow != null)
                return;

            foreach (UIPresentationSpec spec in presentations)
            {
                if (spec != null && !string.IsNullOrWhiteSpace(spec.presentationId))
                    _presentationById[spec.presentationId.Trim()] = spec;
            }

            UIContext context = new(themeId, localeId);
            UIResolver resolver = new(context, new UIThemeAssets(fontSet, null, null));

            _ui = new UIManager(
                rootLayer,
                panelLayer,
                resolver,
                new UIPresentationApplier(),
                panelKeepAliveDepth,
                coveredPanelAlpha: 1f,
                overlayLayer: overlayLayer);

            List<UIBase> views = RegisterViews();

            _localizer = new Localizer(texts != null ? texts.text : string.Empty, localeId);
            _viewTexts = new ViewTexts(views, _localizer);

            _flow = new KedUIFlow(_ui, _localizer, PresentationOf);

            if (TryGetComponent(out UIDisplayRefreshDriver driver))
                driver.Initialize(_ui);
        }

        /// <summary>재생과 호스트를 잇는다. slots는 저장 슬롯이 없는 예제에서 null.</summary>
        public void Configure(
            KedPlaybackBindings playback,
            IKedTitlePort title,
            IKedSaveSlotPort slots,
            IKedStagePort stage)
        {
            RequireFlow().Configure(playback, title, slots, stage);
        }

        public void OpenTitle()
        {
            RequireFlow().GoToTitle();
        }

        private void Update()
        {
            _flow?.Tick();
        }

        private void LateUpdate()
        {
            _flow?.LateTick();
        }

        private void OnDestroy()
        {
            _flow?.Dispose();
            _viewTexts?.Dispose();
        }

        // 층 아래의 화면(Root · Panel · Page · Overlay)을 모두 등록하고 감춘다. 위젯(카드·줄)은 화면의 일부라 등록하지 않는다.
        private List<UIBase> RegisterViews()
        {
            var views = new List<UIBase>();

            foreach (UIBase view in GetComponentsInChildren<UIBase>(includeInactive: true))
            {
                if (view is not (IUIRoot or IUIPanel or IUIPage or IUIOverlay))
                    continue;

                _ui.Register(view);
                views.Add(view);

                if (view.gameObject.activeSelf)
                    view.gameObject.SetActive(false);
            }

            return views;
        }

        private UIPresentationSpec PresentationOf(Type viewType)
        {
            if (_presentationByType.TryGetValue(viewType, out UIPresentationSpec spec))
                return spec;

            string key = UIViewKey.Of(viewType);

            if (!_presentationById.TryGetValue(key, out spec) || spec == null)
            {
                // 빌더가 Presentation을 만들지 않은 화면: 빈 Presentation(프리팹 그대로 + 공용 글꼴·글자 크기).
                spec = ScriptableObject.CreateInstance<UIPresentationSpec>();
                spec.name = key;
                spec.presentationId = key;
            }

            _presentationByType[viewType] = spec;
            return spec;
        }

        private KedUIFlow RequireFlow()
        {
            if (_flow == null)
                throw new InvalidOperationException("[Ked UI] Prepare()를 먼저 부른다.");

            return _flow;
        }
    }
}
