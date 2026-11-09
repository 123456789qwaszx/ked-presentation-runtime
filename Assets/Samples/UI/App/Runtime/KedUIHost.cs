using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 캔버스 루트. 빌더(Tools/Ked UI/Build Sample UI)가 만든 KedUICanvas 프리팹에 붙어 있다.
    /// Prepare()가 UIPF(층 · 문맥 · UIManager · 화면 등록 · UI 글자 표)와 흐름(KedUIFlow)을 세우고,
    /// 재생 조립에 건넬 것(대사창 · 선택지 상자 어댑터, 선택지 항목 프리팹, 대사창 레이아웃 DB)을 내준다.
    /// 흐름에는 Unity 수명(Update · OnDestroy)만 이어 준다.
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
        [Tooltip("화면별 Presentation. presentationId = 화면 키(UIViewKey — 묶음은 Ked.{타입}). 없는 화면은 빈 Presentation으로 연다.")]
        [SerializeField] private UIPresentationSpec[] presentations = Array.Empty<UIPresentationSpec>();

        [Header("재생 쪽에 넘기는 것")]
        [Tooltip("선택지 항목 프리팹(진행 예제의 CustomOptionItem). 새 그림으로 빌더가 만든다.")]
        [SerializeField] private CustomOptionItem optionItemPrefab;
        [Tooltip("새 대사창에 맞춘 연출 프리셋 DB. 대본의 surface_layout 키는 옛 DB와 같다.")]
        [SerializeField] private DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb;

        private UIManager _ui;
        private Localizer _localizer;
        private ViewTexts _viewTexts;
        private readonly Dictionary<string, UIPresentationSpec> _presentationById = new(StringComparer.Ordinal);
        private readonly Dictionary<Type, UIPresentationSpec> _presentationByType = new();

        public KedUIFlow Flow { get; private set; }

        // 재생 쪽 대사창 · 선택지 상자 계약의 구현. 화면 묶음은 재생 런타임을 모르므로 앱 쪽 어댑터로 잇는다.
        public IPresentationDialogueBoxView DialogueBox { get; private set; }
        public IChapterOptionsBoxView OptionsBox { get; private set; }

        // 메신저 패널. 재생 화면에 대사창 틀이 없으면 null — 본체는 메신저 줄도 대사창으로 보인다.
        public IPresentationMessengerView Messenger { get; private set; }

        public CustomOptionItem OptionItemPrefab => optionItemPrefab;
        public DialogueSurfaceLayoutPresetDBSO SurfaceLayoutDb => surfaceLayoutDb;

        /// <summary>화면을 세운다. 연출 조립(SampleBootstrap.AssemblePresentation)보다 먼저.</summary>
        public void Prepare()
        {
            if (Flow != null)
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

            PlayScreen play = _ui.GetUI<PlayScreen>();
            if (play == null)
                throw new InvalidOperationException("[Ked UI] 재생 화면(PlayScreen)이 등록되지 않았다. 빌더(Tools/Ked UI/Build Sample UI)를 다시 실행한다.");

            DialogueBox = new KedDialogueBoxView(play.Dialogue);
            OptionsBox = new KedOptionsBoxView(play.Options);
            Messenger = play.Messenger != null ? new KedMessengerView(play.Messenger) : null;

            _localizer = new Localizer(texts != null ? texts.text : string.Empty, localeId);
            _viewTexts = new ViewTexts(views, _localizer);

            Flow = new KedUIFlow(_ui, _localizer, PresentationOf);

            if (TryGetComponent(out UIDisplayRefreshDriver driver))
                driver.Initialize(_ui);
        }

        // Unity 입력은 여기서 읽고 흐름에는 이벤트로 넘긴다. 무엇을 닫을지는 흐름이 정한다.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Flow?.OnCancelPressed();

            // 재생 상태에는 바뀜을 알리는 이벤트가 없어 매 프레임 맞춘다.
            Flow?.SyncPlaybackState();
        }

        private void OnDestroy()
        {
            Flow?.Dispose();
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
    }
}
