using System;
using UnityEngine;

public interface IStageDepthContentSlotProvider
{
    RectTransform GetDepthContent(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer);
}

public sealed partial class PresentationUIRoot : IStageDepthContentSlotProvider
{
    public RectTransform GetDepthContent(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer)
    {
        switch (stage)
        {
            case PresentationStageKey.Stage00:
                return GetStage00DepthContent(layer);

            case PresentationStageKey.Stage01:
                return GetStage01DepthContent(layer);

            case PresentationStageKey.Stage02:
                return GetStage02DepthContent(layer);

            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
        }
    }

    private RectTransform GetStage00DepthContent(PresentationDepthLayerKey layer)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                return View.Rect(Refs.Stage00Depth_Far_Content);

            case PresentationDepthLayerKey.Back:
                return View.Rect(Refs.Stage00Depth_Back_Content);

            case PresentationDepthLayerKey.Mid:
                return View.Rect(Refs.Stage00Depth_Mid_Content);

            case PresentationDepthLayerKey.Front:
                return View.Rect(Refs.Stage00Depth_Front_Content);

            case PresentationDepthLayerKey.Close:
                return View.Rect(Refs.Stage00Depth_Close_Content);

            default:
                throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    private RectTransform GetStage01DepthContent(PresentationDepthLayerKey layer)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                return View.Rect(Refs.Stage01Depth_Far_Content);

            case PresentationDepthLayerKey.Back:
                return View.Rect(Refs.Stage01Depth_Back_Content);

            case PresentationDepthLayerKey.Mid:
                return View.Rect(Refs.Stage01Depth_Mid_Content);

            case PresentationDepthLayerKey.Front:
                return View.Rect(Refs.Stage01Depth_Front_Content);

            case PresentationDepthLayerKey.Close:
                return View.Rect(Refs.Stage01Depth_Close_Content);

            default:
                throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    private RectTransform GetStage02DepthContent(PresentationDepthLayerKey layer)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                return View.Rect(Refs.Stage02Depth_Far_Content);

            case PresentationDepthLayerKey.Back:
                return View.Rect(Refs.Stage02Depth_Back_Content);

            case PresentationDepthLayerKey.Mid:
                return View.Rect(Refs.Stage02Depth_Mid_Content);

            case PresentationDepthLayerKey.Front:
                return View.Rect(Refs.Stage02Depth_Front_Content);

            case PresentationDepthLayerKey.Close:
                return View.Rect(Refs.Stage02Depth_Close_Content);

            default:
                throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }
}