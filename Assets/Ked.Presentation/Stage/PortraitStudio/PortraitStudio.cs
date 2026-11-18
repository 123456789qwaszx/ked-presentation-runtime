using System;
using UnityEngine;

/// <summary>
/// 촬영소 — 화면에 보이지 않는 자리에 인물을 세우고, 그 리그에 묶인 카메라가 텍스처에 찍는다.
/// 대사창의 초상 칸이 그 텍스처를 보인다(docs/PLAN_portrait-capture.md).
///
///   PortraitStudio              월드 공간 캔버스. 화면 캔버스(Overlay)는 카메라가 찍지 못해 따로 둔다
///     PortraitStudio_Camera     직교 카메라 → 텍스처. 프레임마다 인물의 캐스팅 축을 따라간다
///     studio_Stage_Root …       무대 리그 하나(키 studio) — 초상 인물은 `portrait`가 여기에 세운다(슬롯 키 portrait)
///
/// 무대 리그로 세우는 까닭: 캐릭터 리그는 무대와 층 위에 선다(CharacterRigRefs.StandOn) — 세우기 · 재기 · 폴드가
/// 그것을 전제한다. 촬영소는 그 전제를 그대로 채우되 화면의 무대 목록(StageRigRegistry.All)에는 들지 않는다.
///
/// 카메라는 배치 축이 아니라 캐스팅 축(CharacterPortrait_VisualOffset)의 포커스 점을 본다 — 인물이 촬영소의 어디에
/// 서 있든 구도가 같고, 그 아래의 연기(표정 · 몸짓 · 반전 · 이모지)는 그대로 찍힌다. 구도는 프리셋(PortraitFramePresetDBSO)이 정한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PortraitStudio : MonoBehaviour
{
    /// <summary>촬영소의 무대 키. 대본의 어휘가 아니다 — 파서가 portrait를 풀 때 쓴다.</summary>
    public const string StageKey = "studio";

    /// <summary>초상 인물의 슬롯 키. `portrait`가 이 키로 세우고, 대본은 표정 · 몸짓을 이 키에 쓴다(`face portrait 2`).</summary>
    public const string SlotKey = "portrait";

    // 화면 밖의 자리. 다른 카메라의 시야(원점 근처)에 들지 않게 멀리 둔다.
    private static readonly Vector3 Origin = new(20000f, 20000f, 0f);

    // 무대 캔버스를 받지 못했을 때의 촬영소 크기. 받았으면 그 캔버스의 크기를 따른다(FitSizeToStage).
    private static readonly Vector2 FallbackSize = new(1920f, 1080f);

    private const float CameraDistance = 100f;

    // 텍스처 한 변의 상한(px) — 화면 배율이 튀어도 터무니없는 텍스처를 만들지 않는다.
    private const int MaxTextureSide = 4096;

    private PresentationStage _stage;
    private CharacterFocusTuningDBSO _focusTuning;
    // 구도의 집 — 프레임마다 다시 읽는다(재생 중에 에셋을 고치면 바로 보인다).
    private PortraitFramePresetDBSO _preset;
    private StageRigRefs _stageRig;
    private Canvas _referenceCanvas;
    private RectTransform _rect;
    private Camera _camera;
    private RenderTexture _texture;

    /// <summary>찍힌 그림. 인물이 서 있는 동안 프레임마다 새로 찍힌다. 해상도가 바뀌어도 같은 객체다.</summary>
    public Texture Texture => _texture;

    /// <summary>
    /// 초상 칸의 자리와 크기(캔버스 px, 대사창 그림의 왼쪽 아래 귀 기준) — 지금의 프리셋 값.
    /// 초상 칸이 프레임마다 읽는다. 텍스처의 px 수는 화면 배율을 따라 바뀌므로 칸의 크기로 쓰지 않는다.
    /// </summary>
    public Rect ReadCellRect() => _preset.Frame.CellRect;

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
        PortraitFramePresetDBSO preset)
    {
        if (preset == null)
            throw new InvalidOperationException("[촬영소] 구도 프리셋(PortraitFramePresetDBSO)이 없다 — 조립이 기본값 에셋이라도 넘겨야 한다.");

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
        rect.sizeDelta = FallbackSize;
        rect.position = Origin;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        PortraitStudio studio = root.AddComponent<PortraitStudio>();
        studio._stage = stage;
        studio._focusTuning = focusTuning;
        studio._preset = preset;
        studio._referenceCanvas = referenceCanvas;
        studio._rect = rect;
        studio.FitSizeToStage();
        studio._texture = CreateTexture(studio.TextureSize(preset.Frame));
        studio._camera = CreateCamera(rect, studio._texture, canvas);
        studio._stageRig = stages.BuildOffscreen(rect, StageKey);

        // 촬영소의 마스크는 끈다 — 무대만 한 사각형이라 켜 두면 화면 위 끝에서 머리가 잘린다(본 무대에서는 그것이 화면 끝이다).
        // 화면 밖 무대라 화면 전체에 걸리는 명령 · 장면 경계가 다시 켜지 않는다.
        studio._stageRig.Mask.SetFullVisible();

        return studio;
    }

    // 촬영소는 화면의 무대와 크기가 같아야 한다. 리그 안의 그림은 무대 크기로 늘어난 rect를 기준으로 자리를 잡는다 —
    // 크기가 다르면 그림이 포커스 점에서 그만큼 어긋나 카메라가 엉뚱한 곳(발)을 찍는다.
    private void FitSizeToStage()
    {
        if (_referenceCanvas == null)
            return;

        Vector2 stageSize = ((RectTransform)_referenceCanvas.transform).rect.size;

        if (stageSize.x <= 0f || stageSize.y <= 0f || _rect.sizeDelta == stageSize)
            return;

        _rect.sizeDelta = stageSize;
    }

    // 텍스처의 px 수 = 범위(캔버스 px) × 칸의 배율 × 화면 배율(캔버스 px → 화면 px) × 해상도 배율.
    // 칸이 화면에 그려지는 px 수에 맞춘다 — 고정해 두면 큰 화면에서 흐려지고 작은 화면에서 낭비다.
    private Vector2Int TextureSize(in PortraitFrame frame)
    {
        float screenScale = _referenceCanvas != null && _referenceCanvas.scaleFactor > 0f
            ? _referenceCanvas.scaleFactor
            : 1f;

        float scale = screenScale * frame.cellScale * frame.textureScale;

        return new Vector2Int(
            Mathf.Clamp(Mathf.CeilToInt(frame.size.x * scale), 1, MaxTextureSide),
            Mathf.Clamp(Mathf.CeilToInt(frame.size.y * scale), 1, MaxTextureSide));
    }

    // 화면 크기나 구도가 바뀌면 텍스처를 다시 잡는다. 객체는 그대로다 — 초상 칸이 든 참조가 끊기지 않는다.
    private void FitTextureToScreen(in PortraitFrame frame)
    {
        Vector2Int size = TextureSize(frame);

        if (_texture.width == size.x && _texture.height == size.y)
            return;

        _texture.Release();
        _texture.width = size.x;
        _texture.height = size.y;
        _texture.Create();
        Clear(_texture);
    }

    private static RenderTexture CreateTexture(Vector2Int size)
    {
        // 깊이 24 = 스텐실 8 — 무대 마스크(UI Mask)가 스텐실을 쓴다.
        RenderTexture texture = new(size.x, size.y, 24, RenderTextureFormat.ARGB32)
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
        PortraitFrame frame = _preset.Frame;

        FitSizeToStage();
        FitTextureToScreen(frame);

        if (!TryFindSubject(out CharacterRigRefs rig))
        {
            if (HasSubject)
                Clear(_texture);

            HasSubject = false;
            _camera.enabled = false;
            return;
        }

        string tuningKey = _stage.castRegistry.TryPeekCharacter(SlotKey, out string characterKey)
            ? characterKey
            : SlotKey;

        CharacterFacing facing = _stage.castRegistry.TryPeekFacing(SlotKey, out CharacterFacing castFacing)
            ? castFacing
            : CharacterFacing.Right;

        // 포커스 점의 식은 한 곳에만 있다(CharacterFocusPointResolver) — 본 무대의 카메라가 보는 점과 같은 점이다.
        CharacterFocusPointResolver.ResolveFocusMeasureInputs(
            rig,
            tuningKey,
            frame.focus,
            frame.centerOffset,
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

        _camera.orthographicSize = frame.size.y * 0.5f * Mathf.Abs(scale.y);
        _camera.aspect = (frame.size.x * Mathf.Abs(scale.x)) / (frame.size.y * Mathf.Abs(scale.y));

        HasSubject = true;
        _camera.enabled = true;
    }

    // 찍는 인물은 정해진 슬롯 하나다 — portrait가 세운 슬롯(SlotKey). "촬영소에 서 있는 아무나"를 찾지 않는다.
    private bool TryFindSubject(out CharacterRigRefs rig)
        => _stage.characterRigs.TryGetRig(SlotKey, out rig)
           && rig.Stage == _stageRig
           && rig.CharacterPortrait_VisualOffset != null;

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
