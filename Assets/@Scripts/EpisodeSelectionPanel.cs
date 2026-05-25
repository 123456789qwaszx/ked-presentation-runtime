using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class EpisodeSelectionPanel : UIPanel<EpisodeSelectionPanel.Refs>, IUIPanel
{
    public event Action OnCloseRequested;
    
    public void SetHandlers(Action<string> onMain, Action<string, LinkKind, string> onBranch)
    {
        episodeGraphView.SetHandlers(onMain, onBranch);
    }

    #region Refs

    public enum Refs
    {
        EpisodeSelectionBG_Root,
        EpisodeSelectionBG_Image,

        ReturnBlock_Root,
        CurrentScreenLabel_Root,
        CurrentScreenLabelBG_Image,
        CurrentScreenLabel_Text,
        CurrentScreenLabelIcon_Image,

        ReturnButton_Root,
        ReturnButton_Image,
        //ReturnButton_Button,

        EpisodeList_Root,
        ButtonViewport,
        EpisodeButtons,

        ChapterSummary_Root,
        ChapterMeta_Root,
        ChapterIndex_Root,
        ChapterIndex_Image,
        ChapterIndex_Text,

        ChapterEra_Root,
        ChapterEra_Text,

        ChapterTitle_Root,
        ChapterTitle_Text,

        ChoiceOutcome_Root,
        OutcomeTitle_Root,
        OutcomeTitle_Image,

        OutcomeMetrics_Root,
        Intuition_Root,
        IntuitionBG_Image,
        IntuitionIcon_Root,
        IntuitionIcon_Image,

        IntuitionLabel_Root,
        IntuitionLabel_Text,
        IntuitionPercent_Root,
        IntuitionPercent_Text,

        Analysis_Root,
        AnalysisBG_Image,
        AnalysisIcon_Root,
        AnalysisIcon_Image,

        AnalysisLabel_Root,
        AnalysisIconLabel_Text,
        AnalysisPercent_Root,
        AnalysisIconPercent_Text,

        Chaos_Root,
        ChaosBG_Image,
        ChaosIcon_Root,
        ChaosIcon_Image,
        ChaosLabel_Root,
        ChaosLabel_Text,
        ChaosPercent_Root,
        ChaosPercent_Text,

        EndingBranchName_Root,
        EndingBranchName_Text,
        
        ReturnButton_BWidget
    }

    private TMP_Text _chapterIndex;
    private TMP_Text _chapterEra;
    private TMP_Text _chapterTitle;
    private Button _returnButton;
    private RectTransform _choiceOutcomeRoot;
    private TMP_Text _endingBranchNameText;
    private Image _outcomeTitleImage;
    private TMP_Text _intuitionPercentText;
    private TMP_Text _analysisPercentText;
    private TMP_Text _chaosPercentText;

    private Image _intuitionIconImage;
    private Image _analysisIconImage;
    private Image _chaosIconImage;
    
    private ButtonWidget _return;
    
    #endregion
    
    [SerializeField] private EpisodeGraphView episodeGraphView;
    
    private bool _valid;

    protected override void OnInitialize()
    {
        _chapterIndex         = View.Text(Refs.ChapterIndex_Text);
        _chapterEra           = View.Text(Refs.ChapterEra_Text);
        _chapterTitle         = View.Text(Refs.ChapterTitle_Text);
        
        _choiceOutcomeRoot    = View.Rect(Refs.ChoiceOutcome_Root);
        _endingBranchNameText = View.Text(Refs.EndingBranchName_Text);
        _outcomeTitleImage    = View.Image(Refs.OutcomeTitle_Image);
        _intuitionPercentText = View.Text(Refs.IntuitionPercent_Text);
        _analysisPercentText  = View.Text(Refs.AnalysisIconPercent_Text);
        
        _chaosPercentText     = View.Text(Refs.ChaosPercent_Text);
        _intuitionIconImage   = View.Image(Refs.IntuitionIcon_Image);
        _analysisIconImage    = View.Image(Refs.AnalysisIcon_Image);
        _chaosIconImage       = View.Image(Refs.ChaosIcon_Image);
        
        _return               = View.Widget<ButtonWidget>(Refs.ReturnButton_BWidget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif
        
        _return.SetLabel("뒤로");
        _return.OnClicked += HandleReturn;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (_return != null)
            _return.OnClicked -= HandleReturn;
    }
    
    #region Present
    
    public void Present(in EpisodeSelectionPanelModel panel, in PlayerStateSnapshot model)
    {
        if (!_valid) return;

        PresentHeader(panel.ChapterMeta);
        PresentGraph(panel.Graph);
        PresentOutcome(model);
    }
    
    
    private void PresentHeader(in ChapterMetaModel meta)
    {
        _chapterIndex.text = meta.ChapterIndex;
        _chapterEra.text   = meta.EraText;
        _chapterTitle.text = meta.ChapterTitle;
    }

    private void PresentGraph(in EpisodeGraphModel graph)
    {
        episodeGraphView.Render(graph);
    }

    private void PresentOutcome(in PlayerStateSnapshot model)
    {
        _intuitionPercentText.text = $"{model.Intuition}%";
        _analysisPercentText.text  = $"{model.Analysis}%";
        _chaosPercentText.text     = $"{model.Chaos}%";
        
        //_endingBranchNameText.text = string.IsNullOrEmpty(model.CurrentBranchName) ? "미결정" : model.CurrentBranchName;
        //_outcomeTitleImage.sprite = null;
    }
    #endregion

    #region Event Handlers

    private void HandleReturn()
    {
        OnCloseRequested?.Invoke();
    }
    
    #endregion
    
    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _chapterIndex,         Refs.ChapterIndex_Text);
        AppendMissing(ref missing, _chapterEra,           Refs.ChapterEra_Text);
        AppendMissing(ref missing, _chapterTitle,         Refs.ChapterTitle_Text);
        
        AppendMissing(ref missing, _choiceOutcomeRoot,    Refs.ChoiceOutcome_Root);
        AppendMissing(ref missing, _endingBranchNameText, Refs.EndingBranchName_Text);
        AppendMissing(ref missing, _outcomeTitleImage,    Refs.OutcomeTitle_Image);
        AppendMissing(ref missing, _intuitionPercentText, Refs.IntuitionPercent_Text);
        AppendMissing(ref missing, _analysisPercentText,  Refs.AnalysisIconPercent_Text);
        
        AppendMissing(ref missing, _chaosPercentText,     Refs.ChaosPercent_Text);
        AppendMissing(ref missing, _intuitionIconImage,   Refs.IntuitionIcon_Image);
        AppendMissing(ref missing, _analysisIconImage,    Refs.AnalysisIcon_Image);
        AppendMissing(ref missing, _chaosIconImage,       Refs.ChaosIcon_Image);
        
        //AppendMissing(ref missing, episodeGraphView, Refs.EpisodeButtons);
        AppendMissing(ref missing, _return, Refs.ReturnButton_BWidget);


        if (missing.Length > 0)
        {
            Debug.LogWarning($"[EpisodeSelectionPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}