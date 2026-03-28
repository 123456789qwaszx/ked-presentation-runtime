using UnityEngine;

public sealed class VnRawInputPoller : MonoBehaviour
{
    private DialogueAdvanceRouter _dialogueAdvanceRouter;
    public DialogueAdvanceRouter DialogueAdvanceRouter => _dialogueAdvanceRouter;

    public void Initialize(DialogueAdvanceRouter router)
    {
        _dialogueAdvanceRouter = router;
    }

    private void Update()
    {
        if (_dialogueAdvanceRouter == null)
            return;

        bool pressed = false;

        if (Input.GetKeyDown(KeyCode.Space))
            pressed = true;

        if (pressed)
            _dialogueAdvanceRouter.DispatchAdvance();
    }
}