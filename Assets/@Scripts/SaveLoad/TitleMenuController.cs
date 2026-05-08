using UnityEngine;
using UnityEngine.UI;

public sealed class TitleMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _albumButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    [Header("Panels")]
    [SerializeField] private SaveLoadMenuController _saveLoadMenu;
    [SerializeField] private AlbumMenuController _albumMenu;

    [Header("New Game")]
    [SerializeField] private string _startNodeName = "Start";

    [Tooltip("IVNGameStarter를 구현한 MonoBehaviour")]
    [SerializeField] private MonoBehaviour _gameStarterBehaviour;

    private IVNGameStarter _gameStarter;
    private VNServiceContainer _svc;

    private void Awake()
    {
        _gameStarter = _gameStarterBehaviour as IVNGameStarter;

        if (_gameStarterBehaviour != null && _gameStarter == null)
            Debug.LogError("[TitleMenuController] GameStarterBehaviour does not implement IVNGameStarter.", this);
    }

    private void OnEnable()
    {
        _svc = VNServiceContainer.Instance;

        if (_svc != null)
        {
            _svc.PersistentInitialized += RefreshButtons;
            _svc.RuntimeBound += RefreshButtons;
        }

        RefreshButtons();
    }

    private void OnDisable()
    {
        if (_svc != null)
        {
            _svc.PersistentInitialized -= RefreshButtons;
            _svc.RuntimeBound -= RefreshButtons;
        }
    }

    private void Start()
    {
        if (_continueButton != null) _continueButton.onClick.AddListener(OnContinue);
        if (_newGameButton != null) _newGameButton.onClick.AddListener(OnNewGame);
        if (_loadButton != null) _loadButton.onClick.AddListener(OnLoad);
        if (_albumButton != null) _albumButton.onClick.AddListener(OnAlbum);
        if (_quitButton != null) _quitButton.onClick.AddListener(OnQuit);

        RefreshButtons();
    }

    public void RefreshButtons()
    {
        _svc = VNServiceContainer.Instance;

        if (_continueButton != null)
            _continueButton.interactable = _svc != null && _svc.CanContinue();

        if (_loadButton != null)
            _loadButton.interactable = _svc != null && _svc.IsPersistentInitialized;

        if (_albumButton != null)
            _albumButton.interactable = _svc != null && _svc.IsPersistentInitialized;
    }

    private void OnContinue()
    {
        if (_svc == null)
        {
            Debug.LogWarning("[TitleMenuController] VNServiceContainer not found.");
            return;
        }

        _svc.TryContinue();
    }

    private void OnNewGame()
    {
        if (_gameStarter == null)
        {
            Debug.LogWarning($"[TitleMenuController] New Game requested, but no IVNGameStarter is bound. startNode='{_startNodeName}'");
            return;
        }

        _gameStarter.StartNewGame(_startNodeName);
    }

    private void OnLoad()
    {
        _saveLoadMenu?.OpenAsLoadMenu();
    }

    private void OnAlbum()
    {
        _albumMenu?.Open();
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}