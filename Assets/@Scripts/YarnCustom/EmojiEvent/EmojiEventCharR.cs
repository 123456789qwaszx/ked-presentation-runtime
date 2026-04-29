using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity;

public sealed class EmojiEventCharR : ActionMarkupHandler
{
    [System.Serializable]
    public sealed class EmojiEntry
    {
        public string key;
        public Sprite sprite;
    }

    [Header("Emoji Map")]
    [SerializeField] private List<EmojiEntry> emojiEntries = new();

    [Header("Policy")]
    [SerializeField] private float defaultDuration = 1.0f;
    [SerializeField] private bool hideOnLineDismiss = true;

    // 현재 line의 화자명은 바깥(Presenter)에서 넣어준다.
    public string CurrentSpeaker { get; set; }

    // 현재 line의 rig registry도 바깥에서 넣어준다.
    public Dictionary<string, object> RigRegistry { get; set; }

    private readonly Dictionary<string, Sprite> _emojiMap = new();
    private readonly Dictionary<int, string> _emojiEvents = new();

    private Image _targetImage;
    private CoroutineHost _host;
    private Coroutine _hideRoutine;

    private void Awake()
    {
        RebuildEmojiMap();
        _host = GetComponent<CoroutineHost>();
        if (_host == null)
            _host = gameObject.AddComponent<CoroutineHost>();
    }

    private void OnValidate()
    {
        RebuildEmojiMap();
    }

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        _emojiEvents.Clear();
        _targetImage = null;

        if (RigRegistry == null)
            return;

        if (string.IsNullOrEmpty(CurrentSpeaker))
            return;

        if (!RigRegistry.TryGetCharRigRefs(CurrentSpeaker, out CharacterRigRefs rigRefs))
            return;

        _targetImage = rigRefs.CharacterEmoji_Image;
        if (_targetImage == null)
            return;

        foreach (MarkupAttribute attribute in line.Attributes)
        {
            if (attribute.Name != "emoji")
                continue;

            if (!attribute.TryGetProperty("key", out string emojiKey))
                continue;

            _emojiEvents[attribute.Position] = emojiKey;
        }
    }

    public override async YarnTask OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        if (_targetImage == null)
            return;

        if (!_emojiEvents.TryGetValue(currentCharacterIndex, out string emojiKey))
            return;

        if (!_emojiMap.TryGetValue(emojiKey, out Sprite sprite) || sprite == null)
        {
            Debug.LogWarning($"[EmojiEventCharR] No sprite mapped for emoji key '{emojiKey}'.", this);
            return;
        }

        ShowEmoji(sprite);

        if (_hideRoutine != null)
            _host.StopCoroutine(_hideRoutine);

        _hideRoutine = _host.StartCoroutine(HideAfterDelay(defaultDuration));
        await YarnTask.CompletedTask;
    }

    public override void OnLineWillDismiss()
    {
        if (!hideOnLineDismiss)
            return;

        HideEmoji();
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }
    public override void OnLineDisplayComplete() { }

    private void ShowEmoji(Sprite sprite)
    {
        if (_targetImage == null)
            return;

        _targetImage.sprite = sprite;
        _targetImage.enabled = true;
        _targetImage.gameObject.SetActive(true);
    }

    private void HideEmoji()
    {
        if (_hideRoutine != null)
        {
            _host.StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (_targetImage == null)
            return;

        _targetImage.enabled = false;
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_targetImage != null)
            _targetImage.enabled = false;

        _hideRoutine = null;
    }

    private void RebuildEmojiMap()
    {
        _emojiMap.Clear();

        for (int i = 0; i < emojiEntries.Count; i++)
        {
            EmojiEntry entry = emojiEntries[i];
            if (entry == null)
                continue;

            if (string.IsNullOrEmpty(entry.key))
                continue;

            if (entry.sprite == null)
                continue;

            _emojiMap[entry.key] = entry.sprite;
        }
    }

    private sealed class CoroutineHost : MonoBehaviour { }
}