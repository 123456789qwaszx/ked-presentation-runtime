using UnityEngine;

public sealed class VnAdvanceInputPoller : MonoBehaviour
{
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;

    public void Initialize(DialogueAdvanceDispatcher dialogueAdvanceDispatcher)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
    }

    private void Update()
    {
        if (_dialogueAdvanceDispatcher == null)
            return;

        bool pressed = false;

        if (Input.GetKeyDown(KeyCode.Space))
            pressed = true;

        if (pressed)
            _dialogueAdvanceDispatcher.DispatchAdvance();
    }
}