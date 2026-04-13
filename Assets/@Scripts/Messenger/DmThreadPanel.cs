using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class DmThreadPanel : UIPanel<DmThreadPanel.Refs>
{
    #region Refs

    public enum Refs
    {
        Pnl_DmThread,
        ThreadScrollRect_ScrollRect,
        ThreadContent_Root,
        OptionsContent_Root,
    }

    private CanvasGroup _rootCg;
    private ScrollRect _scroll;
    private RectTransform _bubbleContent;
    private RectTransform _optionsContent;

    protected override void Initialize()
    {
        _rootCg = View.CanvasGroup(Refs.Pnl_DmThread);

        RectTransform scrollRt = View.Rect(Refs.ThreadScrollRect_ScrollRect);
        _scroll = scrollRt.GetComponent<ScrollRect>();

        _bubbleContent = View.Rect(Refs.ThreadContent_Root);
        _optionsContent = View.Rect(Refs.OptionsContent_Root);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif
    }

    private bool ValidateRefs()
    {
        string missing = "";
        AppendMissing(ref missing, _rootCg, Refs.Pnl_DmThread);
        AppendMissing(ref missing, _scroll, Refs.ThreadScrollRect_ScrollRect);
        AppendMissing(ref missing, _bubbleContent, Refs.ThreadContent_Root);
        AppendMissing(ref missing, _optionsContent, Refs.OptionsContent_Root);

        if (missing.Length > 0)
            Debug.LogWarning($"[DmThreadPanel] Missing refs:\n{missing}", this);

        return missing.Length == 0;
    }

    #endregion

    private readonly List<GameObject> _activeBubbles = new();
    private readonly List<GameObject> _activeOptions = new();

    private bool _valid;

    public MessengerBubbleView AppendBubble(MessengerBubbleView prefab, string speaker, string text)
    {
        if (!_valid || prefab == null)
            return null;

        MessengerBubbleView view = Instantiate(prefab, _bubbleContent);
        view.ShowText(speaker, text);

        _activeBubbles.Add(view.gameObject);
        return view;
    }

    public MessengerBubbleView AppendTypingBubble(MessengerBubbleView prefab, string speaker)
    {
        if (!_valid || prefab == null)
            return null;

        MessengerBubbleView view = Instantiate(prefab, _bubbleContent);
        view.ShowTyping(speaker);

        _activeBubbles.Add(view.gameObject);
        return view;
    }

    public MessengerOptionButtonView AppendOption(
        MessengerOptionButtonView prefab,
        string text,
        System.Action onClick)
    {
        if (!_valid || prefab == null)
            return null;

        MessengerOptionButtonView view = Instantiate(prefab, _optionsContent);
        view.SetText(text);
        view.SetOnClick(onClick);

        _activeOptions.Add(view.gameObject);
        return view;
    }

    public void RemoveBubble(MessengerBubbleView bubble)
    {
        if (!_valid || bubble == null)
            return;

        _activeBubbles.Remove(bubble.gameObject);
        Destroy(bubble.gameObject);
    }

    public void ClearBubbles()
    {
        if (!_valid)
            return;

        for (int i = 0; i < _activeBubbles.Count; i++)
        {
            GameObject go = _activeBubbles[i];
            if (go != null)
                Destroy(go);
        }

        _activeBubbles.Clear();
        ScrollToBottom();
    }

    public void ClearOptions()
    {
        if (!_valid)
            return;

        for (int i = 0; i < _activeOptions.Count; i++)
        {
            GameObject go = _activeOptions[i];
            if (go != null)
                Destroy(go);
        }

        _activeOptions.Clear();
        ScrollToBottom();
    }

    public void LockOptions()
    {
        if (!_valid)
            return;

        for (int i = 0; i < _activeOptions.Count; i++)
        {
            GameObject go = _activeOptions[i];
            if (go == null)
                continue;

            MessengerOptionButtonView button = go.GetComponent<MessengerOptionButtonView>();
            if (button != null)
                button.SetInteractable(false);
        }
    }

    public void ClearThread()
    {
        if (!_valid)
            return;

        ClearBubbles();
        ClearOptions();
    }

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollNextFrame());
    }

    private IEnumerator ScrollNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        _scroll.verticalNormalizedPosition = 0f;
    }
}