using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CharacterEmojiLibrarySO))]
public sealed class CharacterEmojiLibrarySOEditor : Editor
{
    private SerializedProperty _entries;
    private SerializedProperty _savedLayouts;

    private Vector2 _scroll;
    private bool _showSavedLayouts = true;
    private bool _showEntryList;

    private ReorderableList _entryQuickList;

    private readonly Dictionary<string, bool> _entrySavedLayoutFoldouts = new();

    private const float PreviewSize = 64f;

    private void OnEnable()
    {
        _entries = serializedObject.FindProperty("entries");
        _savedLayouts = serializedObject.FindProperty("savedLayouts");

        BuildEntryQuickList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeaderTools();

        EditorGUILayout.Space(8);

        DrawSavedLayouts();

        EditorGUILayout.Space(8);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawEntries();
        EditorGUILayout.EndScrollView();

        serializedObject.ApplyModifiedProperties();
    }

    private void BuildEntryQuickList()
    {
        if (_entries == null)
            return;

        _entryQuickList = new ReorderableList(
            serializedObject,
            _entries,
            true,
            false,
            false,
            false);

        _entryQuickList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
        _entryQuickList.drawElementCallback = DrawEntryQuickListElement;

        _entryQuickList.onReorderCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Repaint();
        };

        _entryQuickList.onSelectCallback = list =>
        {
            if (list.index < 0 || list.index >= _entries.arraySize)
                return;

            SerializedProperty entry = _entries.GetArrayElementAtIndex(list.index);
            entry.isExpanded = true;

            GUI.FocusControl(null);
            Repaint();
        };
    }

    private void DrawHeaderTools()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Character Emoji Library ({_entries.arraySize})",
                    EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                string listLabel = _showEntryList ? "Entry List ▼" : "Entry List ▶";

                if (GUILayout.Button(listLabel, GUILayout.Width(110f)))
                {
                    _showEntryList = !_showEntryList;
                }
            }

            if (_showEntryList)
            {
                DrawEntryQuickList();
                EditorGUILayout.Space(4);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Entry"))
                {
                    AddEntry();
                }

                if (GUILayout.Button("Add Layout Slot"))
                {
                    AddLayoutSlot();
                }

                if (GUILayout.Button("Validate"))
                {
                    ValidateEntries();
                }

                if (GUILayout.Button("Sort By Key"))
                {
                    SortByKey();
                }
            }
        }
    }

    private void DrawEntryQuickList()
    {
        if (_entries == null || _entries.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No emoji entries registered.", MessageType.Info);
            return;
        }

        if (_entryQuickList == null)
            BuildEntryQuickList();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Registered Emoji Keys", EditorStyles.miniBoldLabel);

            EditorGUILayout.HelpBox(
                "Drag items here to reorder the actual emoji entries.",
                MessageType.None);

            _entryQuickList.DoLayoutList();
        }
    }

    private void DrawEntryQuickListElement(
        Rect rect,
        int index,
        bool isActive,
        bool isFocused)
    {
        if (_entries == null)
            return;

        if (index < 0 || index >= _entries.arraySize)
            return;

        SerializedProperty entry = _entries.GetArrayElementAtIndex(index);
        SerializedProperty emojiKey = entry.FindPropertyRelative("emojiKey");
        SerializedProperty sprite = entry.FindPropertyRelative("sprite");

        string key = string.IsNullOrEmpty(emojiKey.stringValue)
            ? "(empty key)"
            : emojiKey.stringValue;

        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        Rect indexRect = rect;
        indexRect.width = 34f;

        Rect keyRect = rect;
        keyRect.xMin = indexRect.xMax + 4f;
        keyRect.xMax -= 74f;

        Rect openRect = rect;
        openRect.xMin = rect.xMax - 64f;

        EditorGUI.LabelField(indexRect, $"{index + 1:00}.", EditorStyles.miniLabel);
        EditorGUI.LabelField(keyRect, key, EditorStyles.label);

        if (sprite.objectReferenceValue == null)
        {
            Rect warningRect = keyRect;
            warningRect.xMin = Mathf.Max(keyRect.xMin, keyRect.xMax - 72f);

            EditorGUI.LabelField(
                warningRect,
                "No Sprite",
                EditorStyles.miniLabel);
        }

        string buttonLabel = entry.isExpanded ? "Close" : "Open";

        if (GUI.Button(openRect, buttonLabel, EditorStyles.miniButton))
        {
            entry.isExpanded = !entry.isExpanded;

            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void DrawSavedLayouts()
    {
        if (_savedLayouts == null)
        {
            EditorGUILayout.HelpBox("Failed to find 'savedLayouts' property.", MessageType.Error);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Rect headerRect = EditorGUILayout.GetControlRect(
                false,
                EditorGUIUtility.singleLineHeight + 2f);

            Rect buttonRect = headerRect;
            buttonRect.xMin = buttonRect.xMax - 28f;

            Rect foldoutRect = headerRect;
            foldoutRect.xMax = buttonRect.xMin - 4f;

            _showSavedLayouts = DrawWideFoldoutInRect(
                foldoutRect,
                _showSavedLayouts,
                $"Saved Layout Slots ({_savedLayouts.arraySize})",
                EditorStyles.boldLabel);

            if (GUI.Button(buttonRect, "+"))
            {
                AddLayoutSlot();
            }

            if (!_showSavedLayouts)
                return;

            if (_savedLayouts.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No saved layout slots yet. Add a slot, then save an entry layout into it.",
                    MessageType.Info);
                return;
            }

            int removeIndex = -1;

            for (int i = 0; i < _savedLayouts.arraySize; i++)
            {
                SerializedProperty slot = _savedLayouts.GetArrayElementAtIndex(i);

                SerializedProperty label = slot.FindPropertyRelative("label");
                SerializedProperty layout = slot.FindPropertyRelative("layout");

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Slot {i + 1}", GUILayout.Width(56f));
                        EditorGUILayout.PropertyField(label, GUIContent.none);

                        if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                        {
                            removeIndex = i;
                        }
                    }

                    EditorGUILayout.PropertyField(layout, true);
                }
            }

            if (removeIndex >= 0)
            {
                RemoveLayoutSlot(removeIndex);
            }
        }
    }

    private void DrawEntries()
    {
        if (_entries == null)
        {
            EditorGUILayout.HelpBox("Failed to find 'entries' property.", MessageType.Error);
            return;
        }

        int removeIndex = -1;

        for (int i = 0; i < _entries.arraySize; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);

            if (entry == null)
                continue;

            SerializedProperty emojiKey = entry.FindPropertyRelative("emojiKey");
            SerializedProperty sprite = entry.FindPropertyRelative("sprite");
            SerializedProperty layout = entry.FindPropertyRelative("layout");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawEntryHeader(i, entry, emojiKey, ref removeIndex);

                if (entry.isExpanded)
                {
                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawSpritePreview(sprite);
                        DrawEntryFields(emojiKey, sprite);
                    }

                    EditorGUILayout.Space(4);

                    DrawLayoutSeedButtons(entry);

                    EditorGUILayout.Space(4);

                    DrawSavedLayoutActions(entry);

                    EditorGUILayout.Space(4);

                    DrawLayoutProperty(layout);
                }
            }

            EditorGUILayout.Space(4);
        }

        if (removeIndex >= 0)
            RemoveEntry(removeIndex);
    }
    
    private void DrawLayoutProperty(SerializedProperty layout)
    {
        if (layout == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUIStyle layoutTitleStyle = new(EditorStyles.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
            };

            Rect headerRect = EditorGUILayout.GetControlRect(
                false,
                EditorGUIUtility.singleLineHeight + 4f);

            Rect foldoutRect = headerRect;
            foldoutRect.xMin += 2f;
            foldoutRect.xMax -= 2f;

            layout.isExpanded = DrawWideFoldoutInRect(
                foldoutRect,
                layout.isExpanded,
                "Layout",
                layoutTitleStyle);

            if (!layout.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    layout.FindPropertyRelative("anchoredPosition"),
                    new GUIContent("Anchored Position"));

                EditorGUILayout.PropertyField(
                    layout.FindPropertyRelative("localScale"),
                    new GUIContent("Local Scale"));

                EditorGUILayout.PropertyField(
                    layout.FindPropertyRelative("rotationZ"),
                    new GUIContent("Rotation Z"));

                EditorGUILayout.PropertyField(
                    layout.FindPropertyRelative("preserveAspect"),
                    new GUIContent("Preserve Aspect"));

                EditorGUILayout.PropertyField(
                    layout.FindPropertyRelative("setNativeSize"),
                    new GUIContent("Set Native Size"));
            }
        }
    }

    private void DrawEntryHeader(
        int index,
        SerializedProperty entry,
        SerializedProperty emojiKey,
        ref int removeIndex)
    {
        GUIStyle entryTitleStyle = new(EditorStyles.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
        };

        using (new EditorGUILayout.HorizontalScope())
        {
            string title = string.IsNullOrEmpty(emojiKey.stringValue)
                ? $"Entry {index + 1}"
                : $"Entry {index + 1:00}  {emojiKey.stringValue}";

            Rect foldoutArea = GUILayoutUtility.GetRect(
                GUIContent.none,
                entryTitleStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight + 2f));

            entry.isExpanded = DrawWideFoldoutInRect(
                foldoutArea,
                entry.isExpanded,
                title,
                entryTitleStyle);

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                removeIndex = index;
            }
        }
    }

    private void DrawEntryFields(SerializedProperty emojiKey, SerializedProperty sprite)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.PropertyField(emojiKey);
            EditorGUILayout.PropertyField(sprite);
        }
    }

    private void DrawSpritePreview(SerializedProperty spriteProperty)
    {
        Object spriteObject = spriteProperty.objectReferenceValue;

        Rect rect = GUILayoutUtility.GetRect(
            PreviewSize,
            PreviewSize,
            GUILayout.Width(PreviewSize),
            GUILayout.Height(PreviewSize));

        GUI.Box(rect, GUIContent.none);

        if (spriteObject == null)
            return;

        Texture2D preview = AssetPreview.GetAssetPreview(spriteObject);

        if (preview == null)
            preview = AssetPreview.GetMiniThumbnail(spriteObject);

        if (preview == null)
            return;

        GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
    }

    private void DrawLayoutSeedButtons(SerializedProperty entry)
    {
        EditorGUILayout.LabelField("Load Built-in Layout Seed", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Default"))
                ApplyLayoutToEntry(entry, CharacterEmojiLayout.Default);

            if (GUILayout.Button("Head Left"))
                ApplyLayoutToEntry(entry, CharacterEmojiLayout.HeadLeft);
            
            if (GUILayout.Button("Head Right"))
                ApplyLayoutToEntry(entry, CharacterEmojiLayout.HeadRight);

            if (GUILayout.Button("Above Head"))
                ApplyLayoutToEntry(entry, CharacterEmojiLayout.AboveHead);
        }
        
        // using (new EditorGUILayout.HorizontalScope())
        // {
        //     if (GUILayout.Button("Default"))
        //         ApplyLayoutToEntry(entry, CharacterEmojiLayout.Default);
        // }
    }

    private void DrawSavedLayoutActions(SerializedProperty entry)
    {
        string foldoutKey = entry.propertyPath + ".SavedLayoutActions";

        if (!_entrySavedLayoutFoldouts.TryGetValue(foldoutKey, out bool isExpanded))
            isExpanded = false;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Rect headerRect = EditorGUILayout.GetControlRect(
                false,
                EditorGUIUtility.singleLineHeight + 2f);

            Rect countRect = headerRect;
            countRect.xMin = countRect.xMax - 56f;

            Rect foldoutRect = headerRect;
            foldoutRect.xMax = countRect.xMin - 4f;

            isExpanded = DrawWideFoldoutInRect(
                foldoutRect,
                isExpanded,
                "Saved Layout Slots",
                EditorStyles.boldLabel);

            _entrySavedLayoutFoldouts[foldoutKey] = isExpanded;

            if (_savedLayouts != null && _savedLayouts.arraySize > 0)
            {
                EditorGUI.LabelField(
                    countRect,
                    $"{_savedLayouts.arraySize} slots",
                    EditorStyles.miniLabel);
            }

            if (!isExpanded)
                return;

            if (_savedLayouts == null || _savedLayouts.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No saved layout slots. Add a slot above to save/load layouts.",
                    MessageType.Info);
                return;
            }

            for (int i = 0; i < _savedLayouts.arraySize; i++)
            {
                SerializedProperty slot = _savedLayouts.GetArrayElementAtIndex(i);

                SerializedProperty label = slot.FindPropertyRelative("label");
                SerializedProperty slotLayout = slot.FindPropertyRelative("layout");

                string labelText = string.IsNullOrEmpty(label.stringValue)
                    ? $"Slot {i + 1}"
                    : $"Slot {i + 1}: {label.stringValue}";

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(labelText);

                    Color previousColor = GUI.backgroundColor;
                    
                    GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
                    if (GUILayout.Button("Load", GUILayout.Width(64f)))
                    {
                        LoadSlotLayoutToEntry(entry, slotLayout, i);
                    }

                    GUI.backgroundColor = new Color(1f, 0.78f, 0.45f);
                    if (GUILayout.Button("Save", GUILayout.Width(64f)))
                    {
                        SaveEntryLayoutToSlot(entry, slotLayout, i);
                    }

                    GUI.backgroundColor = previousColor;
                }
            }
        }
    }

    private void ApplyLayoutToEntry(SerializedProperty entry, CharacterEmojiLayout layoutValue)
    {
        Undo.RecordObject(target, "Apply Emoji Layout Seed");

        SerializedProperty layout = entry.FindPropertyRelative("layout");
        WriteLayout(layout, layoutValue);

        EditorUtility.SetDirty(target);
    }

    private void SaveEntryLayoutToSlot(
        SerializedProperty entry,
        SerializedProperty slotLayout,
        int slotIndex)
    {
        string entryKey = "(empty key)";

        SerializedProperty emojiKey = entry.FindPropertyRelative("emojiKey");
        if (emojiKey != null && !string.IsNullOrEmpty(emojiKey.stringValue))
            entryKey = emojiKey.stringValue;

        SerializedProperty slot = _savedLayouts.GetArrayElementAtIndex(slotIndex);
        SerializedProperty label = slot.FindPropertyRelative("label");

        string slotLabel = label != null && !string.IsNullOrEmpty(label.stringValue)
            ? label.stringValue
            : $"Layout {slotIndex + 1}";

        bool confirmed = EditorUtility.DisplayDialog(
            "Save Emoji Layout",
            $"Save current layout of '{entryKey}' into slot {slotIndex + 1}: '{slotLabel}'?\n\n" +
            "This will overwrite the saved layout currently stored in that slot.",
            "Save",
            "Cancel");

        if (!confirmed)
            return;

        Undo.RecordObject(target, "Save Emoji Layout Slot");

        SerializedProperty entryLayout = entry.FindPropertyRelative("layout");
        CharacterEmojiLayout value = ReadLayout(entryLayout);

        WriteLayout(slotLayout, value);

        EditorUtility.SetDirty(target);

        Debug.Log(
            $"[CharacterEmojiLibrarySOEditor] Saved entry '{entryKey}' layout to slot {slotIndex + 1} '{slotLabel}'.",
            target);
    }

    private void LoadSlotLayoutToEntry(
        SerializedProperty entry,
        SerializedProperty slotLayout,
        int slotIndex)
    {
        Undo.RecordObject(target, "Load Emoji Layout Slot");

        CharacterEmojiLayout value = ReadLayout(slotLayout);

        SerializedProperty entryLayout = entry.FindPropertyRelative("layout");
        WriteLayout(entryLayout, value);

        EditorUtility.SetDirty(target);

        Debug.Log(
            $"[CharacterEmojiLibrarySOEditor] Loaded layout from slot {slotIndex + 1}.",
            target);
    }

    private void AddEntry()
    {
        Undo.RecordObject(target, "Add Emoji Entry");

        _entries.InsertArrayElementAtIndex(0);

        SerializedProperty entry = _entries.GetArrayElementAtIndex(0);

        entry.isExpanded = true;

        entry.FindPropertyRelative("emojiKey").stringValue = "";
        entry.FindPropertyRelative("sprite").objectReferenceValue = null;

        WriteLayout(entry.FindPropertyRelative("layout"), CharacterEmojiLayout.Default);

        _scroll = Vector2.zero;

        EditorUtility.SetDirty(target);
    }

    private void RemoveEntry(int index)
    {
        Undo.RecordObject(target, "Remove Emoji Entry");

        _entries.DeleteArrayElementAtIndex(index);

        EditorUtility.SetDirty(target);
    }

    private void AddLayoutSlot()
    {
        Undo.RecordObject(target, "Add Emoji Layout Slot");

        int index = _savedLayouts.arraySize;
        _savedLayouts.InsertArrayElementAtIndex(index);

        SerializedProperty slot = _savedLayouts.GetArrayElementAtIndex(index);

        slot.FindPropertyRelative("label").stringValue = $"Layout {index + 1}";
        WriteLayout(slot.FindPropertyRelative("layout"), CharacterEmojiLayout.Default);

        EditorUtility.SetDirty(target);
    }

    private void RemoveLayoutSlot(int index)
    {
        Undo.RecordObject(target, "Remove Emoji Layout Slot");

        _savedLayouts.DeleteArrayElementAtIndex(index);

        EditorUtility.SetDirty(target);
    }

    private void ValidateEntries()
    {
        serializedObject.ApplyModifiedProperties();

        HashSet<string> usedKeys = new();
        bool hasIssue = false;

        for (int i = 0; i < _entries.arraySize; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);

            SerializedProperty emojiKey = entry.FindPropertyRelative("emojiKey");
            SerializedProperty sprite = entry.FindPropertyRelative("sprite");
            SerializedProperty layout = entry.FindPropertyRelative("layout");
            SerializedProperty localScale = layout.FindPropertyRelative("localScale");

            string key = emojiKey.stringValue;
            Vector3 scale = localScale.vector3Value;

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry {i} has empty emojiKey.", target);
                hasIssue = true;
            }
            else if (!usedKeys.Add(key))
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Duplicate emojiKey '{key}'.", target);
                hasIssue = true;
            }

            if (sprite.objectReferenceValue == null)
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry '{key}' has no sprite.", target);
                hasIssue = true;
            }

            if (Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f))
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry '{key}' has zero localScale x/y.", target);
                hasIssue = true;
            }
        }

        if (!hasIssue)
            Debug.Log("[CharacterEmojiLibrarySOEditor] Validation complete. No issues found.", target);
    }

    private void SortByKey()
    {
        serializedObject.ApplyModifiedProperties();

        List<EntrySnapshot> snapshots = new();

        for (int i = 0; i < _entries.arraySize; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);

            EntrySnapshot snapshot = new()
            {
                emojiKey = entry.FindPropertyRelative("emojiKey").stringValue,
                sprite = entry.FindPropertyRelative("sprite").objectReferenceValue as Sprite,
                layout = ReadLayout(entry.FindPropertyRelative("layout"))
            };

            snapshots.Add(snapshot);
        }

        snapshots.Sort((a, b) => string.CompareOrdinal(a.emojiKey, b.emojiKey));

        Undo.RecordObject(target, "Sort Emoji Entries By Key");

        _entries.arraySize = snapshots.Count;

        for (int i = 0; i < snapshots.Count; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);

            entry.FindPropertyRelative("emojiKey").stringValue = snapshots[i].emojiKey;
            entry.FindPropertyRelative("sprite").objectReferenceValue = snapshots[i].sprite;

            WriteLayout(entry.FindPropertyRelative("layout"), snapshots[i].layout);
        }

        EditorUtility.SetDirty(target);
    }

    private CharacterEmojiLayout ReadLayout(SerializedProperty layout)
    {
        CharacterEmojiLayout value = CharacterEmojiLayout.Default;

        value.anchoredPosition = layout.FindPropertyRelative("anchoredPosition").vector2Value;
        value.localScale = layout.FindPropertyRelative("localScale").vector3Value;
        value.rotationZ = layout.FindPropertyRelative("rotationZ").floatValue;
        value.preserveAspect = layout.FindPropertyRelative("preserveAspect").boolValue;
        value.setNativeSize = layout.FindPropertyRelative("setNativeSize").boolValue;

        return value;
    }

    private void WriteLayout(SerializedProperty layout, CharacterEmojiLayout value)
    {
        layout.FindPropertyRelative("anchoredPosition").vector2Value = value.anchoredPosition;
        layout.FindPropertyRelative("localScale").vector3Value = value.localScale;
        layout.FindPropertyRelative("rotationZ").floatValue = value.rotationZ;
        layout.FindPropertyRelative("preserveAspect").boolValue = value.preserveAspect;
        layout.FindPropertyRelative("setNativeSize").boolValue = value.setNativeSize;
    }

    private bool DrawWideFoldoutInRect(
        Rect rect,
        bool isExpanded,
        string label,
        GUIStyle style)
    {
        if (style == null)
            style = EditorStyles.label;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            isExpanded = !isExpanded;
            e.Use();
        }

        string prefix = isExpanded ? "▼ " : "▶ ";

        Rect labelRect = rect;
        labelRect.xMin += 4f;

        EditorGUI.LabelField(labelRect, prefix + label, style);

        return isExpanded;
    }

    private sealed class EntrySnapshot
    {
        public string emojiKey;
        public Sprite sprite;
        public CharacterEmojiLayout layout;
    }
}