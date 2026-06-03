using UnityEngine;
using Yarn.Unity;

// Owns the option-box visual lifetime (show transition, hide, abort cleanup), kept separate from the
// flow exactly as DialogueBoxPresentationController is for lines.
//
// This stub establishes the contract and the await points. The actual transition work
// (fade, box variant, character anchoring) is intentionally left for the project to fill in.

public sealed class VNOptionsBoxPresentationController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _itemContainer;

    public async YarnTask<VNOptionsBoxPresentationResult> ShowOptionsAsync(VNOptionsBoxPresentationOptions options)
    {
        if (_itemContainer == null)
            return VNOptionsBoxPresentationResult.Invalid();

        // TODO: branch on options.Style / options.AnchorCharacterName to resolve the target box + container.
        // TODO: run the fade-in (or cut, when options.UseImmediateTransition) and await its completion here.
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        await YarnTask.Yield();

        return VNOptionsBoxPresentationResult.Ready(_itemContainer);
    }

    public void HideImmediate()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    // Called when the run aborts after the box was shown but before a normal selection commit.
    public void CleanupAborted(VNOptionsBoxPresentationResult result)
    {
        // TODO: cancel any in-flight transition tied to this result before hiding.
        HideImmediate();
    }
}