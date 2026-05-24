using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig : MonoBehaviour
{
    private PresentationIntentState _currentState = PresentationIntentState.Default;

    private readonly List<PresentationResponseBinding> _bindings = new();
    private PresentationCameraRootApplier _cameraRootApplier;

    public PresentationIntentState CurrentState => _currentState;
    
    public void Initialize(PresentationCameraRootApplier cameraRootApplier)
    {
        _cameraRootApplier = cameraRootApplier;
    }

    public void ApplyToAllBindings(in PresentationIntentState state)
    {
        _currentState = state;

        _cameraRootApplier?.Apply(in state);

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null || !binding.IsAlive)
            {
                _bindings.RemoveAt(i);
                continue;
            }

            binding.Apply(in state);
        }
    }

    public bool RegisterCharacterRigBinding(CommandRunScope scope, string targetKey, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        string resolvedSlotKey = ResponseBindingKeys.CharacterRig(scope, targetKey);
        if (!scope.characterRigs.TryGetRig(resolvedSlotKey, out CharacterRigRefs rigRefs))
            return false;

        string bindingKey = ResponseBindingKeys.CharacterRigFromSlotKey(resolvedSlotKey);
        CharacterRigResponseTarget target = new CharacterRigResponseTarget(rigRefs);

        return RegisterRuntimeBinding(bindingKey, target, presetProfile, stageRoot);
    }

    public bool RegisterBackgroundRigBinding(CommandRunScope scope, string bgKey, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        string resolvedBgKey = ResponseBindingKeys.BackgroundRig(scope, bgKey);
        if (!scope.backgroundRigs.TryGetRig(resolvedBgKey, out BackgroundRigRefs rigRefs))
            return false;

        string bindingKey = ResponseBindingKeys.BackgroundRigFromRigKey(resolvedBgKey);
        BackgroundRigResponseTarget target = new BackgroundRigResponseTarget(rigRefs);

        return RegisterRuntimeBinding(bindingKey, target, presetProfile, stageRoot);
    }

    public bool RegisterRuntimeBinding(string bindingKey, IResponseTarget target, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        PresentationResponseProfile runtimeProfile = BakeRuntimeProfile(target, presetProfile, stageRoot);

        PresentationResponseBinding binding = new PresentationResponseBinding(bindingKey, runtimeProfile, target, stageRoot);

        // Immediately sync newly registered runtime targets to the current shot state.
        ReplaceBinding(bindingKey, binding);
        binding.Apply(in _currentState);

        return true;
    }

    public bool RemoveBinding(string bindingKey)
    {
        bool removed = false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.Key, bindingKey, StringComparison.Ordinal))
            {
                _bindings.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    public void Clear()
    {
        _currentState = PresentationIntentState.Default;
        _bindings.Clear();
        
        _cameraRootApplier?.Apply(in _currentState);
    }
    

    private void ReplaceBinding(string key, PresentationResponseBinding newBinding)
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.Key, key, StringComparison.Ordinal))
            {
                _bindings[i] = newBinding;
                return;
            }
        }

        _bindings.Add(newBinding);
    }

    private PresentationResponseProfile BakeRuntimeProfile(IResponseTarget target, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        // See "Response Binding Bake Rules" region below.
        #region Response Binding Bake Rules
        // MeasureRect is the stable logical position source for this response target.
        //
        // IMPORTANT:
        // basePositionInRigSpace must be baked from a transform that is affected by
        // stage placement / blocking commands, but NOT affected by Presentation Response
        // output transforms such as FramingTransform or FramingScale.
        //
        // CharacterRig example:
        // - place / anchor / slot positioning should affect CharSlot_Anchor or another
        //   Slot axis node above CharSlot_Scale.
        // - MeasureRect is usually CharSlot_Scale.
        // - PositionRect is usually CharSlot_FramingTransform.
        // - ScaleRect is usually CharSlot_FramingScale.
        //
        // If localPivot stays (0, 0, 0) even after placing the character left/right,
        // check the rig hierarchy and command target order.
        // The most likely cause is that the anchor/place command is applied BELOW
        // MeasureRect, for example on Character_CastTransform, so MeasureRect cannot
        // see the placement offset.
        //
        // Correct direction:
        //   Stage placement / blocking axis
        //   -> MeasureRect
        //   -> Framing response axis
        //   -> Character casting / visual axis
        #endregion
        Vector3 worldPivot = target.MeasureRect.TransformPoint(Vector3.zero);
        Vector3 localPivot = stageRoot.InverseTransformPoint(worldPivot);
        
        PresentationResponseProfile profile = new PresentationResponseProfile
        {
            maxZoomScaleDelta = presetProfile.maxZoomScaleDelta,
            maxZoomSpreadPixels = presetProfile.maxZoomSpreadPixels,
            panResponse = presetProfile.panResponse
        };

        profile.basePositionInRigSpace = new Vector2(localPivot.x, localPivot.y);
        profile.baseLocalScale = new Vector2(target.ScaleRect.localScale.x, target.ScaleRect.localScale.y);

        // Debug.Log(
        //     $"[PresentationResponseRig] BakeRuntimeProfile " +
        //     $"measure={target.MeasureRect?.name}, " +
        //     $"scale={target.ScaleRect?.name}, " +
        //     $"stage={stageRoot?.name}, " +
        //     $"localPivot={localPivot}, " +
        //     $"basePos={profile.basePositionInRigSpace}, " +
        //     $"baseScale={profile.baseScale}, " +
        //     $"zoomDelta={profile.maxZoomScaleDelta}, " +
        //     $"zoomSpread={profile.maxZoomSpreadPixels}, " +
        //     $"pan={profile.panResponse}",
        //     target.MeasureRect);
        
        return profile;
    }
}