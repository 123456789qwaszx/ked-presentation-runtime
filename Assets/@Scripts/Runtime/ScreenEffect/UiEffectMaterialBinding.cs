using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 단일 Image에 바인딩되는 런타임 머터리얼 인스턴스의 소유/수명 관리.
// source(없으면 image.material)를 Instantiate해 image에 할당하고 Dispose에서 파괴한다.
// 화면 이펙트 컨트롤러(Flash/Noise/Vignette)에 중복되던 머터리얼 보일러플레이트를 한 곳으로 모은다.
public sealed class UiEffectMaterialBinding : System.IDisposable
{
    private readonly Image _image;
    private Material _runtimeMaterial;

    public bool IsValid => _runtimeMaterial != null;

    public UiEffectMaterialBinding(Image image, Material sourceMaterial, string ownerName)
    {
        _image = image;

        if (_image == null)
        {
            Debug.LogWarning("[UiEffectMaterialBinding] image is null.");
            return;
        }

        Material baseMaterial = sourceMaterial != null ? sourceMaterial : _image.material;

        if (baseMaterial == null)
        {
            Debug.LogWarning(
                $"[UiEffectMaterialBinding] source material is missing. owner='{ownerName}'.",
                _image);
            return;
        }

        _runtimeMaterial = Object.Instantiate(baseMaterial);
        _runtimeMaterial.name = $"{baseMaterial.name}_Runtime_{ownerName}";
        _image.material = _runtimeMaterial;
    }

    public void SetFloat(int id, float value)
    {
        if (_runtimeMaterial != null)
            _runtimeMaterial.SetFloat(id, value);
    }

    public void SetColor(int id, Color value)
    {
        if (_runtimeMaterial != null)
            _runtimeMaterial.SetColor(id, value);
    }

    public void Dispose()
    {
        if (_runtimeMaterial != null)
        {
            // 에디트 모드에서 Destroy는 동작하지 않고 에러만 남긴다(머티리얼 누수).
            // U12 exporter·EditMode 테스트가 이 경로로 리그를 세운다.
            if (Application.isPlaying)
                Object.Destroy(_runtimeMaterial);
            else
                Object.DestroyImmediate(_runtimeMaterial);

            _runtimeMaterial = null;
        }
    }
}