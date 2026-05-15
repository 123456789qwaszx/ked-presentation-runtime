using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class EpisodeNodeView : UIBase<EpisodeNodeView.Refs>
{
    public event Action<string> OnMainCardClicked;
    public event Action<string, LinkKind, string> OnBranchNodeClicked;


    #region Refs
    
    public enum Refs
    {
        Timeline_Root,
        TimelineBG_Image,
        TimelineEra_Text,
        TimelineCursorIcon_Image,

        EpisodeNodeSelectZone_Root,
        EpisodeNodeSelectZoneBG_Image,

        UpperAttachment_Root,
        UpperAttachmentBG_Image,
        UpperAttachmentTitle_Root,
        UpperAttachmentTitle_Text,
        UpperAttachmentHit_Button,

        LowerAttachment_Root,
        LowerAttachmentBG_Image,
        LowerAttachmentTitle_Root,
        LowerAttachmentTitle_Text,
        LowerAttachmentHit_Button,

        MainCard_Root,
        MainCardBG_Image,
        MainCardIndex_Root,
        MainCardIndexText_Text,
        MainCardIndexIcon_Image,

        MainCardTitleText_Root,
        MainCardTitleText_Text,

        MainCardHit_Button,
    }

    // Cached UI Refs
    private Button _mainHit;
    private Button _upperHit;
    private Button _lowerHit;

    private TMP_Text _indexText;
    private TMP_Text _titleText;
    
    private RectTransform _upperRoot;
    private TMP_Text _upperTitleText;
    
    private RectTransform  _lowerRoot;
    private TMP_Text _lowerTitleText;
    
    #endregion

    // Runtime State
    private string _upperTarget = "";
    private string _lowerTarget = "";
    private string _episodeId = "";
    
    private bool _valid;
    
    protected override void OnInitialize()
    {
        _mainHit  = View.Button(Refs.MainCardHit_Button);
        _upperHit = View.Button(Refs.UpperAttachmentHit_Button);
        _lowerHit = View.Button(Refs.LowerAttachmentHit_Button);

        _indexText = View.Text(Refs.MainCardIndexText_Text);
        _titleText = View.Text(Refs.MainCardTitleText_Text);
        
        _upperRoot      = View.Rect(Refs.UpperAttachment_Root);
        _upperTitleText = View.Text(Refs.UpperAttachmentTitle_Text);
        
        _lowerRoot      = View.Rect(Refs.LowerAttachment_Root);
        _lowerTitleText = View.Text(Refs.LowerAttachmentTitle_Text);
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
_valid = true;
#endif
        
        BindEvent(_mainHit,  OnMainUp);
        BindEvent(_upperHit, OnUpperUp);
        BindEvent(_lowerHit, OnLowerUp);
    }

    #region Present

    public void Present(in EpisodeNodeModel episode)
    {
        if (!_valid) return;
        
        _upperTarget = "";
        _lowerTarget = "";
        _upperHit.interactable = false;
        _lowerHit.interactable = false;
        
        _indexText.text = episode.IndexText;
        _titleText.text = episode.Title;
        _mainHit.interactable = episode.Interactable && !episode.Locked;
        _episodeId = episode.EpisodeId;
        
        bool hasUpper = episode.UpperAttachment.HasValue;
        bool hasLower = episode.LowerAttachment.HasValue;
        
        _upperRoot.gameObject.SetActive(hasUpper);
        _lowerRoot.gameObject.SetActive(hasLower);
        
        if (hasUpper)
        {
            EpisodeAttachmentModel upper = episode.UpperAttachment.Value;
            
            _upperTitleText.text = upper.DisplayTitle;
            _upperHit.interactable = upper.IsInteractable;
            _upperTarget = upper.HostEpisodeId;
        }
        
        if (hasLower)
        {
            EpisodeAttachmentModel lower = episode.LowerAttachment.Value;
            
            _lowerTitleText.text = lower.DisplayTitle;
            _lowerHit.interactable = lower.IsInteractable;
            _lowerTarget = lower.HostEpisodeId;
            
            if (!lower.IsInteractable)
                _lowerTitleText.text = "[잠금]" + lower.DisplayTitle;
        }
        
        // TODO: selected/locked 시각처리
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnMainUp(PointerEventData _)
    {
        if (!_valid) return;
        if (string.IsNullOrEmpty(_episodeId)) return;

        OnMainCardClicked?.Invoke(_episodeId);
    }

    private void OnUpperUp(PointerEventData _)
    {
        if (!_valid) return;
        if (string.IsNullOrEmpty(_episodeId)) return;
        if (string.IsNullOrEmpty(_upperTarget)) return;

        OnBranchNodeClicked?.Invoke(_episodeId, LinkKind.BranchUpper, _upperTarget);
    }

    private void OnLowerUp(PointerEventData _)
    {
        if (!_valid) return;
        if (string.IsNullOrEmpty(_episodeId)) return;
        if (string.IsNullOrEmpty(_lowerTarget)) return;

        OnBranchNodeClicked?.Invoke(_episodeId, LinkKind.BranchLower, _lowerTarget);
    }
    
    #endregion
    
    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _mainHit,        Refs.MainCardHit_Button);
        AppendMissing(ref missing, _upperHit,       Refs.UpperAttachmentHit_Button);
        AppendMissing(ref missing, _lowerHit,       Refs.LowerAttachmentHit_Button);

        AppendMissing(ref missing, _indexText,      Refs.MainCardIndexText_Text);
        AppendMissing(ref missing, _titleText,      Refs.MainCardTitleText_Text);

        AppendMissing(ref missing, _upperRoot,      Refs.UpperAttachment_Root);
        AppendMissing(ref missing, _upperTitleText, Refs.UpperAttachmentTitle_Text);

        AppendMissing(ref missing, _lowerRoot,      Refs.LowerAttachment_Root);
        AppendMissing(ref missing, _lowerTitleText, Refs.LowerAttachmentTitle_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[EpisodeNodeView] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}