using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterEmojiAnchor : MonoBehaviour
{
    [System.Serializable]
    public sealed class EmojiEntry
    {
        public string key;
        public Sprite sprite;
    }

    [Header("Refs")]
    [SerializeField] private Transform emojiAnchor;
    [SerializeField] private EmojiPopupView emojiPopupPrefab;

    [Header("Emoji Map")]
    [SerializeField] private List<EmojiEntry> emojis = new();

    [Header("Policy")]
    [SerializeField] private float defaultDuration = 1.2f;

    private readonly Dictionary<string, Sprite> _emojiMap = new();
    private EmojiPopupView _activePopup;
    private Coroutine _hideRoutine;

    private void Awake()
    {
        _emojiMap.Clear();

        for (int i = 0; i < emojis.Count; i++)
        {
            EmojiEntry entry = emojis[i];
            if (entry == null)
                continue;

            if (string.IsNullOrEmpty(entry.key))
                continue;

            if (_emojiMap.ContainsKey(entry.key))
                continue;

            _emojiMap.Add(entry.key, entry.sprite);
        }
    }

    public void ShowEmoji(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (!_emojiMap.TryGetValue(key, out Sprite sprite) || sprite == null)
        {
            Debug.LogWarning($"[CharacterEmojiAnchor] No emoji sprite mapped for key '{key}'.", this);
            return;
        }

        if (emojiAnchor == null)
        {
            Debug.LogWarning("[CharacterEmojiAnchor] emojiAnchor is null.", this);
            return;
        }

        EnsurePopup();

        _activePopup.Show(sprite);

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _hideRoutine = StartCoroutine(HideAfterDelay(defaultDuration));
    }

    public void HideEmoji()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (_activePopup != null)
            _activePopup.Hide();
    }

    private void EnsurePopup()
    {
        if (_activePopup != null)
            return;

        if (emojiPopupPrefab == null)
        {
            Debug.LogWarning("[CharacterEmojiAnchor] emojiPopupPrefab is null.", this);
            return;
        }

        _activePopup = Instantiate(emojiPopupPrefab, emojiAnchor);
        _activePopup.transform.localPosition = Vector3.zero;
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_activePopup != null)
            _activePopup.Hide();

        _hideRoutine = null;
    }
}