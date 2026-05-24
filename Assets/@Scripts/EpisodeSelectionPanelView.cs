using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeSelectionPanelView : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text chapterIndexText;
    [SerializeField] private TMP_Text chapterEraText;
    [SerializeField] private TMP_Text chapterTitleText;

    [Header("Outcome")]
    [SerializeField] private TMP_Text intuitionPercentText;
    [SerializeField] private TMP_Text analysisPercentText;
    [SerializeField] private TMP_Text chaosPercentText;
    [SerializeField] private TMP_Text endingBranchNameText;
    [SerializeField] private Image outcomeTitleImage;

    [Header("Graph")]
    [SerializeField] private EpisodeGraphView graphView;

    [Header("Return")]
    [SerializeField] private Button returnButton;

    public event Action CloseRequested;

    private void Awake()
    {
        if (returnButton != null)
            returnButton.onClick.AddListener(HandleReturnClicked);
    }

    private void OnDestroy()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(HandleReturnClicked);
    }

    public void SetHandlers(
        Action<string> onMain,
        Action<string, LinkKind, string> onBranch)
    {
        if (graphView != null)
            graphView.SetHandlers(onMain, onBranch);
    }

    public void Present(
        in EpisodeSelectionPanelModel panel,
        in PlayerStateSnapshot playerState)
    {
        PresentHeader(panel.ChapterMeta);
        PresentGraph(panel.Graph);
        PresentOutcome(playerState);
    }

    public void ClearGraph()
    {
        if (graphView != null)
            graphView.ClearAll();
    }

    private void PresentHeader(in ChapterMetaModel meta)
    {
        SetText(chapterIndexText, meta.ChapterIndex);
        SetText(chapterEraText, meta.EraText);
        SetText(chapterTitleText, meta.ChapterTitle);
    }

    private void PresentGraph(in EpisodeGraphModel graph)
    {
        if (graphView != null)
            graphView.Render(graph);
    }

    private void PresentOutcome(in PlayerStateSnapshot state)
    {
        SetText(intuitionPercentText, $"{state.Intuition}%");
        SetText(analysisPercentText, $"{state.Analysis}%");
        SetText(chaosPercentText, $"{state.Chaos}%");
    }

    private void HandleReturnClicked()
    {
        CloseRequested?.Invoke();
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? "";
    }
}