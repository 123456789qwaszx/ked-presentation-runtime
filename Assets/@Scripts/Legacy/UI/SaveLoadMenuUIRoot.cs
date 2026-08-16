// using System;
// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using static UIRefValidation;
//
// public enum SaveLoadMenuMode
// {
//     Save = 0,
//     Load = 1
// }
//
// public sealed class SaveLoadMenuUIPanel : UIPanel<SaveLoadMenuUIPanel.Refs>
// {
//     public event Action<int> SlotClicked;
//     public event Action<SaveLoadMenuMode> ModeChanged;
//     public event Action CloseClicked;
//
//     #region Refs
//
//     public enum Refs
//     {
//         SaveLoadBG_Image,
//
//         SaveMode_Btn,
//         LoadModeBtn,
//
//         ContentRoot_Rect,
//
//         FirstSlots_Btn,
//         PrevSlots_Btn,
//         PageLabel_Text,
//         NextSlots_Btn,
//         LastSlots_Btn,
//
//         CloseButton_BWidget,
//     }
//
//     [Header("Prefab")]
//     [SerializeField] private VNSaveSlotButton _slotButtonPrefab;
//
//     [Header("Paging")]
//     [SerializeField, Min(1)] private int _slotsPerPage = 6;
//
//     private Image _saveLoadBg;
//
//     private Button _saveModeButton;
//     private Button _loadModeButton;
//
//     private RectTransform _contentRoot;
//
//     private RectTransform _firstSlotsBtnRoot;
//     private RectTransform _prevSlotsBtnRoot;
//     private RectTransform _nextSlotsBtnRoot;
//     private RectTransform _lastSlotsBtnRoot;
//
//     private Button _firstSlotsButton;
//     private Button _prevSlotsButton;
//     private Button _nextSlotsButton;
//     private Button _lastSlotsButton;
//
//     private TMP_Text _pageLabel;
//
//     private ButtonWidget _close;
//
//     #endregion
//
//     private readonly List<VNSaveSlotButton> _slotButtons = new();
//
//     private bool _valid;
//     private SaveLoadMenuMode _mode;
//
//     private VNSaveSlotMeta[] _metas = Array.Empty<VNSaveSlotMeta>();
//
//     // 0-based page index
//     private int _pageIndex;
//
//     private int TotalPageCount
//     {
//         get
//         {
//             if (_metas == null || _metas.Length <= 0)
//                 return 1;
//
//             return Mathf.Max(1, Mathf.CeilToInt(_metas.Length / (float)_slotsPerPage));
//         }
//     }
//
//     protected override void OnInitialize()
//     {
//         _saveLoadBg = View.Image(Refs.SaveLoadBG_Image);
//
//         _saveModeButton = View.Button(Refs.SaveMode_Btn);
//         _loadModeButton = View.Button(Refs.LoadModeBtn);
//
//         _contentRoot = View.Rect(Refs.ContentRoot_Rect);
//
//         _firstSlotsBtnRoot = View.Rect(Refs.FirstSlots_Btn);
//         _prevSlotsBtnRoot = View.Rect(Refs.PrevSlots_Btn);
//         _nextSlotsBtnRoot = View.Rect(Refs.NextSlots_Btn);
//         _lastSlotsBtnRoot = View.Rect(Refs.LastSlots_Btn);
//
//         _firstSlotsButton = ResolveButton(_firstSlotsBtnRoot);
//         _prevSlotsButton = ResolveButton(_prevSlotsBtnRoot);
//         _nextSlotsButton = ResolveButton(_nextSlotsBtnRoot);
//         _lastSlotsButton = ResolveButton(_lastSlotsBtnRoot);
//
//         _pageLabel = View.Text(Refs.PageLabel_Text);
//
//         _close = View.Widget<ButtonWidget>(Refs.CloseButton_BWidget);
//
// #if UNITY_EDITOR || DEVELOPMENT_BUILD
//         _valid = ValidateRefs();
//         if (!_valid) return;
// #else
//         _valid = true;
// #endif
//
//         _saveModeButton.onClick.RemoveListener(HandleSaveModeClicked);
//         _saveModeButton.onClick.AddListener(HandleSaveModeClicked);
//
//         _loadModeButton.onClick.RemoveListener(HandleLoadModeClicked);
//         _loadModeButton.onClick.AddListener(HandleLoadModeClicked);
//
//         _close.SetLabel("돌아가기");
//         _close.OnClicked -= HandleClose;
//         _close.OnClicked += HandleClose;
//
//         _firstSlotsButton.onClick.RemoveListener(HandleFirstPage);
//         _firstSlotsButton.onClick.AddListener(HandleFirstPage);
//
//         _prevSlotsButton.onClick.RemoveListener(HandlePrevPage);
//         _prevSlotsButton.onClick.AddListener(HandlePrevPage);
//
//         _nextSlotsButton.onClick.RemoveListener(HandleNextPage);
//         _nextSlotsButton.onClick.AddListener(HandleNextPage);
//
//         _lastSlotsButton.onClick.RemoveListener(HandleLastPage);
//         _lastSlotsButton.onClick.AddListener(HandleLastPage);
//
//         ClearSlots();
//         CreateSlotButtonsIfNeeded();
//         RefreshPage();
//     }
//
//     public void Rebuild(SaveLoadMenuMode mode, VNSaveSlotMeta[] metas)
//     {
//         if (!_valid)
//             return;
//
//         _mode = mode;
//         _metas = metas ?? Array.Empty<VNSaveSlotMeta>();
//
//         _pageIndex = Mathf.Clamp(_pageIndex, 0, TotalPageCount - 1);
//
//         CreateSlotButtonsIfNeeded();
//         RefreshPage();
//     }
//
//     public void ResetPage()
//     {
//         _pageIndex = 0;
//         RefreshPage();
//     }
//
//     #region Mode
//
//     private void HandleSaveModeClicked()
//     {
//         if (_mode == SaveLoadMenuMode.Save)
//             return;
//
//         ModeChanged?.Invoke(SaveLoadMenuMode.Save);
//     }
//
//     private void HandleLoadModeClicked()
//     {
//         if (_mode == SaveLoadMenuMode.Load)
//             return;
//
//         ModeChanged?.Invoke(SaveLoadMenuMode.Load);
//     }
//
//     private void RefreshModeButtons()
//     {
//         bool isSaveMode = _mode == SaveLoadMenuMode.Save;
//
//         // 현재 선택된 모드는 비활성화.
//         // 반대 모드만 클릭 가능.
//         _saveModeButton.interactable = !isSaveMode;
//         _loadModeButton.interactable = isSaveMode;
//     }
//
//     #endregion
//
//     #region Paging
//
//     private void HandleFirstPage()
//     {
//         if (_pageIndex <= 0)
//             return;
//
//         _pageIndex = 0;
//         RefreshPage();
//     }
//
//     private void HandlePrevPage()
//     {
//         if (_pageIndex <= 0)
//             return;
//
//         _pageIndex--;
//         RefreshPage();
//     }
//
//     private void HandleNextPage()
//     {
//         if (_pageIndex >= TotalPageCount - 1)
//             return;
//
//         _pageIndex++;
//         RefreshPage();
//     }
//
//     private void HandleLastPage()
//     {
//         int lastPageIndex = TotalPageCount - 1;
//
//         if (_pageIndex >= lastPageIndex)
//             return;
//
//         _pageIndex = lastPageIndex;
//         RefreshPage();
//     }
//
//     private void RefreshPage()
//     {
//         if (!_valid)
//             return;
//
//         bool isSaveMode = _mode == SaveLoadMenuMode.Save;
//         int startMetaIndex = _pageIndex * _slotsPerPage;
//
//         for (int i = 0; i < _slotButtons.Count; i++)
//         {
//             VNSaveSlotButton button = _slotButtons[i];
//
//             if (button == null)
//                 continue;
//
//             int metaIndex = startMetaIndex + i;
//
//             if (metaIndex >= _metas.Length)
//             {
//                 button.gameObject.SetActive(false);
//                 continue;
//             }
//
//             button.gameObject.SetActive(true);
//
//             int slotIndex = metaIndex + 1;
//             VNSaveSlotMeta meta = _metas[metaIndex];
//
//             button.Bind(slotIndex, meta, isSaveMode, HandleSlotClicked);
//         }
//
//         RefreshPagingButtons();
//         RefreshPageLabel();
//         RefreshModeButtons();
//     }
//
//     private void RefreshPagingButtons()
//     {
//         int lastPageIndex = TotalPageCount - 1;
//
//         bool canGoPrev = _pageIndex > 0;
//         bool canGoNext = _pageIndex < lastPageIndex;
//
//         _firstSlotsButton.interactable = canGoPrev;
//         _prevSlotsButton.interactable = canGoPrev;
//
//         _nextSlotsButton.interactable = canGoNext;
//         _lastSlotsButton.interactable = canGoNext;
//     }
//
//     private void RefreshPageLabel()
//     {
//         if (_pageLabel == null)
//             return;
//
//         _pageLabel.text = $"{_pageIndex + 1} / {TotalPageCount}";
//     }
//
//     #endregion
//
//     #region Slots
//
//     private void CreateSlotButtonsIfNeeded()
//     {
//         if (_contentRoot == null || _slotButtonPrefab == null)
//             return;
//
//         while (_slotButtons.Count < _slotsPerPage)
//         {
//             VNSaveSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
//             _slotButtons.Add(button);
//         }
//
//         for (int i = _slotButtons.Count - 1; i >= _slotsPerPage; i--)
//         {
//             if (_slotButtons[i] != null)
//                 Destroy(_slotButtons[i].gameObject);
//
//             _slotButtons.RemoveAt(i);
//         }
//     }
//
//     private void ClearSlots()
//     {
//         _slotButtons.Clear();
//
//         if (_contentRoot == null)
//             return;
//
//         for (int i = _contentRoot.childCount - 1; i >= 0; i--)
//             Destroy(_contentRoot.GetChild(i).gameObject);
//     }
//
//     private void HandleSlotClicked(int slotIndex)
//     {
//         SlotClicked?.Invoke(slotIndex);
//     }
//
//     #endregion
//
//     #region Event Handlers
//
//     private void HandleClose()
//     {
//         CloseClicked?.Invoke();
//     }
//
//     protected override void OnDestroy()
//     {
//         base.OnDestroy();
//
//         if (!_valid)
//             return;
//
//         if (_saveModeButton != null)
//             _saveModeButton.onClick.RemoveListener(HandleSaveModeClicked);
//
//         if (_loadModeButton != null)
//             _loadModeButton.onClick.RemoveListener(HandleLoadModeClicked);
//
//         if (_close != null)
//             _close.OnClicked -= HandleClose;
//
//         if (_firstSlotsButton != null)
//             _firstSlotsButton.onClick.RemoveListener(HandleFirstPage);
//
//         if (_prevSlotsButton != null)
//             _prevSlotsButton.onClick.RemoveListener(HandlePrevPage);
//
//         if (_nextSlotsButton != null)
//             _nextSlotsButton.onClick.RemoveListener(HandleNextPage);
//
//         if (_lastSlotsButton != null)
//             _lastSlotsButton.onClick.RemoveListener(HandleLastPage);
//     }
//
//     #endregion
//
//     #region Helpers
//
//     private Button ResolveButton(RectTransform root)
//     {
//         if (root == null)
//             return null;
//
//         Button button = root.GetComponent<Button>();
//
//         if (button != null)
//             return button;
//
//         return root.GetComponentInChildren<Button>(true);
//     }
//
//     private bool ValidateRefs()
//     {
//         string missing = "";
//
//         AppendMissing(ref missing, _saveLoadBg, Refs.SaveLoadBG_Image);
//
//         AppendMissing(ref missing, _saveModeButton, Refs.SaveMode_Btn);
//         AppendMissing(ref missing, _loadModeButton, Refs.LoadModeBtn);
//
//         AppendMissing(ref missing, _contentRoot, Refs.ContentRoot_Rect);
//
//         AppendMissing(ref missing, _firstSlotsBtnRoot, Refs.FirstSlots_Btn);
//         AppendMissing(ref missing, _prevSlotsBtnRoot, Refs.PrevSlots_Btn);
//         AppendMissing(ref missing, _nextSlotsBtnRoot, Refs.NextSlots_Btn);
//         AppendMissing(ref missing, _lastSlotsBtnRoot, Refs.LastSlots_Btn);
//
//         AppendMissing(ref missing, _firstSlotsButton, Refs.FirstSlots_Btn);
//         AppendMissing(ref missing, _prevSlotsButton, Refs.PrevSlots_Btn);
//         AppendMissing(ref missing, _nextSlotsButton, Refs.NextSlots_Btn);
//         AppendMissing(ref missing, _lastSlotsButton, Refs.LastSlots_Btn);
//
//         AppendMissing(ref missing, _pageLabel, Refs.PageLabel_Text);
//
//         AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);
//
//         if (_slotButtonPrefab == null)
//             missing += "- _slotButtonPrefab\n";
//
//         if (missing.Length > 0)
//         {
//             Debug.LogWarning($"[SaveLoadMenuUIRoot] Missing refs:\n{missing}", this);
//             return false;
//         }
//
//         return true;
//     }
//
//     #endregion
// }