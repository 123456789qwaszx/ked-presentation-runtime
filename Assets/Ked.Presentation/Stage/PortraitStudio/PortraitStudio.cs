using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 촬영소 — 화면에 보이지 않는 자리에 인물을 세우고, 그 리그에 묶인 카메라가 텍스처에 찍는다.
/// 대사창의 초상 칸이 그 텍스처를 보인다(docs/PLAN_portrait-capture.md).
///
///   PortraitStudio              월드 공간 캔버스. 화면 캔버스(Overlay)는 카메라가 찍지 못해 따로 둔다
///     PortraitStudio_Camera     직교 카메라 → 텍스처. 프레임마다 인물의 캐스팅 축을 따라간다
///     studio_Stage_Root …       무대 리그 하나(키 studio) — 인물은 `slot 키 studio`로 여기에 선다
///
/// 무대 리그로 세우는 까닭: 캐릭터 리그는 무대와 층 위에 선다(CharacterRigRefs.StandOn) — 세우기 · 재기 · 폴드가
/// 그것을 전제한다. 촬영소는 그 전제를 그대로 채우되 화면의 무대 목록(StageRigRegistry.All)에는 들지 않는다.
///
/// 카메라는 배치 축이 아니라 캐스팅 축(CharacterPortrait_VisualOffset)의 포커스 점을 본다 — 인물이 촬영소의 어디에
/// 서 있든 구도가 같고, 그 아래의 연기(표정 · 몸짓 · 반전 · 이모지)는 그대로 찍힌다. 구도는 PortraitFrame이 정한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PortraitStudio : MonoBehaviour
{
    /// <summary>대본이 부르는 무대 키 — `slot pov studio`.</summary>
    public const string StageKey = "studio";

    // 화면 밖의 자리. 다른 카메라의 시야(원점 근처)에 들지 않게 멀리 둔다.
    private static readonly Vector3 Origin = new(20000f, 20000f, 0f);

    // 촬영소 캔버스의 크기. 카메라가 리그를 따라가므로 화면 크기와 맞출 까닭이 없다 —
    // 무대 마스크(캔버스만 한 사각형)가 머리를 자르지 않게 넉넉히 둔다.
    private static readonly Vector2 Size = new(4096f, 4096f);

    private const float CameraDistance = 100f;

    private readonly List<KeyValuePair<string, CharacterRigRefs>> _rigScratch = new();

    private PresentationStage _stage;
    private CharacterFocusTuningDBSO _focusTuning;
    private PortraitFrame _frame;
    private StageRigRefs _stageRig;
    private Camera _camera;
    private RenderTexture _texture;

    /// <summary>찍힌 그림. 인물이 서 있는 동안 프레임마다 새로 찍힌다.</summary>
    public Texture Texture => _texture;

    /// <summary>촬영소에 선 인물이 있는가 — 없으면 카메라는 쉬고 텍스처는 투명이다.</summary>
    public bool HasSubject { get; private set; }

    /// <summary>
    /// 촬영소를 세우고 무대 리그(키 studio)를 화면 밖 무대로 등록한다 — 화면 전체에 걸리는 명령과 무대 틀은 촬영소를 모른다.
    /// referenceCanvas는 화면의 무대 캔버스다 — 그림의 배율(referencePixelsPerUnit)과 셰이더 채널을 같게 맞춘다.
    /// </summary>
    public static PortraitStudio Build(
        StageRigRegistry stages,
        PresentationStage stage,
        CharacterFocusTuningDBSO focusTuning,
        Canvas referenceCanvas,
        PortraitFrame frame)
    {
        if (focusTuning == null)
            throw new InvalidOperationException("[촬영소] 포커스 튜닝(CharacterFocusTuningDBSO)이 없다 — 카메라가 볼 점을 정할 수 없다.");

        GameObject root = new("PortraitStudio", typeof(RectTransform), typeof(Canvas));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        if (referenceCanvas != null)
        {
            canvas.referencePixelsPerUnit = referenceCanvas.referencePixelsPerUnit;
            canvas.additionalShaderChannels = referenceCanvas.additionalShaderChannels;
        }

        RectTransform rect = (RectTransform)root.transform;
        rect.sizeDelta = Size;
        rect.position = Origin;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        PortraitStudio studio = root.AddComponent<PortraitStudio>();
        studio._stage = stage;
        studio._focusTuning = focusTuning;
        studio._frame = frame.Sanitized();
        studio._texture = CreateTexture(studio._frame);
        studio._camera = CreateCamera(rect, studio._texture, canvas);
        studio._stageRig = stages.BuildOffscreen(rect, StageKey);

        return studio;
    }

    private static RenderTexture CreateTexture(PortraitFrame frame)
    {
        int width = Mathf.Max(1, Mathf.RoundToInt(frame.size.x * frame.textureScale));
        int height = Mathf.Max(1, Mathf.RoundToInt(frame.size.y * frame.textureScale));

        // 깊이 24 = 스텐실 8 — 무대 마스크(UI Mask)가 스텐실을 쓴다.
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "PortraitStudio_Texture",
            useMipMap = false,
            antiAliasing = 1,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        texture.Create();
        Clear(texture);

        return texture;
    }

    // 새 텍스처의 내용은 정해져 있지 않다. 인물이 떠난 뒤의 마지막 그림도 남기지 않는다 — 투명으로 지운다.
    private static void Clear(RenderTexture texture)
    {
        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = texture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
    }

    private static Camera CreateCamera(RectTransform studioRoot, RenderTexture texture, Canvas canvas)
    {
        GameObject go = new("PortraitStudio_Camera", typeof(Camera));
        go.transform.SetParent(studioRoot, false);

        Camera camera = go.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = CameraDistance * 2f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.useOcclusionCulling = false;
        camera.targetTexture = texture;

        // 인물이 설 때까지 찍지 않는다.
        camera.enabled = false;

        canvas.worldCamera = camera;

        return camera;
    }

    // 트윈(Update) · 커맨드 코루틴이 지난 뒤, 캔버스가 그려지기 전에 카메라를 맞춘다 — 같은 프레임의 연기가 찍힌다.
    private void LateUpdate()
    {
        if (!TryFindSubject(out string slotKey, out CharacterRigRefs rig))
        {
            if (HasSubject)
                Clear(_texture);

            HasSubject = false;
            _camera.enabled = false;
            return;
        }

        string tuningKey = _stage.castRegistry.TryPeekCharacter(slotKey, out string characterKey)
            ? characterKey
            : slotKey;

        CharacterFacing facing = _stage.castRegistry.TryPeekFacing(slotKey, out CharacterFacing castFacing)
            ? castFacing
            : CharacterFacing.Right;

        // 포커스 점의 식은 한 곳에만 있다(CharacterFocusPointResolver) — 본 무대의 카메라가 보는 점과 같은 점이다.
        CharacterFocusPointResolver.ResolveFocusMeasureInputs(
            rig,
            tuningKey,
            _frame.focus,
            _frame.centerOffset,
            _focusTuning,
            facing,
            mirrorCommandOffset: true,
            out RectTransform measureRect,
            out Vector3 focusLocalOffset);

        Vector3 center = measureRect.TransformPoint(focusLocalOffset);
        Vector3 scale = measureRect.lossyScale;
        Quaternion rotation = measureRect.rotation;

        Transform cameraTransform = _camera.transform;
        cameraTransform.SetPositionAndRotation(
            center - rotation * Vector3.forward * CameraDistance,
            rotation);

        _camera.orthographicSize = _frame.size.y * 0.5f * Mathf.Abs(scale.y);
        _camera.aspect = (_frame.size.x * Mathf.Abs(scale.x)) / (_frame.size.y * Mathf.Abs(scale.y));

        HasSubject = true;
        _camera.enabled = true;
    }

    // 초상 칸의 인물은 하나다 — 촬영소에 선 첫 리그를 찍는다(docs/PLAN_portrait-capture.md §3 규칙 7).
    private bool TryFindSubject(out string slotKey, out CharacterRigRefs rig)
    {
        _rigScratch.Clear();
        _stage.characterRigs.CollectAliveRigEntries(_rigScratch);

        for (int i = 0; i < _rigScratch.Count; i++)
        {
            CharacterRigRefs candidate = _rigScratch[i].Value;

            if (candidate.Stage != _stageRig || candidate.CharacterPortrait_VisualOffset == null)
                continue;

            slotKey = _rigScratch[i].Key;
            rig = candidate;
            return true;
        }

        slotKey = null;
        rig = null;
        return false;
    }

    private void OnDestroy()
    {
        if (_camera != null)
            _camera.targetTexture = null;

        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }
    }
}
