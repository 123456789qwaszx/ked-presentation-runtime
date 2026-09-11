using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VNScreenBindings
{
    private SaveLoadMenuMode _currentSaveLoadMode;

    // 현재 패널에 표시된 Bookmark 순서.
    // UI의 숫자 slotIndex를 영속 Bookmark.Id로 변환하기 위한 화면 전용 매핑.
    private readonly List<Bookmark> _saveLoadSlots = new();

    private SaveLoadMenuUIPanel _saveLoadPanel;
    private bool _saveLoadRequestInProgress;

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        if (_learningMode)
        {
            Debug.Log("[학습] 수동 슬롯은 이후 단계에서 연결한다. 현재는 새 게임·이어하기를 사용한다.");
            return;
        }

        if (_saveCoordinator == null)
            return;

        _currentSaveLoadMode = mode;

        UI.PushPanel<SaveLoadMenuUIPanel>(panel =>
        {
            _saveLoadPanel = panel;

            BindPanel(panel, ApplyBindings);

            Refresh(panel);
            panel.ResetPage();
        });
    }

    private void ApplyBindings(SaveLoadMenuUIPanel panel)
    {
        AddBinding(
            panel,
            p => p.SlotClicked += HandleSlotClicked,
            p => p.SlotClicked -= HandleSlotClicked);

        AddBinding(
            panel,
            p => p.ModeChanged += HandleSaveLoadModeChanged,
            p => p.ModeChanged -= HandleSaveLoadModeChanged);

        AddBinding(
            panel,
            p => p.CloseClicked += HandleSaveLoadCloseClicked,
            p => p.CloseClicked -= HandleSaveLoadCloseClicked);
    }

    #region Handlers

    private void HandleSlotClicked(int slotIndex)
    {
        if (_saveLoadRequestInProgress)
            return;

        int index = slotIndex - 1;

        if (index < 0)
            return;

        switch (_currentSaveLoadMode)
        {
            case SaveLoadMenuMode.Save:
                HandleSaveSlotClicked(index);
                break;

            case SaveLoadMenuMode.Load:
                HandleLoadSlotClicked(index);
                break;
        }
    }

    private void HandleSaveLoadModeChanged(SaveLoadMenuMode mode)
    {
        // 타이틀처럼 진행 중이 아닌 곳에서는 저장할 현재 라인이 없다.
        if (mode == SaveLoadMenuMode.Save &&
            (_progressionLauncher == null || !_progressionLauncher.IsRunning))
        {
            Debug.Log("[수동 저장] 진행 중이 아니므로 저장 모드로 전환할 수 없다.");

            RefreshCurrentSaveLoadPanel();
            return;
        }

        _currentSaveLoadMode = mode;

        RefreshCurrentSaveLoadPanel();
        _saveLoadPanel?.ResetPage();
    }

    private void HandleSaveLoadCloseClicked()
    {
        _saveLoadPanel = null;
        ClosePanel();
    }

    #endregion

    #region Save

    private void HandleSaveSlotClicked(int index)
    {
        if (!TryCaptureManualSave(
                out IReadOnlyList<CommittedChoice> path,
                out IReadOnlyList<VNChoiceRecord> yarnChoices,
                out SaveLineTarget target,
                out string preview))
        {
            return;
        }

        // 기존 Bookmark를 누름 = 덮어쓰기.
        if (index < _saveLoadSlots.Count)
        {
            Bookmark existing = _saveLoadSlots[index];

            OverwriteManualSave(
                existing,
                path,
                yarnChoices,
                target,
                preview);

            return;
        }

        // Save 모드에서 기존 슬롯 뒤에 붙인 빈 슬롯.
        if (index == _saveLoadSlots.Count)
        {
            CreateManualSave(
                path,
                yarnChoices,
                target,
                preview);
        }
    }

    private bool TryCaptureManualSave(
        out IReadOnlyList<CommittedChoice> path,
        out IReadOnlyList<VNChoiceRecord> yarnChoices,
        out SaveLineTarget target,
        out string preview)
    {
        path = null;
        yarnChoices = null;
        target = null;
        preview = null;

        if (_saveCoordinator == null ||
            _progressionLauncher == null ||
            _vnFeatures == null ||
            !_progressionLauncher.IsRunning)
        {
            return false;
        }

        if (!_vnFeatures.TryGetCurrentLine(
                out target,
                out preview))
        {
            Debug.Log(
                "[수동 저장] 지금은 저장할 수 있는 대사 위치가 아니다.");

            return false;
        }

        path = _progressionLauncher.PendingPath;
        yarnChoices = _vnFeatures.CreateYarnChoiceSnapshot();

        return true;
    }

    private void CreateManualSave(
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview)
    {
        Bookmark bookmark = _saveCoordinator.CreateBookmark(
            path,
            yarnChoices,
            target,
            preview);

        if (bookmark == null)
        {
            Debug.LogWarning("[수동 저장] 저장하지 못했다.");
            return;
        }

        RefreshCurrentSaveLoadPanel();
    }

    private void OverwriteManualSave(
        Bookmark existing,
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview)
    {
        if (existing == null)
            return;

        _saveCoordinator.OverwriteBookmark(
            existing.Id,
            path,
            yarnChoices,
            target,
            preview,
            existing.Label);

        RefreshCurrentSaveLoadPanel();
    }

    #endregion

    #region Load

    private async void HandleLoadSlotClicked(int index)
    {
        if (_progressionLauncher == null ||
            _saveCoordinator == null)
        {
            return;
        }

        if (index < 0 || index >= _saveLoadSlots.Count)
            return;

        Bookmark bookmark = _saveLoadSlots[index];

        if (bookmark == null || string.IsNullOrEmpty(bookmark.Id))
            return;

        _saveLoadRequestInProgress = true;

        bool transitionStarted = false;

        try
        {
            await _progressionLauncher.TransitionAfterAsync(
                async () =>
                {
                    Bookmark hydrated =
                        await _saveCoordinator.GetBookmarkAsync(bookmark.Id);

                    return hydrated != null;
                },
                () =>
                {
                    transitionStarted = true;

                    // 다운로드와 전환 유효성 검사가 끝난 뒤에 닫는다.
                    // 다운로드 실패 시에는 Load 화면을 그대로 유지.
                    _saveLoadRequestInProgress = false;
                    _saveLoadPanel = null;

                    ClosePanel();

                    return _saveCoordinator.ForkFromBookmark(bookmark);
                });
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"[수동 저장] 불러오기 실패\n{error}");
        }
        finally
        {
            // Transition이 시작되지 않았다면
            // hydration 실패 또는 늦은 요청 취소.
            if (!transitionStarted)
                _saveLoadRequestInProgress = false;
        }
    }

    #endregion

    #region Refresh

    private void RefreshCurrentSaveLoadPanel()
    {
        if (_saveLoadPanel != null)
            Refresh(_saveLoadPanel);
    }

    private void Refresh(SaveLoadMenuUIPanel saveLoadPanel)
    {
        if (saveLoadPanel == null ||
            _saveCoordinator == null)
        {
            return;
        }

        IReadOnlyList<Bookmark> bookmarks =
            _saveCoordinator.Bookmarks;

        _saveLoadSlots.Clear();

        for (int i = 0; i < bookmarks.Count; i++)
        {
            Bookmark bookmark = bookmarks[i];

            if (bookmark != null)
                _saveLoadSlots.Add(bookmark);
        }

        int visibleCount = _saveLoadSlots.Count;

        // Save 모드에서는 기존 저장들 뒤에
        // "새 저장"용 빈 슬롯 하나를 추가한다.
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
            visibleCount++;

        var metas = new VNSaveSlotMeta[visibleCount];

        for (int i = 0; i < _saveLoadSlots.Count; i++)
            metas[i] = CreateSlotMeta(_saveLoadSlots[i]);

        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
            metas[^1] = CreateEmptySlotMeta();

        saveLoadPanel.Rebuild(
            _currentSaveLoadMode,
            metas);
    }

    private static VNSaveSlotMeta CreateSlotMeta(Bookmark bookmark)
    {
        return new VNSaveSlotMeta
        {
            IsEmpty = false,

            Label = bookmark.Label,
            Preview = bookmark.Preview,

            ChapterId = bookmark.ChapterId,
            SavedAtUtc = bookmark.CreatedAtUtc,

            PlaySeconds = bookmark.PlaySecondsAtBookmark,

            RequiresDownload =
                bookmark.SnapshotKey == null,

            HasSyncError =
                !string.IsNullOrEmpty(bookmark.SyncError),
        };
    }

    private static VNSaveSlotMeta CreateEmptySlotMeta()
    {
        return new VNSaveSlotMeta
        {
            IsEmpty = true,
        };
    }

    #endregion
}
