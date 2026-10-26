using System.Collections;

public sealed class UIThemePatchAdapter : IUIThemePatchPort
{
    private readonly UIManager _uiManager;
    private readonly UIPatchService _uiPatchService;

    public UIThemePatchAdapter(UIManager uiManager, UIPatchService uiPatchService)
    {
        _uiManager = uiManager;
        _uiPatchService = uiPatchService;
    }

    public IEnumerator PatchCurrentScreen(string themeId, string localeId)
    {
        UIContext context = new UIContext(themeId, localeId);

        yield return _uiPatchService.PatchUIInHierarchy(_uiManager.CurSceneRoot, context);
    }
}