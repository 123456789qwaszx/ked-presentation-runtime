using System.Collections.Generic;
using UnityEngine;

public sealed class ChapterCardRuntimeSpawner
{
    private readonly ChapterButtonCardBuilder _builder = new ChapterButtonCardBuilder();

    public ChapterButtonCard CreateCard(
        RectTransform parent,
        RectTransform prefab,
        string rolePrefix,
        string rootName)
    {
        RectTransform root = _builder.BuildCardRigRoot(
            prefab,
            rolePrefix,
            rootName);

        root.SetParent(parent, false);

        ChapterButtonCard card = root.GetComponent<ChapterButtonCard>();

        if (card == null)
            card = root.gameObject.AddComponent<ChapterButtonCard>();

        return card;
    }

    public List<ChapterButtonCard> CreateCards(
        RectTransform parent,
        RectTransform prefab,
        int count)
    {
        List<ChapterButtonCard> cards = new List<ChapterButtonCard>();

        for (int i = 0; i < count; i++)
        {
            string prefix = $"card{i:00}_";
            string rootName = "ChapterButtonCard";

            ChapterButtonCard card = CreateCard(parent, prefab, prefix, rootName);

            if (card != null)
                cards.Add(card);
        }

        return cards;
    }
}