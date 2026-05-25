using System.Collections.Generic;
using UnityEngine;

public sealed class ChapterCardRuntimeSpawner
{
    private readonly ChapterButtonCardBuilder _builder = new ChapterButtonCardBuilder();

    public ChapterButtonCard CreateCard(
        RectTransform parent,
        RectTransform prefab,
        string rolePrefix,
        string rootName,
        ChapterButtonCardBuildOptions options = null)
    {
        return _builder.BuildCard(parent, prefab, rolePrefix, rootName, options);
    }

    public List<ChapterButtonCard> CreateCards(
        RectTransform parent,
        RectTransform prefab,
        int count,
        ChapterButtonCardBuildOptions options = null)
    {
        List<ChapterButtonCard> cards = new List<ChapterButtonCard>();

        for (int i = 0; i < count; i++)
        {
            string prefix = $"card{i:00}_";
            string rootName = "ChapterButtonCard";

            ChapterButtonCard card = CreateCard(parent, prefab, prefix, rootName, options);

            if (card != null)
                cards.Add(card);
        }

        return cards;
    }
}