using UnityEngine;

public sealed class VnRawInputPoller : MonoBehaviour
{
    private DialogueAdvanceRouter _router;

    public void Initialize(DialogueAdvanceRouter router)
    {
        _router = router;
    }

    private void Update()
    {
        if (_router == null)
            return;

        bool pressed = false;

        if (Input.GetKeyDown(KeyCode.Space))
            pressed = true;

        if (pressed)
            _router.DispatchAdvance();
    }
}