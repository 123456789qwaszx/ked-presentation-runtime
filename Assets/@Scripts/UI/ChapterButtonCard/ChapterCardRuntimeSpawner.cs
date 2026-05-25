using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ChapterCardRuntimeSpawner
{
    public ChapterButtonCard CreateCard(
        RectTransform parent,
        RectTransform prefab,
        string rolePrefix,
        string rootName)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ChapterCardRuntimeSpawner] Prefab is null.");
            return null;
        }

        RectTransform instance = Object.Instantiate(prefab);

        if (!string.IsNullOrWhiteSpace(rootName))
            instance.name = WithRole(rolePrefix, rootName);

        if (parent != null)
            instance.SetParent(parent, false);
        else
            Debug.LogWarning("[ChapterCardRuntimeSpawner] Parent is null.", instance);

        ChapterButtonCard card = instance.GetComponent<ChapterButtonCard>();

        if (card == null)
        {
            Debug.LogWarning(
                "[ChapterCardRuntimeSpawner] Prefab does not have ChapterButtonCard component.",
                instance);

            return null;
        }

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

            ChapterButtonCard card = CreateCard(
                parent,
                prefab,
                prefix,
                "ChapterButtonCard");

            if (card != null)
                cards.Add(card);
        }

        return cards;
    }

    private static string WithRole(string rolePrefix, string baseName)
    {
        if (string.IsNullOrEmpty(rolePrefix))
            return baseName;

        if (baseName.StartsWith(rolePrefix, System.StringComparison.Ordinal))
            return baseName;

        return $"{rolePrefix}{baseName}";
    }
}