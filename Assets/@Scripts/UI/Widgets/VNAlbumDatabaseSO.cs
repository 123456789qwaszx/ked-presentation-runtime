using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AlbumDatabase", menuName = "VN/Album/Album Database")]
public sealed class VNAlbumDatabaseSO : ScriptableObject
{
    [SerializeField] private List<VNAlbumItemSO> _items = new List<VNAlbumItemSO>();

    private Dictionary<string, VNAlbumItemSO> _cache;

    public IReadOnlyList<VNAlbumItemSO> Items => _items;

    private void OnEnable()
    {
        RebuildCache();
    }

    public VNAlbumItemSO FindByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (_cache == null)
            RebuildCache();

        _cache.TryGetValue(key, out VNAlbumItemSO item);
        return item;
    }

    public void RebuildCache()
    {
        _cache = new Dictionary<string, VNAlbumItemSO>();

        for (int i = 0; i < _items.Count; i++)
        {
            VNAlbumItemSO item = _items[i];

            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.key))
            {
                Debug.LogWarning($"[VNAlbumDatabaseSO] Empty key at index {i}.", this);
                continue;
            }

            if (_cache.ContainsKey(item.key))
            {
                Debug.LogWarning($"[VNAlbumDatabaseSO] Duplicate key '{item.key}' at index {i}.", this);
                continue;
            }

            _cache.Add(item.key, item);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCache();
    }
#endif
}