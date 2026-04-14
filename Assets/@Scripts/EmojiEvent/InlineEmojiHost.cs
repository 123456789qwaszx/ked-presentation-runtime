using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct EmojiCueMapEntry
{
    public string cue;
    public string emojiKey;
}

public sealed class InlineEmojiHost : MonoBehaviour, InlineEventMarkupHandler.IInlineEmojiHost
{
    [Header("Emoji Database")]
    [SerializeField] private CharacterEmojiDatabaseSO database;

    [Header("Cue -> EmojiKey")]
    [SerializeField] private List<EmojiCueMapEntry> cueMap = new();

    [Header("Policy")]
    [SerializeField] private bool hideIfKeyEmpty = true;
    [SerializeField] private float defaultHideDelay = 1.0f;
    [SerializeField] private Ease fadeEase = Ease.OutCubic;

    private Dictionary<string, object> _rigRegistry;
    private Dictionary<string, string> _cueToEmojiKey;

    private string _currentSpeaker;

    private readonly Dictionary<string, Sequence> _activeSeqByRole = new();

    public void Initialize(Dictionary<string, object> rigRegistry)
    {
        _rigRegistry = rigRegistry;
        RebuildMap();
    }

    public void SetCurrentSpeaker(string speaker)
    {
        _currentSpeaker = speaker ?? "";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildMap();
    }
#endif

    public void PlayEmojiCue(string cue)
    {
        if (hideIfKeyEmpty && string.IsNullOrWhiteSpace(cue))
        {
            HideCurrentSpeakerEmoji();
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentSpeaker))
        {
            Debug.LogWarning("[InlineEmojiHost] Current speaker is empty.");
            return;
        }

        if (_rigRegistry == null)
        {
            Debug.LogWarning("[InlineEmojiHost] Rig registry is null.");
            return;
        }

        if (!_rigRegistry.TryGetCharRigRefs(_currentSpeaker, out CharacterRigRefs rig) || rig == null)
        {
            Debug.LogWarning($"[InlineEmojiHost] Failed to find CharRigRefs. speaker={_currentSpeaker}", this);
            return;
        }

        string emojiKey = ResolveCueToEmojiKey(cue);

        if (string.IsNullOrWhiteSpace(emojiKey))
        {
            Debug.LogWarning($"[InlineEmojiHost] Emoji key is empty. cue={cue}", this);
            return;
        }

        if (database == null || !database.TryGet(emojiKey.Trim(), out CharacterEmojiPresetSO preset) || preset == null)
        {
            Debug.LogWarning($"[InlineEmojiHost] Failed to resolve emoji preset. cue={cue}, emojiKey={emojiKey}", this);
            return;
        }

        ShowEmoji(rig, preset, _currentSpeaker);
    }

    public void HideCurrentSpeakerEmoji()
    {
        if (string.IsNullOrWhiteSpace(_currentSpeaker))
            return;

        if (_rigRegistry == null)
            return;

        if (!_rigRegistry.TryGetCharRigRefs(_currentSpeaker, out CharacterRigRefs rig) || rig == null)
            return;

        HideEmojiImmediate(rig, _currentSpeaker);
    }

    private void ShowEmoji(CharacterRigRefs rig, CharacterEmojiPresetSO preset, string roleKey)
    {
        RectTransform emojiRoot = rig.CharacterEmoji_Root;
        RectTransform emojiAnchor = rig.CharacterEmoji_Anchor;
        Image emojiImg = rig.CharacterEmoji_Image;

        if (emojiRoot == null || emojiAnchor == null || emojiImg == null)
        {
            Debug.LogWarning($"[InlineEmojiHost] Missing emoji refs. roleKey={roleKey}", this);
            return;
        }

        CanvasGroup cg = GetOrAddCanvasGroup(emojiRoot);

        if (!emojiRoot.gameObject.activeSelf)
            emojiRoot.gameObject.SetActive(true);

        if (!emojiAnchor.gameObject.activeSelf)
            emojiAnchor.gameObject.SetActive(true);

        cg.DOKill(false);

        if (_activeSeqByRole.TryGetValue(roleKey, out Sequence oldSeq) && oldSeq != null)
        {
            oldSeq.Kill(false);
        }

        KillPersistentOn(emojiAnchor);

        emojiImg.sprite = preset.sprite;
        emojiImg.preserveAspect = true;
        emojiImg.enabled = true;

        ApplyLayout(emojiAnchor, preset.layout);

        float fadeIn = Mathf.Max(0f, preset.fadeIn);
        float visibleDelay = Mathf.Max(0f, defaultHideDelay);
        float fadeOut = Mathf.Max(0f, preset.fadeOut);

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (fadeIn <= 0f)
        {
            cg.alpha = 1f;
            ApplyPersistent(preset, emojiAnchor);
        }
        else
        {
            cg.alpha = 0f;
            seq.Append(cg.DOFade(1f, fadeIn).SetEase(fadeEase));
            seq.AppendCallback(() => ApplyPersistent(preset, emojiAnchor));
        }

        if (visibleDelay > 0f)
            seq.AppendInterval(visibleDelay);

        if (fadeOut > 0f)
        {
            seq.Append(cg.DOFade(0f, fadeOut).SetEase(fadeEase));
        }
        else
        {
            seq.AppendCallback(() => cg.alpha = 0f);
        }

        seq.OnComplete(() =>
        {
            KillPersistentOn(emojiAnchor);
            _activeSeqByRole.Remove(roleKey);
        });

        _activeSeqByRole[roleKey] = seq;
    }

    private string ResolveCueToEmojiKey(string cue)
    {
        if (_cueToEmojiKey == null)
        {
            Debug.LogWarning($"[InlineEmojiHost] Cue map is null. Falling back to raw cue as emojiKey. cue={cue}");
            return cue;
        }

        if (_cueToEmojiKey.TryGetValue(cue, out string emojiKey))
            return emojiKey;

        return cue;
    }

    private void HideEmojiImmediate(CharacterRigRefs rig, string roleKey)
    {
        if (rig.CharacterEmoji_Root == null)
            return;

        if (_activeSeqByRole.TryGetValue(roleKey, out Sequence oldSeq) && oldSeq != null)
        {
            oldSeq.Kill(false);
            _activeSeqByRole.Remove(roleKey);
        }

        CanvasGroup cg = rig.CharacterEmoji_Root.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 0f;

        if (rig.CharacterEmoji_Anchor != null)
            KillPersistentOn(rig.CharacterEmoji_Anchor);

        if (rig.CharacterEmoji_Image != null)
            rig.CharacterEmoji_Image.enabled = false;
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform root)
    {
        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg != null)
            return cg;

        return root.gameObject.AddComponent<CanvasGroup>();
    }

    private static void ApplyLayout(RectTransform anchor, CharacterEmojiPresetSO.EmojiLayout layout)
    {
        anchor.anchorMin = layout.anchorMin;
        anchor.anchorMax = layout.anchorMax;
        anchor.anchoredPosition = layout.anchoredPos;

        Vector3 scale = anchor.localScale;
        scale.x = layout.scale.x;
        scale.y = layout.scale.y;
        anchor.localScale = scale;

        Vector3 euler = anchor.localEulerAngles;
        euler.z = layout.rotationZ;
        anchor.localEulerAngles = euler;
    }

    private static void KillPersistentOn(RectTransform anchor)
    {
        if (anchor == null)
            return;

        anchor.DOKill(false);
    }

    private static void ApplyPersistent(CharacterEmojiPresetSO preset, RectTransform anchor)
    {
        if (preset == null || anchor == null)
            return;

        CharacterEmojiPresetSO.EffectParams p = preset.effectParams;

        switch (preset.effect)
        {
            case CharacterEmojiPresetSO.EmojiPersistentEffect.None:
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Sway:
                anchor.DOLocalRotate(
                        new Vector3(0f, 0f, preset.layout.rotationZ + 8f * p.amp),
                        0.6f / Mathf.Max(0.0001f, p.freq))
                    .SetEase(Ease.InOutSine)
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Yoyo)
                    .SetUpdate(true);
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Bob:
                anchor.DOLocalMoveY(
                        anchor.localPosition.y + 12f * p.amp,
                        0.5f / Mathf.Max(0.0001f, p.freq))
                    .SetEase(Ease.InOutSine)
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Yoyo)
                    .SetUpdate(true);
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Shake:
                anchor.DOShakeRotation(
                        0.6f / Mathf.Max(0.0001f, p.freq),
                        new Vector3(0f, 0f, 6f * p.amp),
                        6,
                        60f)
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Restart)
                    .SetUpdate(true);
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Pulse:
                anchor.DOPunchScale(
                        new Vector3(0.08f * p.amp, 0.08f * p.amp, 0f),
                        0.5f / Mathf.Max(0.0001f, p.freq),
                        6,
                        0.7f)
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Restart)
                    .SetUpdate(true);
                break;
        }
    }

    private void RebuildMap()
    {
        _cueToEmojiKey = new Dictionary<string, string>(StringComparer.Ordinal);

        if (cueMap == null)
            return;

        for (int i = 0; i < cueMap.Count; i++)
        {
            EmojiCueMapEntry entry = cueMap[i];

            if (string.IsNullOrWhiteSpace(entry.cue))
                continue;

            _cueToEmojiKey[entry.cue] = entry.emojiKey;
        }
    }
}