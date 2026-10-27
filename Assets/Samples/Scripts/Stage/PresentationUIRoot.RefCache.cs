using UnityEngine;

public partial class PresentationUIRoot
{
    #region StageViewport Layer Cache

    private RectTransform _stageViewportLayer;

    private RectTransform _stageShotRoot;
    private RectTransform _stagePanRoot;
    private RectTransform _stageZoomRoot;

    private RectTransform _stage00Root;

    private RectTransform _stage00DepthSystemRoot;
    private RectTransform _stage00DepthFarRoot;
    private RectTransform _stage00DepthFarFramingTransform;
    private RectTransform _stage00DepthFarFramingScale;
    private RectTransform _stage00DepthFarContent;

    private RectTransform _stage00DepthBackRoot;
    private RectTransform _stage00DepthBackFramingTransform;
    private RectTransform _stage00DepthBackFramingScale;
    private RectTransform _stage00DepthBackContent;

    private RectTransform _stage00DepthMidRoot;
    private RectTransform _stage00DepthMidFramingTransform;
    private RectTransform _stage00DepthMidFramingScale;
    private RectTransform _stage00DepthMidContent;

    private RectTransform _stage00DepthFrontRoot;
    private RectTransform _stage00DepthFrontFramingTransform;
    private RectTransform _stage00DepthFrontFramingScale;
    private RectTransform _stage00DepthFrontContent;

    private RectTransform _stage00DepthCloseRoot;
    private RectTransform _stage00DepthCloseFramingTransform;
    private RectTransform _stage00DepthCloseFramingScale;
    private RectTransform _stage00DepthCloseContent;

    private RectTransform _stage01Root;

    private RectTransform _stage01DepthSystemRoot;
    private RectTransform _stage01DepthFarRoot;
    private RectTransform _stage01DepthFarFramingTransform;
    private RectTransform _stage01DepthFarFramingScale;
    private RectTransform _stage01DepthFarContent;

    private RectTransform _stage01DepthBackRoot;
    private RectTransform _stage01DepthBackFramingTransform;
    private RectTransform _stage01DepthBackFramingScale;
    private RectTransform _stage01DepthBackContent;

    private RectTransform _stage01DepthMidRoot;
    private RectTransform _stage01DepthMidFramingTransform;
    private RectTransform _stage01DepthMidFramingScale;
    private RectTransform _stage01DepthMidContent;

    private RectTransform _stage01DepthFrontRoot;
    private RectTransform _stage01DepthFrontFramingTransform;
    private RectTransform _stage01DepthFrontFramingScale;
    private RectTransform _stage01DepthFrontContent;

    private RectTransform _stage01DepthCloseRoot;
    private RectTransform _stage01DepthCloseFramingTransform;
    private RectTransform _stage01DepthCloseFramingScale;
    private RectTransform _stage01DepthCloseContent;

    private RectTransform _stage02Root;

    private RectTransform _stage02DepthSystemRoot;
    private RectTransform _stage02DepthFarRoot;
    private RectTransform _stage02DepthFarFramingTransform;
    private RectTransform _stage02DepthFarFramingScale;
    private RectTransform _stage02DepthFarContent;

    private RectTransform _stage02DepthBackRoot;
    private RectTransform _stage02DepthBackFramingTransform;
    private RectTransform _stage02DepthBackFramingScale;
    private RectTransform _stage02DepthBackContent;

    private RectTransform _stage02DepthMidRoot;
    private RectTransform _stage02DepthMidFramingTransform;
    private RectTransform _stage02DepthMidFramingScale;
    private RectTransform _stage02DepthMidContent;

    private RectTransform _stage02DepthFrontRoot;
    private RectTransform _stage02DepthFrontFramingTransform;
    private RectTransform _stage02DepthFrontFramingScale;
    private RectTransform _stage02DepthFrontContent;

    private RectTransform _stage02DepthCloseRoot;
    private RectTransform _stage02DepthCloseFramingTransform;
    private RectTransform _stage02DepthCloseFramingScale;
    private RectTransform _stage02DepthCloseContent;

    #endregion

    #region ScreenEffect Layer Cache

    private RectTransform _screenEffectLayer;

    #endregion

    private void CacheRefs()
    {
        CacheStageViewportRefs();
        CacheScreenEffectRefs();
    }

    // 빠진 칸은 경고로 알린다(에디터·개발 빌드).
    private void ValidateRefs()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateStageViewportRefs();
        ValidateScreenEffectRefs();
#endif
    }

    #region StageViewport Layer

    private void CacheStageViewportRefs()
    {
        _stageViewportLayer = View.Rect(Refs.StageViewportLayer);

        _stageShotRoot = View.Rect(Refs.StageShot_Root);
        _stagePanRoot = View.Rect(Refs.StagePan_Root);
        _stageZoomRoot = View.Rect(Refs.StageZoom_Root);

        _stage00Root = View.Rect(Refs.Stage00_Root);

        _stage00DepthSystemRoot = View.Rect(Refs.Stage00DepthSystem_Root);
        _stage00DepthFarRoot = View.Rect(Refs.Stage00Depth_Far_Root);
        _stage00DepthFarFramingTransform = View.Rect(Refs.Stage00Depth_Far_FramingTransform);
        _stage00DepthFarFramingScale = View.Rect(Refs.Stage00Depth_Far_FramingScale);
        _stage00DepthFarContent = View.Rect(Refs.Stage00Depth_Far_Content);

        _stage00DepthBackRoot = View.Rect(Refs.Stage00Depth_Back_Root);
        _stage00DepthBackFramingTransform = View.Rect(Refs.Stage00Depth_Back_FramingTransform);
        _stage00DepthBackFramingScale = View.Rect(Refs.Stage00Depth_Back_FramingScale);
        _stage00DepthBackContent = View.Rect(Refs.Stage00Depth_Back_Content);

        _stage00DepthMidRoot = View.Rect(Refs.Stage00Depth_Mid_Root);
        _stage00DepthMidFramingTransform = View.Rect(Refs.Stage00Depth_Mid_FramingTransform);
        _stage00DepthMidFramingScale = View.Rect(Refs.Stage00Depth_Mid_FramingScale);
        _stage00DepthMidContent = View.Rect(Refs.Stage00Depth_Mid_Content);

        _stage00DepthFrontRoot = View.Rect(Refs.Stage00Depth_Front_Root);
        _stage00DepthFrontFramingTransform = View.Rect(Refs.Stage00Depth_Front_FramingTransform);
        _stage00DepthFrontFramingScale = View.Rect(Refs.Stage00Depth_Front_FramingScale);
        _stage00DepthFrontContent = View.Rect(Refs.Stage00Depth_Front_Content);

        _stage00DepthCloseRoot = View.Rect(Refs.Stage00Depth_Close_Root);
        _stage00DepthCloseFramingTransform = View.Rect(Refs.Stage00Depth_Close_FramingTransform);
        _stage00DepthCloseFramingScale = View.Rect(Refs.Stage00Depth_Close_FramingScale);
        _stage00DepthCloseContent = View.Rect(Refs.Stage00Depth_Close_Content);

        _stage01Root = View.Rect(Refs.Stage01_Root);

        _stage01DepthSystemRoot = View.Rect(Refs.Stage01DepthSystem_Root);
        _stage01DepthFarRoot = View.Rect(Refs.Stage01Depth_Far_Root);
        _stage01DepthFarFramingTransform = View.Rect(Refs.Stage01Depth_Far_FramingTransform);
        _stage01DepthFarFramingScale = View.Rect(Refs.Stage01Depth_Far_FramingScale);
        _stage01DepthFarContent = View.Rect(Refs.Stage01Depth_Far_Content);

        _stage01DepthBackRoot = View.Rect(Refs.Stage01Depth_Back_Root);
        _stage01DepthBackFramingTransform = View.Rect(Refs.Stage01Depth_Back_FramingTransform);
        _stage01DepthBackFramingScale = View.Rect(Refs.Stage01Depth_Back_FramingScale);
        _stage01DepthBackContent = View.Rect(Refs.Stage01Depth_Back_Content);

        _stage01DepthMidRoot = View.Rect(Refs.Stage01Depth_Mid_Root);
        _stage01DepthMidFramingTransform = View.Rect(Refs.Stage01Depth_Mid_FramingTransform);
        _stage01DepthMidFramingScale = View.Rect(Refs.Stage01Depth_Mid_FramingScale);
        _stage01DepthMidContent = View.Rect(Refs.Stage01Depth_Mid_Content);

        _stage01DepthFrontRoot = View.Rect(Refs.Stage01Depth_Front_Root);
        _stage01DepthFrontFramingTransform = View.Rect(Refs.Stage01Depth_Front_FramingTransform);
        _stage01DepthFrontFramingScale = View.Rect(Refs.Stage01Depth_Front_FramingScale);
        _stage01DepthFrontContent = View.Rect(Refs.Stage01Depth_Front_Content);

        _stage01DepthCloseRoot = View.Rect(Refs.Stage01Depth_Close_Root);
        _stage01DepthCloseFramingTransform = View.Rect(Refs.Stage01Depth_Close_FramingTransform);
        _stage01DepthCloseFramingScale = View.Rect(Refs.Stage01Depth_Close_FramingScale);
        _stage01DepthCloseContent = View.Rect(Refs.Stage01Depth_Close_Content);

        _stage02Root = View.Rect(Refs.Stage02_Root);

        _stage02DepthSystemRoot = View.Rect(Refs.Stage02DepthSystem_Root);
        _stage02DepthFarRoot = View.Rect(Refs.Stage02Depth_Far_Root);
        _stage02DepthFarFramingTransform = View.Rect(Refs.Stage02Depth_Far_FramingTransform);
        _stage02DepthFarFramingScale = View.Rect(Refs.Stage02Depth_Far_FramingScale);
        _stage02DepthFarContent = View.Rect(Refs.Stage02Depth_Far_Content);

        _stage02DepthBackRoot = View.Rect(Refs.Stage02Depth_Back_Root);
        _stage02DepthBackFramingTransform = View.Rect(Refs.Stage02Depth_Back_FramingTransform);
        _stage02DepthBackFramingScale = View.Rect(Refs.Stage02Depth_Back_FramingScale);
        _stage02DepthBackContent = View.Rect(Refs.Stage02Depth_Back_Content);

        _stage02DepthMidRoot = View.Rect(Refs.Stage02Depth_Mid_Root);
        _stage02DepthMidFramingTransform = View.Rect(Refs.Stage02Depth_Mid_FramingTransform);
        _stage02DepthMidFramingScale = View.Rect(Refs.Stage02Depth_Mid_FramingScale);
        _stage02DepthMidContent = View.Rect(Refs.Stage02Depth_Mid_Content);

        _stage02DepthFrontRoot = View.Rect(Refs.Stage02Depth_Front_Root);
        _stage02DepthFrontFramingTransform = View.Rect(Refs.Stage02Depth_Front_FramingTransform);
        _stage02DepthFrontFramingScale = View.Rect(Refs.Stage02Depth_Front_FramingScale);
        _stage02DepthFrontContent = View.Rect(Refs.Stage02Depth_Front_Content);

        _stage02DepthCloseRoot = View.Rect(Refs.Stage02Depth_Close_Root);
        _stage02DepthCloseFramingTransform = View.Rect(Refs.Stage02Depth_Close_FramingTransform);
        _stage02DepthCloseFramingScale = View.Rect(Refs.Stage02Depth_Close_FramingScale);
        _stage02DepthCloseContent = View.Rect(Refs.Stage02Depth_Close_Content);
    }

    private bool ValidateStageViewportRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _stageViewportLayer, Refs.StageViewportLayer);

        AppendMissing(ref missing, _stageShotRoot, Refs.StageShot_Root);
        AppendMissing(ref missing, _stagePanRoot, Refs.StagePan_Root);
        AppendMissing(ref missing, _stageZoomRoot, Refs.StageZoom_Root);

        AppendMissing(ref missing, _stage00Root, Refs.Stage00_Root);
        AppendMissing(ref missing, _stage00DepthSystemRoot, Refs.Stage00DepthSystem_Root);
        AppendMissing(ref missing, _stage00DepthFarRoot, Refs.Stage00Depth_Far_Root);
        AppendMissing(ref missing, _stage00DepthFarFramingTransform, Refs.Stage00Depth_Far_FramingTransform);
        AppendMissing(ref missing, _stage00DepthFarFramingScale, Refs.Stage00Depth_Far_FramingScale);
        AppendMissing(ref missing, _stage00DepthFarContent, Refs.Stage00Depth_Far_Content);

        AppendMissing(ref missing, _stage00DepthBackRoot, Refs.Stage00Depth_Back_Root);
        AppendMissing(ref missing, _stage00DepthBackFramingTransform, Refs.Stage00Depth_Back_FramingTransform);
        AppendMissing(ref missing, _stage00DepthBackFramingScale, Refs.Stage00Depth_Back_FramingScale);
        AppendMissing(ref missing, _stage00DepthBackContent, Refs.Stage00Depth_Back_Content);

        AppendMissing(ref missing, _stage00DepthMidRoot, Refs.Stage00Depth_Mid_Root);
        AppendMissing(ref missing, _stage00DepthMidFramingTransform, Refs.Stage00Depth_Mid_FramingTransform);
        AppendMissing(ref missing, _stage00DepthMidFramingScale, Refs.Stage00Depth_Mid_FramingScale);
        AppendMissing(ref missing, _stage00DepthMidContent, Refs.Stage00Depth_Mid_Content);

        AppendMissing(ref missing, _stage00DepthFrontRoot, Refs.Stage00Depth_Front_Root);
        AppendMissing(ref missing, _stage00DepthFrontFramingTransform, Refs.Stage00Depth_Front_FramingTransform);
        AppendMissing(ref missing, _stage00DepthFrontFramingScale, Refs.Stage00Depth_Front_FramingScale);
        AppendMissing(ref missing, _stage00DepthFrontContent, Refs.Stage00Depth_Front_Content);

        AppendMissing(ref missing, _stage00DepthCloseRoot, Refs.Stage00Depth_Close_Root);
        AppendMissing(ref missing, _stage00DepthCloseFramingTransform, Refs.Stage00Depth_Close_FramingTransform);
        AppendMissing(ref missing, _stage00DepthCloseFramingScale, Refs.Stage00Depth_Close_FramingScale);
        AppendMissing(ref missing, _stage00DepthCloseContent, Refs.Stage00Depth_Close_Content);

        AppendMissing(ref missing, _stage01Root, Refs.Stage01_Root);
        AppendMissing(ref missing, _stage01DepthSystemRoot, Refs.Stage01DepthSystem_Root);
        AppendMissing(ref missing, _stage01DepthFarRoot, Refs.Stage01Depth_Far_Root);
        AppendMissing(ref missing, _stage01DepthFarFramingTransform, Refs.Stage01Depth_Far_FramingTransform);
        AppendMissing(ref missing, _stage01DepthFarFramingScale, Refs.Stage01Depth_Far_FramingScale);
        AppendMissing(ref missing, _stage01DepthFarContent, Refs.Stage01Depth_Far_Content);

        AppendMissing(ref missing, _stage01DepthBackRoot, Refs.Stage01Depth_Back_Root);
        AppendMissing(ref missing, _stage01DepthBackFramingTransform, Refs.Stage01Depth_Back_FramingTransform);
        AppendMissing(ref missing, _stage01DepthBackFramingScale, Refs.Stage01Depth_Back_FramingScale);
        AppendMissing(ref missing, _stage01DepthBackContent, Refs.Stage01Depth_Back_Content);

        AppendMissing(ref missing, _stage01DepthMidRoot, Refs.Stage01Depth_Mid_Root);
        AppendMissing(ref missing, _stage01DepthMidFramingTransform, Refs.Stage01Depth_Mid_FramingTransform);
        AppendMissing(ref missing, _stage01DepthMidFramingScale, Refs.Stage01Depth_Mid_FramingScale);
        AppendMissing(ref missing, _stage01DepthMidContent, Refs.Stage01Depth_Mid_Content);

        AppendMissing(ref missing, _stage01DepthFrontRoot, Refs.Stage01Depth_Front_Root);
        AppendMissing(ref missing, _stage01DepthFrontFramingTransform, Refs.Stage01Depth_Front_FramingTransform);
        AppendMissing(ref missing, _stage01DepthFrontFramingScale, Refs.Stage01Depth_Front_FramingScale);
        AppendMissing(ref missing, _stage01DepthFrontContent, Refs.Stage01Depth_Front_Content);

        AppendMissing(ref missing, _stage01DepthCloseRoot, Refs.Stage01Depth_Close_Root);
        AppendMissing(ref missing, _stage01DepthCloseFramingTransform, Refs.Stage01Depth_Close_FramingTransform);
        AppendMissing(ref missing, _stage01DepthCloseFramingScale, Refs.Stage01Depth_Close_FramingScale);
        AppendMissing(ref missing, _stage01DepthCloseContent, Refs.Stage01Depth_Close_Content);

        AppendMissing(ref missing, _stage02Root, Refs.Stage02_Root);
        AppendMissing(ref missing, _stage02DepthSystemRoot, Refs.Stage02DepthSystem_Root);
        AppendMissing(ref missing, _stage02DepthFarRoot, Refs.Stage02Depth_Far_Root);
        AppendMissing(ref missing, _stage02DepthFarFramingTransform, Refs.Stage02Depth_Far_FramingTransform);
        AppendMissing(ref missing, _stage02DepthFarFramingScale, Refs.Stage02Depth_Far_FramingScale);
        AppendMissing(ref missing, _stage02DepthFarContent, Refs.Stage02Depth_Far_Content);

        AppendMissing(ref missing, _stage02DepthBackRoot, Refs.Stage02Depth_Back_Root);
        AppendMissing(ref missing, _stage02DepthBackFramingTransform, Refs.Stage02Depth_Back_FramingTransform);
        AppendMissing(ref missing, _stage02DepthBackFramingScale, Refs.Stage02Depth_Back_FramingScale);
        AppendMissing(ref missing, _stage02DepthBackContent, Refs.Stage02Depth_Back_Content);

        AppendMissing(ref missing, _stage02DepthMidRoot, Refs.Stage02Depth_Mid_Root);
        AppendMissing(ref missing, _stage02DepthMidFramingTransform, Refs.Stage02Depth_Mid_FramingTransform);
        AppendMissing(ref missing, _stage02DepthMidFramingScale, Refs.Stage02Depth_Mid_FramingScale);
        AppendMissing(ref missing, _stage02DepthMidContent, Refs.Stage02Depth_Mid_Content);

        AppendMissing(ref missing, _stage02DepthFrontRoot, Refs.Stage02Depth_Front_Root);
        AppendMissing(ref missing, _stage02DepthFrontFramingTransform, Refs.Stage02Depth_Front_FramingTransform);
        AppendMissing(ref missing, _stage02DepthFrontFramingScale, Refs.Stage02Depth_Front_FramingScale);
        AppendMissing(ref missing, _stage02DepthFrontContent, Refs.Stage02Depth_Front_Content);

        AppendMissing(ref missing, _stage02DepthCloseRoot, Refs.Stage02Depth_Close_Root);
        AppendMissing(ref missing, _stage02DepthCloseFramingTransform, Refs.Stage02Depth_Close_FramingTransform);
        AppendMissing(ref missing, _stage02DepthCloseFramingScale, Refs.Stage02Depth_Close_FramingScale);
        AppendMissing(ref missing, _stage02DepthCloseContent, Refs.Stage02Depth_Close_Content);

        return LogMissingRefs("StageViewport Layer", missing);
    }

    #endregion

    #region ScreenEffect Layer

    private void CacheScreenEffectRefs()
    {
        _screenEffectLayer = View.Rect(Refs.ScreenEffectLayer);
    }

    private bool ValidateScreenEffectRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _screenEffectLayer, Refs.ScreenEffectLayer);

        return LogMissingRefs("ScreenEffect Layer", missing);
    }

    #endregion

    private static void AppendMissing(ref string acc, Object obj, Refs key)
    {
        if (obj)
            return;

        if (acc.Length > 0)
            acc += "\n";

        acc += $"- {key}";
    }

    private bool LogMissingRefs(string regionName, string missing)
    {
        if (missing.Length <= 0)
            return true;

        Debug.LogWarning($"[PresentationUIRoot] Missing refs in {regionName}:\n{missing}", this);
        return false;
    }
}