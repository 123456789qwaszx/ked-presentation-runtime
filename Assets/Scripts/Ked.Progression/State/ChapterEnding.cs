namespace Ked.Progression
{
    public readonly struct ChapterEnding
    {
        public string ChapterId { get; }
        public string EndingKey { get; }

        public ChapterEnding(string chapterId, string endingKey)
        {
            ChapterId = chapterId;
            EndingKey = endingKey;
        }

        public override string ToString() => $"{ChapterId}:{EndingKey}";
    }
}