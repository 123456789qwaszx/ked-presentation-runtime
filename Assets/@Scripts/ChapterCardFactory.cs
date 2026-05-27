using System;
using UnityEngine;

public sealed class ChapterCardFactory : MonoBehaviour
{
    [SerializeField] private ChapterCardEntry[] entries = Array.Empty<ChapterCardEntry>();

    public ChapterButtonCardModel[] CreateModels()
    {
        if (entries == null || entries.Length == 0)
            return Array.Empty<ChapterButtonCardModel>();

        ChapterButtonCardModel[] models = new ChapterButtonCardModel[entries.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            ChapterCardEntry entry = entries[i];

            if (entry == null)
            {
                models[i] = ChapterButtonCardModel.Empty();
                continue;
            }

            models[i] = entry.CreateModel();
        }

        return models;
    }
    
    public bool TryGetProgression(int chapterId, out ChapterEpisodeProgressionSO progression)
    {
        progression = null;

        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            ChapterCardEntry entry = entries[i];

            if (entry == null)
                continue;

            if (entry.ChapterId != chapterId)
                continue;

            progression = entry.EpisodeProgression;
            return progression != null;
        }

        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Set Default Chapter Entries")]
    private void SetDefaultEntriesForEditor()
    {
        entries = ChapterCardEntryDefaults.CreateDefaultEntries();

        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Clear Chapter Entries")]
    private void ClearEntriesForEditor()
    {
        entries = Array.Empty<ChapterCardEntry>();

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}