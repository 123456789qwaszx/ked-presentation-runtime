using System.Collections.Generic;

public static class UIInitializer
{
    public static void Run(IEnumerable<UIBase> uis)
    {
        if (uis == null)
            return;

        foreach (UIBase ui in uis)
        {
            if (ui == null)
                continue;

            ui.EnsureInitialized();
        }
    }
}
