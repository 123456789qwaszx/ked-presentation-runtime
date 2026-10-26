using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ked.Samples.UI.Editor
{
    internal enum KedButtonStyle
    {
        Primary,
        Secondary,
        Danger,
    }

    /// <summary>
    /// 빌더가 만드는 노드 하나: RectTransform과 그 노드의 시안 좌표(1600×900, 왼쪽 위 원점, 아래로 +y).
    /// 자식은 시안의 절대 좌표로 적고, 부모의 시안 좌표를 빼서 Unity 앵커 좌표로 바꾼다 —
    /// 그래서 스펙의 숫자는 Design/layouts.json과 같은 숫자다.
    /// </summary>
    internal sealed class KedNode
    {
        public readonly RectTransform Rect;
        public readonly Rect Design;

        public KedNode(RectTransform rect, Rect design)
        {
            Rect = rect;
            Design = design;
        }

        public GameObject Go => Rect.gameObject;
    }

    internal static class KedAnchor
    {
        public static readonly Vector2 TopLeft = new(0f, 1f);
        public static readonly Vector2 TopCenter = new(0.5f, 1f);
        public static readonly Vector2 TopRight = new(1f, 1f);
        public static readonly Vector2 MiddleLeft = new(0f, 0.5f);
        public static readonly Vector2 Center = new(0.5f, 0.5f);
        public static readonly Vector2 MiddleRight = new(1f, 0.5f);
        public static readonly Vector2 BottomLeft = new(0f, 0f);
        public static readonly Vector2 BottomCenter = new(0.5f, 0f);
        public static readonly Vector2 BottomRight = new(1f, 0f);
    }

    /// <summary>
    /// 화면 빌드 명세(Specs/*)가 쓰는 부품 상자. 시안의 부품 규칙(Docs/IMPLEMENTATION.md §4·§8)을 한곳에 둔다:
    /// Simple/Sliced는 asset-manifest.json대로, 아이콘은 비율 유지, 버튼은 UIPF ButtonAnimation(누름 0.98)과
    /// KedButton(포커스 테두리·막힘 투명도), 글자는 모두 TMP(Noto Sans KR).
    /// </summary>
    internal sealed class KedUIKit
    {
        public const string Root = "Assets/Samples/KedUI";
        public const string ArtDir = Root + "/Art";
        public static readonly Rect Screen = new(0f, 0f, 1600f, 900f);

        [Serializable]
        private sealed class Manifest
        {
            public ManifestEntry[] assets;
        }

        [Serializable]
        private sealed class ManifestEntry
        {
            public string name;
            public int width;
            public int height;
            public string imageType;
            public int[] border;
        }

        private readonly Dictionary<string, Sprite> _sprites = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ManifestEntry> _manifest = new(StringComparer.Ordinal);

        public TMP_FontAsset Font { get; }
        public List<string> Problems { get; } = new();

        public KedUIKit(TMP_FontAsset font)
        {
            Font = font;

            string manifestPath = Root + "/asset-manifest.json";
            if (!File.Exists(manifestPath))
            {
                Problems.Add($"manifest 없음: {manifestPath}");
                return;
            }

            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest?.assets == null)
            {
                Problems.Add("manifest에 assets가 없다");
                return;
            }

            foreach (ManifestEntry entry in manifest.assets)
                _manifest[entry.name] = entry;
        }

        #region 그림

        public Sprite Sprite(string name)
        {
            if (_sprites.TryGetValue(name, out Sprite cached))
                return cached;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");
            if (sprite == null)
                Problems.Add($"스프라이트 없음(임포트 설정 전?): {ArtDir}/{name}.png");

            _sprites[name] = sprite;
            return sprite;
        }

        public bool IsSliced(string name) =>
            _manifest.TryGetValue(name, out ManifestEntry entry) &&
            string.Equals(entry.imageType, "Sliced", StringComparison.OrdinalIgnoreCase);

        public int SourceHeight(string name) =>
            _manifest.TryGetValue(name, out ManifestEntry entry) ? entry.height : 0;

        #endregion

        #region 노드

        /// <summary>화면(View) 프리팹의 뿌리. 부모(층 · PageRoot)를 꽉 채운다. design은 부모가 차지하는 시안 사각형.</summary>
        public KedNode NewView<T>(string name, Rect? design = null) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            StretchToParent(rect);

            go.AddComponent<CanvasGroup>();
            go.AddComponent<T>();

            return new KedNode(rect, design ?? Screen);
        }

        /// <summary>
        /// 화면 크기 1600×900 판. 가운데에 고정한다 — 캔버스 비율 맞춤이 Expand라 어떤 화면비에서도 판 전체가 보인다.
        /// 배경·딤은 판 밖(화면 전체)에, 내용은 판 안에 둔다.
        /// </summary>
        public KedNode Frame(KedNode parent, string name = "Frame")
            => Node(parent, name, Screen, KedAnchor.Center);

        /// <summary>배경 그림: 화면 전체를 비율 유지로 덮는다(넘치는 쪽은 잘린다).</summary>
        public Image Backdrop(KedNode parent, string name, string sprite, bool raycast = true)
        {
            KedNode node = Fill(parent, name);
            Image image = Image(node, sprite, raycast: raycast);

            AspectRatioFitter fitter = node.Go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = image.sprite != null
                ? image.sprite.rect.width / Mathf.Max(1f, image.sprite.rect.height)
                : 16f / 9f;

            return image;
        }

        /// <summary>앵커 한 점에 고정한 노드. anchor는 Unity 규칙(x 오른쪽, y 위쪽 0–1). 시안 위치는 1600×900에서 정확히 같다.</summary>
        public KedNode Node(KedNode parent, string name, Rect design, Vector2 anchor)
        {
            RectTransform rect = NewRect(parent, name);
            Place(rect, design, parent.Design, anchor);
            return new KedNode(rect, design);
        }

        public KedNode Node(KedNode parent, string name, Rect design)
            => Node(parent, name, design, KedAnchor.TopLeft);

        public KedNode Node(KedNode parent, string name, float x, float y, float w, float h)
            => Node(parent, name, new Rect(x, y, w, h), KedAnchor.TopLeft);

        public KedNode Node(KedNode parent, string name, float x, float y, float w, float h, Vector2 anchor)
            => Node(parent, name, new Rect(x, y, w, h), anchor);

        /// <summary>부모를 꽉 채운다. inset은 안쪽 여백(왼·위·오른·아래, 음수면 밖으로).</summary>
        public KedNode Fill(KedNode parent, string name, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            RectTransform rect = NewRect(parent, name);
            StretchToParent(rect);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);

            Rect d = parent.Design;
            return new KedNode(rect, new Rect(d.x + left, d.y + top, d.width - left - right, d.height - top - bottom));
        }

        /// <summary>부모 안의 시안 사각형을 네 변 앵커로 늘인다(부모가 커지면 같이 커진다).</summary>
        public KedNode Stretch(KedNode parent, string name, Rect design)
        {
            RectTransform rect = NewRect(parent, name);
            Rect p = parent.Design;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(design.x - p.x, (p.y + p.height) - (design.y + design.height));
            rect.offsetMax = new Vector2((design.x + design.width) - (p.x + p.width), -(design.y - p.y));

            return new KedNode(rect, design);
        }

        /// <summary>가로로만 늘인다(위·아래·가운데 한 줄에 고정). anchorY는 1=위, 0.5=가운데, 0=아래.</summary>
        public KedNode StretchX(KedNode parent, string name, Rect design, float anchorY)
        {
            RectTransform rect = NewRect(parent, name);
            Rect p = parent.Design;

            rect.anchorMin = new Vector2(0f, anchorY);
            rect.anchorMax = new Vector2(1f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);

            // 가로 여백: 왼쪽 left, 오른쪽 right. 세로는 anchorY 한 줄에 고정.
            float left = design.x - p.x;
            float right = (p.x + p.width) - (design.x + design.width);
            float pivotDown = design.y + design.height * (1f - anchorY);
            float anchorDown = p.y + p.height * (1f - anchorY);

            rect.sizeDelta = new Vector2(-(left + right), design.height);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, -(pivotDown - anchorDown));

            return new KedNode(rect, design);
        }

        public static void Place(RectTransform rect, Rect design, Rect parent, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(design.width, design.height);

            float px = design.x + design.width * anchor.x;
            float pyDown = design.y + design.height * (1f - anchor.y);
            float qx = parent.x + parent.width * anchor.x;
            float qyDown = parent.y + parent.height * (1f - anchor.y);

            rect.anchoredPosition = new Vector2(px - qx, -(pyDown - qyDown));
        }

        private static RectTransform NewRect(KedNode parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent.Rect, false);
            return rect;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion

        #region 그래픽

        /// <summary>제공된 PNG 하나. Image Type은 manifest(Simple/Sliced)대로.</summary>
        public Image Image(KedNode node, string sprite, Color? color = null, bool raycast = false, bool preserveAspect = false, float ppuMultiplier = 1f)
        {
            Image image = node.Go.AddComponent<Image>();
            image.sprite = Sprite(sprite);
            image.type = IsSliced(sprite) ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = ppuMultiplier;
            image.preserveAspect = preserveAspect;
            image.color = color ?? Color.white;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>단색 사각형(딤·구분선) — pixel_white.</summary>
        public Image Solid(KedNode node, Color color, bool raycast = false)
        {
            return Image(node, "pixel_white", color, raycast);
        }

        /// <summary>둥근 면(surface_white, 9분할). radius는 시안 px.</summary>
        public Image Surface(KedNode node, Color color, float radius = 14f, bool raycast = false)
        {
            return Image(node, "surface_white", color, raycast, ppuMultiplier: 14f / Mathf.Max(1f, radius));
        }

        /// <summary>테두리(outline_white, 9분할). 선 굵기는 시안 px.</summary>
        public Image Outline(KedNode node, Color color, float thickness = 3f)
        {
            return Image(node, "outline_white", color, ppuMultiplier: 3f / Mathf.Max(0.5f, thickness));
        }

        /// <summary>가로로 늘이는 레일(slider·toggle 트랙) — 그림 높이를 노드 높이에 맞춰 9분할 경계를 줄인다.</summary>
        public Image Rail(KedNode node, string sprite, bool raycast = false)
        {
            float source = SourceHeight(sprite);
            float multiplier = source > 0f ? source / Mathf.Max(1f, node.Design.height) : 1f;
            return Image(node, sprite, raycast: raycast, ppuMultiplier: multiplier);
        }

        public Image Clear(KedNode node, bool raycast = true)
        {
            Image image = node.Go.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = raycast;
            return image;
        }

        #endregion

        #region 글자

        public TextMeshProUGUI Text(
            KedNode node,
            string text,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool bold = false,
            bool wrap = false,
            TextOverflowModes overflow = TextOverflowModes.Overflow,
            float lineSpacing = 0f,
            float characterSpacing = 0f)
        {
            TextMeshProUGUI tmp = node.Go.AddComponent<TextMeshProUGUI>();

            if (Font != null)
            {
                tmp.font = Font;
                tmp.fontSharedMaterial = Font.material;
            }

            tmp.text = text ?? string.Empty;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = overflow;
            tmp.lineSpacing = lineSpacing;
            tmp.characterSpacing = characterSpacing;
            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.margin = Vector4.zero;

            return tmp;
        }

        public TextMeshProUGUI Label(
            KedNode parent,
            string name,
            string text,
            float x, float y, float w, float h,
            float size,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            bool bold = false,
            bool wrap = false,
            TextOverflowModes overflow = TextOverflowModes.Overflow,
            Vector2? anchor = null)
        {
            KedNode node = Node(parent, name, new Rect(x, y, w, h), anchor ?? KedAnchor.TopLeft);
            return Text(node, text, size, color, alignment, bold, wrap, overflow);
        }

        #endregion

        #region 버튼

        /// <summary>
        /// 글자 버튼(button_primary · button_secondary · button_danger). choice=true면 "선택됨" 그림이 primary다
        /// (설정의 배속·화면 모드처럼 하나를 고르는 버튼).
        /// </summary>
        public KedButton TextButton(
            KedNode parent,
            string name,
            string labelName,
            string text,
            Rect design,
            KedButtonStyle style,
            float fontSize = 23f,
            Vector2? anchor = null,
            bool choice = false)
        {
            KedNode node = Node(parent, name, design, anchor ?? KedAnchor.TopLeft);
            CenterPivot(node.Rect);

            string sprite = style switch
            {
                KedButtonStyle.Primary => "button_primary",
                KedButtonStyle.Danger => "button_danger",
                _ => "button_secondary",
            };

            Image background = Image(node, sprite, raycast: true);
            Color labelColor = style == KedButtonStyle.Secondary ? KedColors.Navy : KedColors.White;

            Button button = AddButton(node, background);
            GameObject ring = FocusRing(node, name + "Focus");

            KedNode labelNode = Fill(node, labelName, 22f, 0f, 22f, 4f);
            TextMeshProUGUI label = Text(labelNode, text, fontSize, labelColor, TextAlignmentOptions.Center, bold: true,
                overflow: TextOverflowModes.Ellipsis);

            return AddLook(node, button, background, ring, label,
                normalSprite: background.sprite,
                hoverSprite: null,
                selectedSprite: choice ? Sprite("button_primary") : null,
                backgroundNormal: Color.white,
                backgroundSelected: Color.white,
                labelNormal: labelColor,
                labelSelected: KedColors.White);
        }

        /// <summary>
        /// 원형 자리의 아이콘 버튼(시안 icon()): 상아색 둥근 바탕 위에 글리프. 누르는 칸은 바탕 전체(52–58px),
        /// 글리프는 안쪽 여백만큼 작다(24–30px). 켜짐(자동·빨리 넘기기)은 바탕이 청록으로.
        /// </summary>
        public KedButton IconButton(
            KedNode parent,
            string name,
            string icon,
            Rect design,
            float padding = 14f,
            Vector2? anchor = null,
            bool withBase = true)
        {
            KedNode node = Node(parent, name, design, anchor ?? KedAnchor.TopLeft);
            CenterPivot(node.Rect);

            KedNode baseNode = Fill(node, name + "Base");
            Image hit = withBase
                ? Surface(baseNode, KedColors.QuickSurface, radius: 16f, raycast: true)
                : Clear(baseNode);

            KedNode iconNode = Fill(node, name + "Icon", padding, padding, padding, padding);
            Image(iconNode, "icon_" + icon, preserveAspect: true);

            Button button = AddButton(node, hit);
            GameObject ring = FocusRing(node, name + "Focus");

            return AddLook(node, button, hit, ring, null,
                normalSprite: null,
                hoverSprite: null,
                selectedSprite: null,
                backgroundNormal: withBase ? KedColors.QuickSurface : new Color(1f, 1f, 1f, 0f),
                backgroundSelected: KedColors.ActiveSurface,
                labelNormal: KedColors.Navy,
                labelSelected: KedColors.Navy);
        }

        /// <summary>설정의 카테고리 탭(tab_normal · tab_hover · tab_selected). 상태가 바뀌어도 사각형은 같다.</summary>
        public KedButton Tab(KedNode parent, string name, string textName, string text, Rect design, Rect textDesign)
        {
            KedNode node = Node(parent, name, design, KedAnchor.TopLeft);
            CenterPivot(node.Rect);
            Image background = Image(node, "tab_normal", raycast: true);

            Button button = AddButton(node, background);
            GameObject ring = FocusRing(node, name + "Focus");

            KedNode labelNode = Node(node, textName, textDesign, KedAnchor.TopLeft);
            TextMeshProUGUI label = Text(labelNode, text, 22f, KedColors.Navy, TextAlignmentOptions.Center, bold: true);

            return AddLook(node, button, background, ring, label,
                normalSprite: Sprite("tab_normal"),
                hoverSprite: Sprite("tab_hover"),
                selectedSprite: Sprite("tab_selected"),
                backgroundNormal: Color.white,
                backgroundSelected: Color.white,
                labelNormal: KedColors.Navy,
                labelSelected: KedColors.White);
        }

        /// <summary>그림 한 장이 곧 버튼(슬롯 카드). 부모를 꽉 채우고, 안의 내용은 이 노드 아래에 둔다(누름 연출이 함께 움직인다).</summary>
        public KedNode ImageButton(KedNode parent, string name, string sprite)
        {
            KedNode node = Fill(parent, name);
            Image background = Image(node, sprite, raycast: true);

            Button button = AddButton(node, background);
            GameObject ring = FocusRing(node, name + "Focus");

            AddLook(node, button, background, ring, null,
                normalSprite: background.sprite,
                hoverSprite: null,
                selectedSprite: null,
                backgroundNormal: Color.white,
                backgroundSelected: Color.white,
                labelNormal: KedColors.Navy,
                labelSelected: KedColors.Navy);

            return node;
        }

        /// <summary>바탕 없는 글자 버튼(타이틀의 "종료"). 누르는 칸은 노드 전체.</summary>
        public KedButton LinkButton(KedNode parent, string name, string labelName, string text, Rect design, float fontSize, Color color)
        {
            KedNode node = Node(parent, name, design, KedAnchor.TopLeft);
            CenterPivot(node.Rect);
            Image hit = Clear(node);

            Button button = AddButton(node, hit);
            GameObject ring = FocusRing(node, name + "Focus");

            KedNode labelNode = Fill(node, labelName, 10f, 0f, 0f, 0f);
            TextMeshProUGUI label = Text(labelNode, text, fontSize, color, TextAlignmentOptions.Left);

            return AddLook(node, button, hit, ring, label,
                normalSprite: null,
                hoverSprite: null,
                selectedSprite: null,
                backgroundNormal: new Color(1f, 1f, 1f, 0f),
                backgroundSelected: new Color(1f, 1f, 1f, 0f),
                labelNormal: color,
                labelSelected: color);
        }

        // 버튼은 가운데를 기준점으로 — 누름 연출(ButtonAnimation, localScale)이 가운데로 줄어들게. 자리는 그대로 둔다.
        private static void CenterPivot(RectTransform rect)
        {
            Vector2 center = new(0.5f, 0.5f);
            Vector2 delta = center - rect.pivot;

            rect.pivot = center;
            rect.anchoredPosition += new Vector2(delta.x * rect.sizeDelta.x, delta.y * rect.sizeDelta.y);
        }

        private Button AddButton(KedNode node, Graphic target)
        {
            Button button = node.Go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = target;

            node.Go.AddComponent<CanvasGroup>();

            // 누름 0.98 · 올림 크기 변화 없음(시안 §8 — 올림은 테두리로 보인다).
            ButtonAnimation animation = node.Go.AddComponent<ButtonAnimation>();
            SetFloat(animation, "_hoverScale", 1f);
            SetFloat(animation, "_pressScale", 0.98f);

            return button;
        }

        private GameObject FocusRing(KedNode node, string name)
        {
            KedNode ring = Fill(node, name, -5f, -5f, -5f, -5f);
            Outline(ring, KedColors.Turquoise, 3f);
            ring.Go.SetActive(false);
            return ring.Go;
        }

        private KedButton AddLook(
            KedNode node,
            Button button,
            Graphic background,
            GameObject ring,
            TMP_Text label,
            Sprite normalSprite,
            Sprite hoverSprite,
            Sprite selectedSprite,
            Color backgroundNormal,
            Color backgroundSelected,
            Color labelNormal,
            Color labelSelected)
        {
            KedButton look = node.Go.AddComponent<KedButton>();

            var so = new SerializedObject(look);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("background").objectReferenceValue = background as Image;
            so.FindProperty("focusRing").objectReferenceValue = ring;
            so.FindProperty("canvasGroup").objectReferenceValue = node.Go.GetComponent<CanvasGroup>();
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("normalSprite").objectReferenceValue = normalSprite;
            so.FindProperty("hoverSprite").objectReferenceValue = hoverSprite;
            so.FindProperty("selectedSprite").objectReferenceValue = selectedSprite;
            so.FindProperty("backgroundNormal").colorValue = backgroundNormal;
            so.FindProperty("backgroundSelected").colorValue = backgroundSelected;
            so.FindProperty("labelNormal").colorValue = labelNormal;
            so.FindProperty("labelSelected").colorValue = labelSelected;
            so.ApplyModifiedPropertiesWithoutUndo();

            return look;
        }

        #endregion

        #region 슬라이더 · 스위치

        /// <summary>
        /// 슬라이더: 트랙(slider_track) · 채움(slider_fill) · 손잡이(slider_thumb) 따로(시안 §8).
        /// Handle Slide Area를 트랙과 같게 두어 손잡이 중심이 레일 양 끝에서 멈춘다.
        /// track은 레일의 시안 사각형(높이 16), 누르는 칸은 손잡이 높이(32)만큼.
        /// </summary>
        public Slider Slider(KedNode parent, string name, string partPrefix, Rect track, float thumbSize = 32f)
        {
            Rect hit = new(track.x, track.y + track.height * 0.5f - thumbSize * 0.5f, track.width, thumbSize);
            KedNode node = Node(parent, name, hit, KedAnchor.TopLeft);
            Clear(node, raycast: true);

            KedNode trackNode = Node(node, partPrefix + "Track", track, KedAnchor.MiddleLeft);
            StretchHorizontally(trackNode.Rect, track.height);
            Rail(trackNode, "slider_track");

            KedNode fillArea = Node(node, partPrefix + "FillArea", track, KedAnchor.MiddleLeft);
            StretchHorizontally(fillArea.Rect, track.height);

            KedNode fill = Fill(fillArea, partPrefix + "Fill");
            Rail(fill, "slider_fill");
            fill.Rect.anchorMax = new Vector2(0.5f, 1f);

            // 손잡이: 슬라이더가 가로 앵커를 값으로 옮긴다. 세로는 손잡이 칸(32)을 꽉 채운다 — Unity 기본 슬라이더와 같은 꼴.
            KedNode handleArea = Fill(node, partPrefix + "HandleArea");
            KedNode thumb = Node(handleArea, partPrefix + "Thumb",
                new Rect(track.x - thumbSize * 0.5f, hit.y, thumbSize, thumbSize), KedAnchor.MiddleLeft);
            thumb.Rect.anchorMin = new Vector2(0f, 0f);
            thumb.Rect.anchorMax = new Vector2(0f, 1f);
            thumb.Rect.pivot = new Vector2(0.5f, 0.5f);
            thumb.Rect.sizeDelta = new Vector2(thumbSize, 0f);
            thumb.Rect.anchoredPosition = Vector2.zero;
            Image thumbImage = Image(thumb, "slider_thumb", raycast: true, preserveAspect: true);

            Slider slider = node.Go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = thumbImage;
            slider.fillRect = fill.Rect;
            slider.handleRect = thumb.Rect;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(50f);

            return slider;
        }

        private static void StretchHorizontally(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>켜고 끄는 스위치(toggle_track_off/on + toggle_thumb). 손잡이 중심은 트랙 왼쪽 끝에서 20 / 70.</summary>
        public Toggle Switch(KedNode parent, string name, string partPrefix, Rect track, float thumbSize = 34f)
        {
            KedNode node = Node(parent, name, track, KedAnchor.TopLeft);

            KedNode trackNode = Fill(node, partPrefix + "Track");
            Image trackImage = Rail(trackNode, "toggle_track_off", raycast: true);

            float inset = (track.height - thumbSize) * 0.5f;
            float offX = inset + thumbSize * 0.5f;
            float onX = track.width - offX;

            KedNode thumb = Node(node, partPrefix + "Thumb",
                new Rect(track.x + inset, track.y + inset, thumbSize, thumbSize), KedAnchor.MiddleLeft);
            thumb.Rect.pivot = new Vector2(0.5f, 0.5f);
            thumb.Rect.anchoredPosition = new Vector2(offX, 0f);
            Image(thumb, "toggle_thumb", preserveAspect: true);

            Toggle toggle = node.Go.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = trackImage;
            toggle.graphic = null;
            toggle.isOn = false;

            KedToggleSwitch look = node.Go.AddComponent<KedToggleSwitch>();
            var so = new SerializedObject(look);
            so.FindProperty("toggle").objectReferenceValue = toggle;
            so.FindProperty("track").objectReferenceValue = trackImage;
            so.FindProperty("thumb").objectReferenceValue = thumb.Rect;
            so.FindProperty("offSprite").objectReferenceValue = Sprite("toggle_track_off");
            so.FindProperty("onSprite").objectReferenceValue = Sprite("toggle_track_on");
            so.FindProperty("thumbOffX").floatValue = offX;
            so.FindProperty("thumbOnX").floatValue = onX;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        #endregion

        #region 직렬화 도우미

        public static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"[Ked UI 빌더] {target.GetType().Name}.{property} 직렬화 칸이 없다.");
                return;
            }

            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObject(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"[Ked UI 빌더] {target.GetType().Name}.{property} 직렬화 칸이 없다.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
