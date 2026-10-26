using UnityEngine;

public sealed partial class VNScreenBindings
{
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(
            afterPatched: root =>
            {
                BindView(root, ApplyBindings);

                // 저장 슬롯이 없는 예제(연출 단독)는 불러오기 메뉴가 없다.
                root.SetLoadVisible(HasSaveSlots);
                root.SetExample(_exampleName, _otherExampleName);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(TitleUIRoot root)
    {
        AddBinding(root,
            r => r.ContinueClicked += HandleContinueClicked,
            r => r.ContinueClicked -= HandleContinueClicked);

        AddBinding(root,
            r => r.StartClicked += HandleStartClicked,
            r => r.StartClicked -= HandleStartClicked);

        AddBinding(root,
            r => r.LoadClicked += HandleLoadClicked,
            r => r.LoadClicked -= HandleLoadClicked);

        AddBinding(root,
            r => r.QuitClicked += HandleQuitClicked,
            r => r.QuitClicked -= HandleQuitClicked);

        AddBinding(root,
            r => r.SwitchExampleClicked += HandleSwitchExampleClicked,
            r => r.SwitchExampleClicked -= HandleSwitchExampleClicked);
    }

    #region Handlers

    private void HandleContinueClicked()
    {
        _playthroughEntry.Resume();
    }

    private void HandleStartClicked()
    {
        _playthroughEntry.StartNew();
    }

    private void HandleSwitchExampleClicked()
    {
        _switchExample?.Invoke();
    }

    private void HandleLoadClicked()
    {
        OpenLoadMenu();
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}