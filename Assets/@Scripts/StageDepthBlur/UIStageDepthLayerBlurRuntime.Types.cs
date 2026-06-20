using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PresentationStageKey
{
    Stage00 = 0,
    Stage01 = 1,
    Stage02 = 2
}

public enum PresentationDepthLayerKey
{
    Far = 0,
    Back = 1,
    Mid = 2,
    Front = 3,
    Close = 4
}

public sealed partial class UIStageDepthLayerBlurRuntime
{
    private static readonly PresentationStageKey[] StageKeys =
    {
        PresentationStageKey.Stage00,
        PresentationStageKey.Stage01,
        PresentationStageKey.Stage02,
    };

    private static readonly PresentationDepthLayerKey[] LayerKeys =
    {
        PresentationDepthLayerKey.Far,
        PresentationDepthLayerKey.Back,
        PresentationDepthLayerKey.Mid,
        PresentationDepthLayerKey.Front,
        PresentationDepthLayerKey.Close,
    };

    private static int StageToIndex(PresentationStageKey stage)
    {
        return stage switch
        {
            PresentationStageKey.Stage00 => 0,
            PresentationStageKey.Stage01 => 1,
            PresentationStageKey.Stage02 => 2,
            _ => 0
        };
    }

    private static string LayerToKey(PresentationDepthLayerKey layer)
    {
        return layer switch
        {
            PresentationDepthLayerKey.Far => "far",
            PresentationDepthLayerKey.Back => "back",
            PresentationDepthLayerKey.Mid => "mid",
            PresentationDepthLayerKey.Front => "front",
            PresentationDepthLayerKey.Close => "close",
            _ => "mid"
        };
    }

    private static string BuildProxyImagePrefix(PresentationStageKey stage, PresentationDepthLayerKey layer)
    {
        return $"Slot{StageToIndex(stage):00}_{LayerToKey(layer)}_";
    }

    private readonly struct LayerKey : IEquatable<LayerKey>
    {
        public readonly PresentationStageKey Stage;
        public readonly PresentationDepthLayerKey Layer;

        public LayerKey(PresentationStageKey stage, PresentationDepthLayerKey layer)
        {
            Stage = stage;
            Layer = layer;
        }

        public bool Equals(LayerKey other) => Stage == other.Stage && Layer == other.Layer;
        public override bool Equals(object obj) => obj is LayerKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Stage * 397) ^ (int)Layer;
            }
        }
    }

    // 한 layer의 지속 bake 상태. alpha/tween은 Command 소유이므로 여기 없음.
    private sealed class LayerState
    {
        public readonly LayerKey Key;

        public PresentationDepthDefocusTarget Target;

        public CharacterRigRegistry CharacterRigs;
        public BackgroundRigRegistry BackgroundRigs;

        public bool IsTracking;

        public float BlurRadius;
        public int Iterations;
        public UIStageBlurDownsample Downsample;
        public float CoveragePaddingPixels;

        public bool OverlayPaddingCaptured;
        public Vector2 BaseOverlayOffsetMin;
        public Vector2 BaseOverlayOffsetMax;
        public Vector2 BaseRawImageOffsetMin;
        public Vector2 BaseRawImageOffsetMax;

        public RenderTexture BakedTexture;

        public LayerState(LayerKey key) => Key = key;
    }

    // layer root 아래 proxy Image를 필요한 만큼 늘려 재사용
    private sealed class ProxyPool
    {
        private readonly PresentationStageKey _stage;
        private readonly PresentationDepthLayerKey _layer;
        private readonly RectTransform _root;
        private readonly UIStageDepthBlurCaptureBuilder _builder;
        private readonly List<Image> _images = new();

        public ProxyPool(
            PresentationStageKey stage,
            PresentationDepthLayerKey layer,
            RectTransform root,
            UIStageDepthBlurCaptureBuilder builder)
        {
            _stage = stage;
            _layer = layer;
            _root = root;
            _builder = builder;

            CollectExistingImages();
        }

        public Image Acquire(int index)
        {
            if (index < 0)
                return null;

            while (_images.Count <= index)
                _images.Add(CreateImage(_images.Count));

            Image image = _images[index];

            if (image == null)
                _images[index] = image = CreateImage(index);

            return image;
        }

        public void DisableAll()
        {
            for (int i = 0; i < _images.Count; i++)
            {
                if (_images[i] != null)
                    _images[i].enabled = false;
            }
        }

        private void CollectExistingImages()
        {
            _images.Clear();

            if (_root == null)
                return;

            Image[] existing = _root.GetComponentsInChildren<Image>(true);

            Array.Sort(existing, (a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < existing.Length; i++)
            {
                Image image = existing[i];

                if (image == null)
                    continue;

                image.raycastTarget = false;
                image.enabled = false;
                _images.Add(image);
            }
        }

        private Image CreateImage(int index)
        {
            string imageName = $"{BuildProxyImagePrefix(_stage, _layer)}{index:00}_Image";

            Image image = _builder.EnsureProxyImage(_root, imageName);

            if (image == null)
                return null;

            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }
    }
}