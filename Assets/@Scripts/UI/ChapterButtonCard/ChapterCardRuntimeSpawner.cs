using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IChapterCardRootProvider
{
    RectTransform ChapterCardRoot { get; }
}
public sealed partial class ChapterSelectionPanel : IChapterCardRootProvider
{
    public RectTransform ChapterCardRoot => View?.Rect(Refs.ChapterButtons);
}

public sealed class ChapterCardRuntimeSpawner
{
    private IChapterCardRootProvider RootProvider => UIManager.Instance.GetUI<ChapterSelectionPanel>();
    
    public List<ChapterButtonCard> CreateCards(RectTransform prefab, int count)
    {
        List<ChapterButtonCard> cards = new List<ChapterButtonCard>();
        
        if (prefab == null)
        {
            Debug.LogWarning("[ChapterCardRuntimeSpawner] Prefab is null.");
            return cards;
        }

        RectTransform parent = RootProvider?.ChapterCardRoot;

        for (int i = 0; i < count; i++)
        {
            ChapterButtonCard card = CreateCard(parent, prefab);

            if (card != null)
                cards.Add(card);
        }

        return cards;
    }
    
    private ChapterButtonCard CreateCard(RectTransform parent, RectTransform prefab)
    {
        RectTransform instance = Object.Instantiate(prefab, parent, false);

        ChapterButtonCard card = instance.GetComponent<ChapterButtonCard>();

        if (card == null)
        {
            Debug.LogWarning("[ChapterCardRuntimeSpawner] Prefab does not have ChapterButtonCard component.", instance);
            return null;
        }

        return card;
    }
}