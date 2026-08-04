using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IChapterCardRootProvider
{
    RectTransform ChapterCardRoot { get; }
}

public sealed class ChapterCardRuntimeSpawner
{
    private readonly IChapterCardRootProvider _rootProvider;

    public ChapterCardRuntimeSpawner(IChapterCardRootProvider rootProvider)
    {
        _rootProvider = rootProvider;
    }
    
    public List<ChapterButtonCard> CreateCards(RectTransform prefab, int count)
    {
        List<ChapterButtonCard> cards = new List<ChapterButtonCard>();
        
        if (prefab == null)
        {
            Debug.LogWarning("[ChapterCardRuntimeSpawner] Prefab is null.");
            return cards;
        }

        RectTransform parent = _rootProvider?.ChapterCardRoot;

        if (parent == null)
            Debug.LogError("[ChapterCardRuntimeSpawner] ChapterCardRoot is null.");

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

        // 프리팹이 비활성으로 저장되거나 부모가 비활성이면 Awake가 지연.
        // 활성 생성 전제를 없애기 위해 반환 전에 초기화를 보장.
        card.EnsureInitialized();

        return card;
    }
}
