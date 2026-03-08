#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PortraitIdentity))]
public sealed class PortraitIdentityDrawer : PropertyDrawer
{
    private const float LineSpacing = 2f;
    private const float PreviewSize = 240f;
    
    // 미리보기 설정값들 (코드에서만 조절)
    private static float s_CutBottomRatio = 0.45f;
    private static float s_FocusTopRatio = 1f;
    private static float s_ZoomLevel = 1.85f;
    
    // Settings 표시 여부 (false로 설정하면 숨김)
    private static readonly bool ShowSettings = false;
    
    // 토글 상태 (PropertyDrawer는 인스턴스가 여러개라 static으로)
    private static Dictionary<string, bool> s_ZoomStates = new ();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        
        if (ShowSettings)
        {
            return line * 6  // 3개 필드 + 3개 설정 필드
                 + LineSpacing * 5
                 + 6f
                 + PreviewSize;
        }
        else
        {
            return line * 3  // 3개 필드만
                 + LineSpacing * 2
                 + 6f
                 + PreviewSize;
        }
    }

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        EditorGUI.BeginProperty(pos, label, prop);

        var characterProp = prop.FindPropertyRelative("character");
        var variantProp   = prop.FindPropertyRelative("variant");
        var emotionProp   = prop.FindPropertyRelative("emotion");

        Rect row = pos;
        row.height = EditorGUIUtility.singleLineHeight;

        // === 미리보기 설정 (ShowSettings가 true일 때만 표시) ===
        if (ShowSettings)
        {
            EditorGUI.LabelField(row, "Preview Settings", EditorStyles.boldLabel);
            row.y += row.height + LineSpacing;
            
            EditorGUI.indentLevel++;
            
            s_CutBottomRatio = EditorGUI.Slider(row, "Cut Bottom", s_CutBottomRatio, 0f, 0.5f);
            row.y += row.height + LineSpacing;
            
            s_FocusTopRatio = EditorGUI.Slider(row, "Focus Point", s_FocusTopRatio, 0f, 1f);
            row.y += row.height + LineSpacing;
            
            s_ZoomLevel = EditorGUI.Slider(row, "Zoom Level", s_ZoomLevel, 1f, 3f);
            row.y += row.height + LineSpacing;
            
            EditorGUI.indentLevel--;
        }

        // Character
        DrawTextWithDropdown(
            row,
            "Character",
            characterProp,
            PortraitEditorCache.GetCharacters()
        );
        row.y += row.height + LineSpacing;

        // Variant
        DrawTextWithDropdown(
            row,
            "Variant",
            variantProp,
            PortraitEditorCache.GetVariants(characterProp.stringValue),
            allowEmptyAsDefault: true
        );
        row.y += row.height + LineSpacing;

        // Emotion
        DrawTextWithDropdown(
            row,
            "Emotion",
            emotionProp,
            PortraitEditorCache.GetEmotions(
                characterProp.stringValue,
                variantProp.stringValue
            )
        );
        row.y += row.height + 6f;

        // === Sprite Preview (중앙 정렬) ===
        Rect previewContainer = row;
        previewContainer.height = PreviewSize;
        
        // 중앙 정렬을 위한 계산
        float containerWidth = pos.width;
        float previewX = pos.x + (containerWidth - PreviewSize) * 0.5f;
        Rect centeredPreviewRect = new Rect(previewX, previewContainer.y, PreviewSize, PreviewSize);
        
        DrawPreview(centeredPreviewRect, prop, characterProp, variantProp, emotionProp);

        EditorGUI.EndProperty();
    }

    private void DrawPreview(
        Rect previewRect,
        SerializedProperty rootProp,
        SerializedProperty characterProp,
        SerializedProperty variantProp,
        SerializedProperty emotionProp)
    {
        Sprite sprite = PortraitEditorCache.GetSprite(
            characterProp.stringValue,
            variantProp.stringValue,
            emotionProp.stringValue
        );

        GUI.Box(previewRect, GUIContent.none);

        if (sprite != null)
        {
            var inner = new Rect(
                previewRect.x + 4,
                previewRect.y + 4,
                PreviewSize - 8,
                PreviewSize - 8
            );

            // 토글 상태 가져오기
            string key = rootProp.propertyPath;
            if (!s_ZoomStates.ContainsKey(key))
                s_ZoomStates[key] = true; // 기본값: 줌 활성화

            bool isZoomed = s_ZoomStates[key];

            // 클릭 감지
            Event e = Event.current;
            if (e.type == EventType.MouseDown && previewRect.Contains(e.mousePosition))
            {
                s_ZoomStates[key] = !isZoomed;
                e.Use();
            }

            // 그리기
            if (isZoomed)
            {
                DrawPortraitFocusedPreview(inner, sprite);
            }
            else
            {
                DrawPortraitOriginal(inner, sprite);
            }

            // 상태 표시
            var statusRect = new Rect(previewRect.x + 8, previewRect.y + 8, 100, 20);
            GUI.Label(statusRect, isZoomed ? "🔍 Zoomed" : "📷 Original", EditorStyles.whiteLabel);
        }
        else
        {
            EditorGUI.LabelField(previewRect, "No Sprite", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawPortraitOriginal(Rect rect, Sprite sprite)
    {
        if (sprite == null)
            return;

        Rect tr = sprite.textureRect;
        
        // 원본 비율 유지하며 rect에 맞추기 (중앙 정렬)
        float spriteAspect = tr.width / tr.height;
        float rectAspect = rect.width / rect.height;
        
        Rect drawRect = rect;
        if (spriteAspect > rectAspect)
        {
            // 가로가 긴 경우 - 세로 중앙 정렬
            float height = rect.width / spriteAspect;
            drawRect.y += (rect.height - height) * 0.5f;
            drawRect.height = height;
        }
        else
        {
            // 세로가 긴 경우 - 가로 중앙 정렬
            float width = rect.height * spriteAspect;
            drawRect.x += (rect.width - width) * 0.5f;
            drawRect.width = width;
        }

        Rect uv = new Rect(
            tr.x / sprite.texture.width,
            tr.y / sprite.texture.height,
            tr.width / sprite.texture.width,
            tr.height / sprite.texture.height
        );

        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
    }

    private void DrawPortraitFocusedPreview(Rect rect, Sprite sprite)
    {
        if (sprite == null)
            return;

        Rect tr = sprite.textureRect;

        // 1아래쪽 컷
        float cutHeight = tr.height * (1f - s_CutBottomRatio);
        float croppedY = tr.y + tr.height * s_CutBottomRatio;
        Rect cropped = new Rect(tr.x, croppedY, tr.width, cutHeight);

        // 2얼굴 쪽 포커스
        float focusCenterY = cropped.y + cropped.height * s_FocusTopRatio;

        // 3줌 (세로만 줌, 가로는 전체 유지)
        float viewH = cropped.height / s_ZoomLevel;
        float viewW = cropped.width / 1.4f;

        Rect view = new Rect(
            cropped.center.x - viewW * 0.5f,
            focusCenterY - viewH * 0.5f,
            viewW,
            viewH
        );

        view.x = Mathf.Clamp(view.x, cropped.x, cropped.xMax - view.width);
        view.y = Mathf.Clamp(view.y, cropped.y, cropped.yMax - view.height);

        float viewAspect = view.width / view.height;
        float rectAspect = rect.width / rect.height;
    
        Rect drawRect = rect;
        if (viewAspect > rectAspect)
        {
            // 가로가 긴 경우 - 세로 중앙 정렬
            float height = rect.width / viewAspect;
            drawRect.y += (rect.height - height) * 0.5f;
            drawRect.height = height;
        }
        else
        {
            // 세로가 긴 경우 - 가로 중앙 정렬
            float width = rect.height * viewAspect;
            drawRect.x += (rect.width - width) * 0.5f;
            drawRect.width = width;
        }

        Rect uv = new Rect(
            view.x / sprite.texture.width,
            view.y / sprite.texture.height,
            view.width / sprite.texture.width,
            view.height / sprite.texture.height
        );

        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
    }

    private void DrawTextWithDropdown(
        Rect rect,
        string label,
        SerializedProperty prop,
        System.Collections.Generic.List<string> options,
        bool allowEmptyAsDefault = false)
    {
        const float popupWidth = 22f;

        Rect labelRect = rect;
        labelRect.width = EditorGUIUtility.labelWidth;

        Rect fieldRect = rect;
        fieldRect.xMin += EditorGUIUtility.labelWidth;
        fieldRect.xMax -= popupWidth + 2;

        Rect popupRect = rect;
        popupRect.xMin = popupRect.xMax - popupWidth;

        EditorGUI.LabelField(labelRect, label);

        EditorGUI.BeginChangeCheck();
        string next = EditorGUI.TextField(fieldRect, prop.stringValue);
        if (EditorGUI.EndChangeCheck())
        {
            prop.stringValue = next;
        }

        using (new EditorGUI.DisabledScope(options == null || options.Count == 0))
        {
            if (EditorGUI.DropdownButton(popupRect, GUIContent.none, FocusType.Passive))
            {
                var menu = new GenericMenu();

                if (allowEmptyAsDefault)
                {
                    menu.AddItem(
                        new GUIContent("(Default)"),
                        string.IsNullOrEmpty(prop.stringValue),
                        () => Set(prop, "")
                    );
                }

                foreach (var opt in options)
                {
                    string captured = opt;
                    menu.AddItem(
                        new GUIContent(captured),
                        captured == prop.stringValue,
                        () => Set(prop, captured)
                    );
                }

                menu.ShowAsContext();
            }
        }
    }

    private static void Set(SerializedProperty prop, string value)
    {
        prop.stringValue = value;
        prop.serializedObject.ApplyModifiedProperties();
    }
}
#endif