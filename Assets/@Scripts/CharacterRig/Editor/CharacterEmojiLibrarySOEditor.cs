using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CharacterEmojiLibrarySO))]
public sealed class CharacterEmojiLibrarySOEditor : Editor
{
    private SerializedProperty _entries;
    private SerializedProperty _savedPlacements;

    private Vector2 _scroll;
    private bool _showSavedPlacements = true;
    private bool _showEntryList;

    private ReorderableList _entryQuickList;

    private readonly Dictionary<string, bool> _entrySavedPlacementFoldouts = new();

    private const float PreviewSize = 64f;

    private void OnEnable()
    {
        _entries = serializedObject.FindProperty("entries");

        // Latest field name is savedPlacements.
        // Fallback is left here only to keep the editor usable during script/data migration.
        _savedPlacements = serializedObject.FindProperty("savedPlacements") ??
                           serializedObject.FindProperty("savedLayouts");

        BuildEntryQuickList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeaderTools();

        EditorGUILayout.Space(8);

        DrawSavedPlacements();

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
                int count = _entries != null ? _entries.arraySize : 0;

                EditorGUILayout.LabelField(
                    $"Character Emoji Library ({count})",
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

                if (GUILayout.Button("Add Placement Slot"))
                {
                    AddPlacementSlot();
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

    private void DrawSavedPlacements()
    {
        if (_savedPlacements == null)
        {
            EditorGUILayout.HelpBox("Failed to find 'savedPlacements' property.", MessageType.Error);
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

            _showSavedPlacements = DrawWideFoldoutInRect(
                foldoutRect,
                _showSavedPlacements,
                $"Saved Placement Slots ({_savedPlacements.arraySize})",
                EditorStyles.boldLabel);

            if (GUI.Button(buttonRect, "+"))
            {
                AddPlacementSlot();
            }

            if (!_showSavedPlacements)
                return;

            if (_savedPlacements.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No saved placement slots yet. Add a slot, then save an entry placement into it.",
                    MessageType.Info);
                return;
            }

            int removeIndex = -1;

            for (int i = 0; i < _savedPlacements.arraySize; i++)
            {
                SerializedProperty slot = _savedPlacements.GetArrayElementAtIndex(i);

                SerializedProperty label = slot.FindPropertyRelative("label");
                SerializedProperty placement = FindPlacementProperty(slot);

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

                    DrawPlacementProperty(placement, "Placement");
                }
            }

            if (removeIndex >= 0)
            {
                RemovePlacementSlot(removeIndex);
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
            SerializedProperty placement = FindPlacementProperty(entry);

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

                    DrawPlacementSeedButtons(entry);

                    EditorGUILayout.Space(4);

                    DrawSavedPlacementActions(entry);

                    EditorGUILayout.Space(4);

                    DrawPlacementProperty(placement, "Placement");
                }
            }

            EditorGUILayout.Space(4);
        }

        if (removeIndex >= 0)
            RemoveEntry(removeIndex);
    }

    private void DrawPlacementProperty(SerializedProperty placement, string title)
    {
        if (placement == null)
        {
            EditorGUILayout.HelpBox("Failed to find placement property.", MessageType.Error);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUIStyle placementTitleStyle = new(EditorStyles.label)
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

            placement.isExpanded = DrawWideFoldoutInRect(
                foldoutRect,
                placement.isExpanded,
                title,
                placementTitleStyle);

            if (!placement.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    placement.FindPropertyRelative("focusPreset"),
                    new GUIContent("Focus Preset"));

                EditorGUILayout.PropertyField(
                    placement.FindPropertyRelative("offsetFromFocusInRigSpace"),
                    new GUIContent("Offset From Focus (RigSpace)"));

                EditorGUILayout.PropertyField(
                    placement.FindPropertyRelative("localScale"),
                    new GUIContent("Local Scale"));

                EditorGUILayout.PropertyField(
                    placement.FindPropertyRelative("rotationZ"),
                    new GUIContent("Rotation Z"));

                EditorGUILayout.PropertyField(
                    placement.FindPropertyRelative("preserveAspect"),
                    new GUIContent("Preserve Aspect"));

                EditorGUILayout.PropertyField(
                    placement.FindPropertyRelative("setNativeSize"),
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

    private void DrawEntryFields(
        SerializedProperty emojiKey,
        SerializedProperty sprite)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.PropertyField(emojiKey);
            EditorGUILayout.PropertyField(sprite);
        }
    }

    private void DrawSpritePreview(SerializedProperty spriteProperty)
    {
        UnityEngine.Object spriteObject = spriteProperty.objectReferenceValue;

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

    private void DrawPlacementSeedButtons(SerializedProperty entry)
    {
        EditorGUILayout.LabelField("Load Built-in Placement Seed", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Default"))
                ApplyPlacementToEntry(entry, CharacterEmojiPlacement.Default);

            if (GUILayout.Button("Face Left"))
                ApplyPlacementToEntry(entry, CharacterEmojiPlacement.FaceLeft);

            if (GUILayout.Button("Face Right"))
                ApplyPlacementToEntry(entry, CharacterEmojiPlacement.FaceRight);

            if (GUILayout.Button("Above Face"))
                ApplyPlacementToEntry(entry, CharacterEmojiPlacement.AboveFace);
        }
    }

    private void DrawSavedPlacementActions(SerializedProperty entry)
    {
        string foldoutKey = entry.propertyPath + ".SavedPlacementActions";

        if (!_entrySavedPlacementFoldouts.TryGetValue(foldoutKey, out bool isExpanded))
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
                "Saved Placement Slots",
                EditorStyles.boldLabel);

            _entrySavedPlacementFoldouts[foldoutKey] = isExpanded;

            if (_savedPlacements != null && _savedPlacements.arraySize > 0)
            {
                EditorGUI.LabelField(
                    countRect,
                    $"{_savedPlacements.arraySize} slots",
                    EditorStyles.miniLabel);
            }

            if (!isExpanded)
                return;

            if (_savedPlacements == null || _savedPlacements.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No saved placement slots. Add a slot above to save/load placements.",
                    MessageType.Info);
                return;
            }

            for (int i = 0; i < _savedPlacements.arraySize; i++)
            {
                SerializedProperty slot = _savedPlacements.GetArrayElementAtIndex(i);

                SerializedProperty label = slot.FindPropertyRelative("label");
                SerializedProperty slotPlacement = FindPlacementProperty(slot);

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
                        LoadSlotPlacementToEntry(entry, slotPlacement, i);
                    }

                    GUI.backgroundColor = new Color(1f, 0.78f, 0.45f);
                    if (GUILayout.Button("Save", GUILayout.Width(64f)))
                    {
                        SaveEntryPlacementToSlot(entry, slotPlacement, i);
                    }

                    GUI.backgroundColor = previousColor;
                }
            }
        }
    }

    private void ApplyPlacementToEntry(
        SerializedProperty entry,
        CharacterEmojiPlacement placementValue)
    {
        Undo.RecordObject(target, "Apply Emoji Placement Seed");

        SerializedProperty placement = FindPlacementProperty(entry);
        WritePlacement(placement, placementValue);

        EditorUtility.SetDirty(target);
    }

    private void SaveEntryPlacementToSlot(
        SerializedProperty entry,
        SerializedProperty slotPlacement,
        int slotIndex)
    {
        string entryKey = "(empty key)";

        SerializedProperty emojiKey = entry.FindPropertyRelative("emojiKey");
        if (emojiKey != null && !string.IsNullOrEmpty(emojiKey.stringValue))
            entryKey = emojiKey.stringValue;

        SerializedProperty slot = _savedPlacements.GetArrayElementAtIndex(slotIndex);
        SerializedProperty label = slot.FindPropertyRelative("label");

        string slotLabel = label != null && !string.IsNullOrEmpty(label.stringValue)
            ? label.stringValue
            : $"Placement {slotIndex + 1}";

        bool confirmed = EditorUtility.DisplayDialog(
            "Save Emoji Placement",
            $"Save current placement of '{entryKey}' into slot {slotIndex + 1}: '{slotLabel}'?\n\n" +
            "This will overwrite the saved placement currently stored in that slot.",
            "Save",
            "Cancel");

        if (!confirmed)
            return;

        Undo.RecordObject(target, "Save Emoji Placement Slot");

        SerializedProperty entryPlacement = FindPlacementProperty(entry);
        CharacterEmojiPlacement value = ReadPlacement(entryPlacement);

        WritePlacement(slotPlacement, value);

        EditorUtility.SetDirty(target);

        Debug.Log(
            $"[CharacterEmojiLibrarySOEditor] Saved entry '{entryKey}' placement to slot {slotIndex + 1} '{slotLabel}'.",
            target);
    }

    private void LoadSlotPlacementToEntry(
        SerializedProperty entry,
        SerializedProperty slotPlacement,
        int slotIndex)
    {
        Undo.RecordObject(target, "Load Emoji Placement Slot");

        CharacterEmojiPlacement value = ReadPlacement(slotPlacement);

        SerializedProperty entryPlacement = FindPlacementProperty(entry);
        WritePlacement(entryPlacement, value);

        EditorUtility.SetDirty(target);

        Debug.Log(
            $"[CharacterEmojiLibrarySOEditor] Loaded placement from slot {slotIndex + 1}.",
            target);
    }

    private void AddEntry()
    {
        if (_entries == null)
            return;

        Undo.RecordObject(target, "Add Emoji Entry");

        _entries.InsertArrayElementAtIndex(0);

        SerializedProperty entry = _entries.GetArrayElementAtIndex(0);

        entry.isExpanded = true;

        entry.FindPropertyRelative("emojiKey").stringValue = "";
        entry.FindPropertyRelative("sprite").objectReferenceValue = null;

        WritePlacement(FindPlacementProperty(entry), CharacterEmojiPlacement.Default);

        _scroll = Vector2.zero;

        EditorUtility.SetDirty(target);
    }

    private void RemoveEntry(int index)
    {
        if (_entries == null)
            return;

        Undo.RecordObject(target, "Remove Emoji Entry");

        _entries.DeleteArrayElementAtIndex(index);

        EditorUtility.SetDirty(target);
    }

    private void AddPlacementSlot()
    {
        if (_savedPlacements == null)
            return;

        Undo.RecordObject(target, "Add Emoji Placement Slot");

        int index = _savedPlacements.arraySize;
        _savedPlacements.InsertArrayElementAtIndex(index);

        SerializedProperty slot = _savedPlacements.GetArrayElementAtIndex(index);

        SerializedProperty label = slot.FindPropertyRelative("label");
        if (label != null)
            label.stringValue = $"Placement {index + 1}";

        WritePlacement(FindPlacementProperty(slot), CharacterEmojiPlacement.Default);

        EditorUtility.SetDirty(target);
    }

    private void RemovePlacementSlot(int index)
    {
        if (_savedPlacements == null)
            return;

        Undo.RecordObject(target, "Remove Emoji Placement Slot");

        _savedPlacements.DeleteArrayElementAtIndex(index);

        EditorUtility.SetDirty(target);
    }

    private void ValidateEntries()
    {
        serializedObject.ApplyModifiedProperties();

        if (_entries == null)
            return;

        HashSet<string> usedKeys = new(StringComparer.OrdinalIgnoreCase);
        bool hasIssue = false;

        for (int i = 0; i < _entries.arraySize; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);

            SerializedProperty emojiKey = entry.FindPropertyRelative("emojiKey");
            SerializedProperty sprite = entry.FindPropertyRelative("sprite");
            SerializedProperty placement = FindPlacementProperty(entry);
            SerializedProperty localScale = placement?.FindPropertyRelative("localScale");
            SerializedProperty offset = placement?.FindPropertyRelative("offsetFromFocusInRigSpace");

            string key = (emojiKey.stringValue ?? "").Trim();
            Vector3 scale = localScale != null
                ? localScale.vector3Value
                : Vector3.zero;

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry {i} has empty emojiKey.", target);
                hasIssue = true;
            }
            else if (!usedKeys.Add(key))
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Duplicate emojiKey '{key}' ignoring case.", target);
                hasIssue = true;
            }

            if (sprite.objectReferenceValue == null)
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry '{key}' has no sprite.", target);
                hasIssue = true;
            }

            if (placement == null)
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry '{key}' has no placement property.", target);
                hasIssue = true;
            }

            if (Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f))
            {
                Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry '{key}' has zero localScale x/y.", target);
                hasIssue = true;
            }

            if (offset != null)
            {
                Vector2 offsetValue = offset.vector2Value;
                if (float.IsNaN(offsetValue.x) || float.IsNaN(offsetValue.y))
                {
                    Debug.LogWarning($"[CharacterEmojiLibrarySOEditor] Entry '{key}' has NaN focus offset.", target);
                    hasIssue = true;
                }
            }
        }

        if (!hasIssue)
            Debug.Log("[CharacterEmojiLibrarySOEditor] Validation complete. No issues found.", target);
    }

    private void SortByKey()
    {
        serializedObject.ApplyModifiedProperties();

        if (_entries == null)
            return;

        List<EntrySnapshot> snapshots = new();

        for (int i = 0; i < _entries.arraySize; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);

            EntrySnapshot snapshot = new()
            {
                emojiKey = entry.FindPropertyRelative("emojiKey").stringValue,
                sprite = entry.FindPropertyRelative("sprite").objectReferenceValue as Sprite,
                placement = ReadPlacement(FindPlacementProperty(entry))
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

            WritePlacement(FindPlacementProperty(entry), snapshots[i].placement);
        }

        EditorUtility.SetDirty(target);
    }

    private static SerializedProperty FindPlacementProperty(SerializedProperty owner)
    {
        if (owner == null)
            return null;

        // Latest field name is placement. Fallback is only for migration/editor tolerance.
        return owner.FindPropertyRelative("placement") ??
               owner.FindPropertyRelative("layout");
    }

    private CharacterEmojiPlacement ReadPlacement(SerializedProperty placement)
    {
        CharacterEmojiPlacement value = CharacterEmojiPlacement.Default;

        if (placement == null)
            return value;

        SerializedProperty focusPreset = placement.FindPropertyRelative("focusPreset");
        SerializedProperty offset = placement.FindPropertyRelative("offsetFromFocusInRigSpace");
        SerializedProperty localScale = placement.FindPropertyRelative("localScale");
        SerializedProperty rotationZ = placement.FindPropertyRelative("rotationZ");
        SerializedProperty preserveAspect = placement.FindPropertyRelative("preserveAspect");
        SerializedProperty setNativeSize = placement.FindPropertyRelative("setNativeSize");

        if (focusPreset != null)
            value.focusPreset = (CharacterFocusPreset)focusPreset.intValue;

        if (offset != null)
            value.offsetFromFocusInRigSpace = offset.vector2Value;

        if (localScale != null)
            value.localScale = localScale.vector3Value;

        if (rotationZ != null)
            value.rotationZ = rotationZ.floatValue;

        if (preserveAspect != null)
            value.preserveAspect = preserveAspect.boolValue;

        if (setNativeSize != null)
            value.setNativeSize = setNativeSize.boolValue;

        return value;
    }

    private void WritePlacement(
        SerializedProperty placement,
        CharacterEmojiPlacement value)
    {
        if (placement == null)
            return;

        SerializedProperty focusPreset = placement.FindPropertyRelative("focusPreset");
        SerializedProperty offset = placement.FindPropertyRelative("offsetFromFocusInRigSpace");
        SerializedProperty localScale = placement.FindPropertyRelative("localScale");
        SerializedProperty rotationZ = placement.FindPropertyRelative("rotationZ");
        SerializedProperty preserveAspect = placement.FindPropertyRelative("preserveAspect");
        SerializedProperty setNativeSize = placement.FindPropertyRelative("setNativeSize");

        if (focusPreset != null)
            focusPreset.intValue = (int)value.focusPreset;

        if (offset != null)
            offset.vector2Value = value.offsetFromFocusInRigSpace;

        if (localScale != null)
            localScale.vector3Value = value.localScale;

        if (rotationZ != null)
            rotationZ.floatValue = value.rotationZ;

        if (preserveAspect != null)
            preserveAspect.boolValue = value.preserveAspect;

        if (setNativeSize != null)
            setNativeSize.boolValue = value.setNativeSize;
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
        public CharacterEmojiPlacement placement;
    }
}
