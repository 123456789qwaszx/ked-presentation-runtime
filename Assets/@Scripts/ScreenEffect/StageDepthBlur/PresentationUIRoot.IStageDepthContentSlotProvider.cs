using UnityEngine;

public interface IStageDepthContentSlotProvider
{
    RectTransform GetDepthContent(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer);
}

public sealed partial class PresentationUIRoot : IStageDepthContentSlotProvider
{
    private RectTransform[][] _stageDepthContentSlots;

    public RectTransform GetDepthContent(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer)
        => _stageDepthContentSlots[(int)stage][(int)layer];

    private void CacheStageDepthContentSlotProviderRefs()
    {
        _stageDepthContentSlots = new RectTransform[PresentationStageCount][];

        _stageDepthContentSlots[(int)PresentationStageKey.Stage00] =
            BuildStage00DepthContentSlots();

        _stageDepthContentSlots[(int)PresentationStageKey.Stage01] =
            BuildStage01DepthContentSlots();

        _stageDepthContentSlots[(int)PresentationStageKey.Stage02] =
            BuildStage02DepthContentSlots();
    }

    private RectTransform[] BuildStage00DepthContentSlots()
    {
        var slots = new RectTransform[PresentationDepthLayerCount];

        slots[(int)PresentationDepthLayerKey.Far] = _stage00DepthFarContent;
        slots[(int)PresentationDepthLayerKey.Back] = _stage00DepthBackContent;
        slots[(int)PresentationDepthLayerKey.Mid] = _stage00DepthMidContent;
        slots[(int)PresentationDepthLayerKey.Front] = _stage00DepthFrontContent;
        slots[(int)PresentationDepthLayerKey.Close] = _stage00DepthCloseContent;

        return slots;
    }

    private RectTransform[] BuildStage01DepthContentSlots()
    {
        var slots = new RectTransform[PresentationDepthLayerCount];

        slots[(int)PresentationDepthLayerKey.Far] = _stage01DepthFarContent;
        slots[(int)PresentationDepthLayerKey.Back] = _stage01DepthBackContent;
        slots[(int)PresentationDepthLayerKey.Mid] = _stage01DepthMidContent;
        slots[(int)PresentationDepthLayerKey.Front] = _stage01DepthFrontContent;
        slots[(int)PresentationDepthLayerKey.Close] = _stage01DepthCloseContent;

        return slots;
    }

    private RectTransform[] BuildStage02DepthContentSlots()
    {
        var slots = new RectTransform[PresentationDepthLayerCount];

        slots[(int)PresentationDepthLayerKey.Far] = _stage02DepthFarContent;
        slots[(int)PresentationDepthLayerKey.Back] = _stage02DepthBackContent;
        slots[(int)PresentationDepthLayerKey.Mid] = _stage02DepthMidContent;
        slots[(int)PresentationDepthLayerKey.Front] = _stage02DepthFrontContent;
        slots[(int)PresentationDepthLayerKey.Close] = _stage02DepthCloseContent;

        return slots;
    }
}