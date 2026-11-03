public partial class DialogueBoxPresentationController
{
    // surface_layout is a persistent presentation state mutation.
    // It is intentionally not applied to the currently committed box here,
    // because front-matter commands for the next line may run while the previous
    // line is still visible. The layout is applied deterministically at ShowLineAsync,
    // before PrimeText.
    public void SetSurfaceLayout(string presetKey)
    {
        _surfaceState.SetLayout(presetKey);
    }

    public void ResetSurfaceLayout()
    {
        _surfaceState.Reset();
    }
}
