using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class LobbyUIRoot : UIRoot<LobbyUIRoot.Refs>
{
    public event Action OnOpenStory;
    public event Action OnOpenLoad;
    public event Action OnOpenSettings;
    public event Action OnOpenRecruit;
    public event Action OnOpenRoster;
    public event Action OnOpenShop;

    public event Action OnNextBroadcastRequested;

    #region Refs

    public enum Refs
    {
        LobbyBG_Image,
        LobbyCharacter_Image,

        TimestampText_Text,
        NextBroadcastButton_BWidget,

        StoryButton_BWidget,
        LoadButton_BWidget,
        SettingsButton_BWidget,

        RecruitButton_BWidget,
        RosterButton_BWidget,
        ShopButton_BWidget,
    }

    private Image _lobbyBg;
    private Image _lobbyCharacter;

    private TMP_Text _timestampText;
    private ButtonWidget _nextBroadcast;

    private ButtonWidget _story;
    private ButtonWidget _load;
    private ButtonWidget _settings;

    private ButtonWidget _recruit;
    private ButtonWidget _roster;
    private ButtonWidget _shop;

    #endregion

    private bool _valid;

    protected override void OnInitialize()
    {
        _lobbyBg        = View.Image(Refs.LobbyBG_Image);
        _lobbyCharacter = View.Image(Refs.LobbyCharacter_Image);

        _timestampText  = View.Text(Refs.TimestampText_Text);
        _nextBroadcast  = View.Widget<ButtonWidget>(Refs.NextBroadcastButton_BWidget);

        _story    = View.Widget<ButtonWidget>(Refs.StoryButton_BWidget);
        _load     = View.Widget<ButtonWidget>(Refs.LoadButton_BWidget);
        _settings = View.Widget<ButtonWidget>(Refs.SettingsButton_BWidget);

        _recruit  = View.Widget<ButtonWidget>(Refs.RecruitButton_BWidget);
        _roster   = View.Widget<ButtonWidget>(Refs.RosterButton_BWidget);
        _shop     = View.Widget<ButtonWidget>(Refs.ShopButton_BWidget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        Present();
        BindHandlers();
    }

    private void Present()
    {
        _story?.SetLabel("스토리");
        // _load?.SetLabel("불러오기");
        // _settings?.SetLabel("설정");
        //
        // _recruit?.SetLabel("모집");
        // _roster?.SetLabel("여행가");
        // _shop?.SetLabel("상점");

        // Hub 기본 placeholder (런타임 값은 PresentHub가 덮어씀)
        if (_timestampText) _timestampText.text = "--.--.-- --:--";

        if (_nextBroadcast)
        {
            _nextBroadcast.SetLabel("다음 방송 없음");
            _nextBroadcast.SetInteractable(false);
        }
    }

    private void BindHandlers()
    {
        _story.OnClicked += HandleStory;
        // _load.OnClicked += HandleLoad;
        // _settings.OnClicked += HandleSettings;
        //
        // _recruit.OnClicked += HandleRecruit;
        // _roster.OnClicked += HandleRoster;
        // _shop.OnClicked += HandleShop;

        if (_nextBroadcast != null)
            _nextBroadcast.OnClicked += HandleNextBroadcast;
    }

    // Hub Runtime Present (Flow -> Lobby)
    public void PresentHub(
        DateTime currentStampUtc,
        bool canJumpNext,
        DateTime? nextStampUtc,
        string nextBroadcastKey)
    {
        if (!_valid) return;

        // 1) Timestamp
        if (_timestampText)
            _timestampText.text = currentStampUtc.ToString("yyyy.MM.dd HH:mm");

        // 2) Next Broadcast CTA
        if (_nextBroadcast)
        {
            string label = BuildNextBroadcastLabel(canJumpNext, nextStampUtc, nextBroadcastKey);
            _nextBroadcast.SetLabel(label);
            _nextBroadcast.SetInteractable(canJumpNext);
        }
    }

    private static string BuildNextBroadcastLabel(bool canJumpNext, DateTime? nextStampUtc, string nextBroadcastKey)
    {
        if (!canJumpNext || !nextStampUtc.HasValue || string.IsNullOrEmpty(nextBroadcastKey))
            return "다음 방송 없음";

        DateTime t = nextStampUtc.Value;

        // 표기 규칙은 여기서만 바꾸면 됨
        // 예: "3.20까지 점프" (월/일)
        return $"{t.Month}.{t.Day}까지 점프";
    }

    // ------------------------
    // Event Handlers
    // ------------------------

    private void HandleStory()    => OnOpenStory?.Invoke();
    private void HandleLoad()     => OnOpenLoad?.Invoke();
    private void HandleSettings() => OnOpenSettings?.Invoke();
    private void HandleRecruit()  => OnOpenRecruit?.Invoke();
    private void HandleRoster()   => OnOpenRoster?.Invoke();
    private void HandleShop()     => OnOpenShop?.Invoke();

    private void HandleNextBroadcast() => OnNextBroadcastRequested?.Invoke();

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (!_valid) return;

        _story.OnClicked -= HandleStory;
        // _load.OnClicked -= HandleLoad;
        // _settings.OnClicked -= HandleSettings;
        //
        // _recruit.OnClicked -= HandleRecruit;
        // _roster.OnClicked -= HandleRoster;
        // _shop.OnClicked -= HandleShop;

        if (_nextBroadcast != null)
            _nextBroadcast.OnClicked -= HandleNextBroadcast;
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _lobbyBg, Refs.LobbyBG_Image);
        AppendMissing(ref missing, _lobbyCharacter, Refs.LobbyCharacter_Image);

        AppendMissing(ref missing, _timestampText, Refs.TimestampText_Text);
        AppendMissing(ref missing, _nextBroadcast, Refs.NextBroadcastButton_BWidget);

        AppendMissing(ref missing, _story, Refs.StoryButton_BWidget);
        //AppendMissing(ref missing, _load, Refs.LoadButton_BWidget);
        //AppendMissing(ref missing, _settings, Refs.SettingsButton_BWidget);

        //AppendMissing(ref missing, _recruit, Refs.RecruitButton_BWidget);
        //AppendMissing(ref missing, _roster, Refs.RosterButton_BWidget);
        //AppendMissing(ref missing, _shop, Refs.ShopButton_BWidget);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[LobbyUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}