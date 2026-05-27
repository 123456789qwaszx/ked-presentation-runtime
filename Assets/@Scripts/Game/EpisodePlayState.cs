using System;

public sealed class EpisodePlayState
{
    
    public int CurrentChapterId { get; private set; } = 1;
    public string SelectedEpisodeId { get; private set; }
    public string CurrentPlayingEpisodeId { get; private set; }

    public bool IsAttachmentMode { get; private set; }
    public string AttachmentEpisodeId { get; private set; }
    public string AttachmentReturnOwnerEpisodeId { get; private set; }

    public void SetCurrentChapter(int chapterId)
    {
        CurrentChapterId = chapterId;
    }

    public void SetSelectedEpisode(string episodeId)
    {
        SelectedEpisodeId = episodeId;
    }

    public void BeginMainEpisode(string episodeId)
    {
        CurrentPlayingEpisodeId = episodeId;
        SelectedEpisodeId = episodeId;

        IsAttachmentMode = false;
        AttachmentEpisodeId = null;
        AttachmentReturnOwnerEpisodeId = null;
    }

    public void BeginAttachmentEpisode(string ownerEpisodeId, string targetEpisodeId)
    {
        CurrentPlayingEpisodeId = targetEpisodeId;
        SelectedEpisodeId = ownerEpisodeId;

        IsAttachmentMode = true;
        AttachmentEpisodeId = targetEpisodeId;
        AttachmentReturnOwnerEpisodeId = ownerEpisodeId;
    }

    public bool IsCurrentAttachmentRun(string episodeId)
    {
        return IsAttachmentMode
               && !string.IsNullOrEmpty(AttachmentEpisodeId)
               && string.Equals(AttachmentEpisodeId, episodeId, StringComparison.Ordinal);
    }

    public string ResolveReturnSelectedEpisodeId(string fallbackEpisodeId)
    {
        return !string.IsNullOrEmpty(AttachmentReturnOwnerEpisodeId)
            ? AttachmentReturnOwnerEpisodeId
            : fallbackEpisodeId;
    }

    public void ClearAttachmentContext()
    {
        IsAttachmentMode = false;
        AttachmentEpisodeId = null;
        AttachmentReturnOwnerEpisodeId = null;
    }
    
    public void ApplyEpisodeState(string episodeId)
    {
        if (IsAttachmentMode)
        {
            SetSelectedEpisode(
                !string.IsNullOrEmpty(AttachmentReturnOwnerEpisodeId) 
                    ? AttachmentReturnOwnerEpisodeId
                    : episodeId);

            ClearAttachmentContext();
        }
        
        SetSelectedEpisode(episodeId);
    }
}