using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AlbumDatabase",
    menuName = "VN/Album/Album Database")]
public sealed class VNAlbumDatabaseSO : ScriptableObject
{
    [SerializeField]
    private List<VNAlbumItemSO> items = new();

    private Dictionary<string, VNAlbumItemSO> _itemsByKey;

    public IReadOnlyList<VNAlbumItemSO> Items => items;

    private void OnEnable()
    {
        RebuildCache();
    }

    public bool TryGet(
        string key,
        out VNAlbumItemSO item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_itemsByKey == null)
            RebuildCache();

        return _itemsByKey.TryGetValue(
            key,
            out item);
    }

    private void RebuildCache()
    {
        _itemsByKey = new Dictionary<string, VNAlbumItemSO>();

        for (int i = 0; i < items.Count; i++)
        {
            VNAlbumItemSO item = items[i];

            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.Key))
            {
                Debug.LogWarning(
                    $"[VNAlbumDatabaseSO] Empty key. index={i}",
                    this);

                continue;
            }

            if (!_itemsByKey.TryAdd(
                    item.Key,
                    item))
            {
                Debug.LogWarning(
                    $"[VNAlbumDatabaseSO] Duplicate key. key={item.Key}",
                    this);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCache();
    }
#endif
}