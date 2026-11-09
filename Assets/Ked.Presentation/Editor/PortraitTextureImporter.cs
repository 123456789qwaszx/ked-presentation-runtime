using UnityEditor;

/// <summary>
/// 초상 텍스처의 임포트 상한을 올린다 — 경로에 `/Portraits/`가 든 것만.
///
/// 초상은 원본 픽셀 크기 그대로 그린다(CharRigImageSizingMode.SourcePixels, 2026-11-09).
/// 기본 상한 2048에 걸리면 1745×2170 같은 원본이 줄어든 채 들어와 화면에 작게 나온다.
/// 초상 뿌리는 PortraitBuildSettings.scanFolders가 정하지만(샘플 `Samples/Content/Portraits`),
/// 임포트 도중에 설정 에셋을 읽지 않으려고 BackgroundTextureImporter처럼 경로 표식 하나로 건다.
/// 상한을 올리기만 하고 내리지 않는다 — 손으로 더 높게 둔 값은 그대로 둔다.
/// 이미 임포트된 그림은 다시 임포트해야 반영된다(폴더 우클릭 › Reimport).
/// 그래도 걸린 그림은 PresentationTuningExporter가 경고한다.
/// </summary>
public sealed class PortraitTextureImporter : AssetPostprocessor
{
    public const int MaxTextureSize = 4096;

    private const string Marker = "/Portraits/";

    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains(Marker))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        if (importer.maxTextureSize < MaxTextureSize)
            importer.maxTextureSize = MaxTextureSize;
    }
}
