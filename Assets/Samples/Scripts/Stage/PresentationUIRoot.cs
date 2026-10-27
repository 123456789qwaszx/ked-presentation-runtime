using System;
using UnityEngine;

/// <summary>
/// 연출 무대 — 배경·인물이 서는 세 무대(Stage00–02)와 깊이 층, 무대 위 리그 층, 화면 효과 층.
/// 본체의 무대 계약(IShotResponseStageProvider · IStageDepthContentSlotProvider · IStageMaskProvider)을 구현한다(Providers/).
///
/// 화면(타이틀·대사창·선택지·메뉴)은 갖지 않는다 — 새 예제 UI(Samples/KedUI, UIPresentationFlow 위)가 그 위 캔버스에서 그린다.
/// 칸은 자식 오브젝트의 이름(Refs)으로 찾는다. 같은 이름이 둘이면 앞(깊이 우선)의 것.
/// 클래스·스크립트 GUID는 예전 UI 화면이던 때 그대로다 — 씬의 컴포넌트가 이어진다.
/// </summary>
[DisallowMultipleComponent]
public sealed partial class PresentationUIRoot : MonoBehaviour
{
    #region Refs
    
    public enum Refs
    {
        #region StageViewport Layer
        StageViewportLayer,
        
        StageShot_Root,
        StagePan_Root,
        StageZoom_Root,

        Stage00_Root,
        
        Stage00Mask_Root,
        
        Stage00DepthSystem_Root,
        Stage00Depth_Far_Root,
        Stage00Depth_Far_FramingTransform,
        Stage00Depth_Far_FramingScale,
        Stage00Depth_Far_Content,
        
        Stage00Depth_Back_Root,
        Stage00Depth_Back_FramingTransform,
        Stage00Depth_Back_FramingScale,
        Stage00Depth_Back_Content,
        
        Stage00Depth_Mid_Root,
        Stage00Depth_Mid_FramingTransform,
        Stage00Depth_Mid_FramingScale,
        Stage00Depth_Mid_Content,
        
        Stage00Depth_Front_Root,
        Stage00Depth_Front_FramingTransform,
        Stage00Depth_Front_FramingScale,
        Stage00Depth_Front_Content,
        
        Stage00Depth_Close_Root,
        Stage00Depth_Close_FramingTransform,
        Stage00Depth_Close_FramingScale,
        Stage00Depth_Close_Content,
        
        
        Stage01_Root,
        
        Stage01Mask_Root,

        Stage01DepthSystem_Root,
        Stage01Depth_Far_Root,
        Stage01Depth_Far_FramingTransform,
        Stage01Depth_Far_FramingScale,
        Stage01Depth_Far_Content,

        Stage01Depth_Back_Root,
        Stage01Depth_Back_FramingTransform,
        Stage01Depth_Back_FramingScale,
        Stage01Depth_Back_Content,

        Stage01Depth_Mid_Root,
        Stage01Depth_Mid_FramingTransform,
        Stage01Depth_Mid_FramingScale,
        Stage01Depth_Mid_Content,

        Stage01Depth_Front_Root,
        Stage01Depth_Front_FramingTransform,
        Stage01Depth_Front_FramingScale,
        Stage01Depth_Front_Content,

        Stage01Depth_Close_Root,
        Stage01Depth_Close_FramingTransform,
        Stage01Depth_Close_FramingScale,
        Stage01Depth_Close_Content,



        Stage02_Root,
        
        Stage02Mask_Root,

        Stage02DepthSystem_Root,
        Stage02Depth_Far_Root,
        Stage02Depth_Far_FramingTransform,
        Stage02Depth_Far_FramingScale,
        Stage02Depth_Far_Content,

        Stage02Depth_Back_Root,
        Stage02Depth_Back_FramingTransform,
        Stage02Depth_Back_FramingScale,
        Stage02Depth_Back_Content,

        Stage02Depth_Mid_Root,
        Stage02Depth_Mid_FramingTransform,
        Stage02Depth_Mid_FramingScale,
        Stage02Depth_Mid_Content,

        Stage02Depth_Front_Root,
        Stage02Depth_Front_FramingTransform,
        Stage02Depth_Front_FramingScale,
        Stage02Depth_Front_Content,

        Stage02Depth_Close_Root,
        Stage02Depth_Close_FramingTransform,
        Stage02Depth_Close_FramingScale,
        Stage02Depth_Close_Content,
        
        #endregion
        
        #region StageOverlay Layer
        StageOverlayLayer,
        SpriteRig_Root,
        TextRig_Root,
        
        
        #endregion
        
        #region ScreenEffect Layer
        ScreenEffectLayer,
        // ScreenEffectRig
        
        #endregion
    }

    #endregion

    private GameObject[] _refObjects;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 칸을 찾아 둔다. 조립(PresentationAssembler)이 무대를 쓰기 전에 부른다. 여러 번 불러도 한 번만 한다.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        BindRefObjects();

        CacheRefs();
        ValidateRefs();

        CacheShotResponseStageProviderRefs();
        CacheStageDepthContentSlotProviderRefs();
        CacheStageMaskProviderRefs();
    }

    private void BindRefObjects()
    {
        string[] names = Enum.GetNames(typeof(Refs));
        _refObjects = new GameObject[names.Length];

        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < names.Length; i++)
        {
            foreach (Transform t in all)
            {
                if (t.name != names[i])
                    continue;

                _refObjects[i] = t.gameObject;
                break;
            }
        }
    }

    private RefLookup View => new(this);

    /// <summary>이름 칸에서 컴포넌트를 꺼낸다. 없으면 null.</summary>
    private readonly struct RefLookup
    {
        private readonly PresentationUIRoot _root;

        public RefLookup(PresentationUIRoot root)
        {
            _root = root;
        }

        public RectTransform Rect(Refs key) => Component<RectTransform>(key);

        public T Component<T>(Refs key) where T : Component
        {
            GameObject go = _root._refObjects[(int)key];
            return go != null && go.TryGetComponent(out T component) ? component : null;
        }
    }
}
