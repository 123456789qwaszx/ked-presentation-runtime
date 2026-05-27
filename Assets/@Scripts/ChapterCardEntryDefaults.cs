public static class ChapterCardEntryDefaults
{
    public static ChapterCardEntry[] CreateDefaultEntries()
    {
        return new[]
        {
            new ChapterCardEntry(
                chapterId: 0,
                indexText: "01",
                chapterIndexLabel: "챕터 01",
                chapterTitle: "Stella Sora",
                episodeHeading: "01 First Broadcast",
                interactable: true,
                locked: false),

            new ChapterCardEntry(
                chapterId: 1,
                indexText: "02",
                chapterIndexLabel: "챕터 02",
                chapterTitle: "Signal Noise",
                episodeHeading: "02 Unread Message",
                interactable: true,
                locked: false),

            new ChapterCardEntry(
                chapterId: 2,
                indexText: "03",
                chapterIndexLabel: "챕터 03",
                chapterTitle: "Broadcast Fever",
                episodeHeading: "03 Locked Route",
                interactable: false,
                locked: true),
        };
    }
}