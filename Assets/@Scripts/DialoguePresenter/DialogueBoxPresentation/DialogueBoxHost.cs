using System;
using UnityEngine;

/// <summary>
/// 대화 박스의 종류. <b>더 이상 "어느 뷰인가"가 아니다</b> —
/// 박스는 <c>DialogueSurfaceBox</c> 하나뿐이고, kind는
/// <c>DialogueSurfaceLayoutPresetDBSO</c>에서 어느 레이아웃 프리셋을 쓸지를 가리킨다.
/// </summary>
public enum DialogueBoxKind
{
    Portrait = 0,
    Speaker = 1,
    LetterBox = 2,
    OnlyText = 3,
    BlackBook = 4,
    Surface = 5,
}

public enum OptionsBoxKind
{
    Default = 0,
}

[Serializable]
public struct OptionsBoxHostEntry
{
    public OptionsBoxKind kind;

    [Tooltip("IPresentationOptionsBoxView 구현체")]
    public MonoBehaviour view;
}

/// <summary>
/// 선택지 박스 뷰의 해석기.
///
/// 대화 박스 쪽은 걷혔다 — 뷰가 하나뿐이라 해석할 것이 없고,
/// <c>DialogueBoxPresentationController</c>가 <c>DialogueSurfaceBox</c>를 직접 들고 있다.
///
/// ⚠ 선택지 박스도 같은 길을 갈 예정이다: 단일 표면 + 레이아웃 프리셋 데이터.
/// 그때 이 호스트 전체가 사라진다.
/// </summary>
public sealed class DialogueBoxHost : MonoBehaviour
{
    [Header("Options Box Entries")]
    [SerializeField] private OptionsBoxHostEntry[] optionEntries;

    public IPresentationOptionsBoxView ResolveOptionsTarget(OptionsBoxKind kind)
    {
        if (optionEntries == null || optionEntries.Length == 0)
        {
            Debug.LogWarning($"[DialogueBoxHost] No options box entries are assigned. kind={kind}", this);
            return null;
        }

        for (int i = 0; i < optionEntries.Length; i++)
        {
            if (optionEntries[i].kind != kind)
                continue;

            MonoBehaviour behaviour = optionEntries[i].view;

            if (!behaviour)
            {
                Debug.LogWarning($"[DialogueBoxHost] Options view is null. kind={optionEntries[i].kind}", this);
                return null;
            }

            IPresentationOptionsBoxView view = behaviour as IPresentationOptionsBoxView;

            if (view == null)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] Options view must implement IPresentationOptionsBoxView. kind={optionEntries[i].kind}, go={behaviour.name}",
                    behaviour);

                return null;
            }

            return view;
        }

        Debug.LogWarning($"[DialogueBoxHost] Options entry not found. kind={kind}", this);
        return null;
    }

    public void HideAllOptionsBoxes()
    {
        if (optionEntries == null)
            return;

        for (int i = 0; i < optionEntries.Length; i++)
        {
            MonoBehaviour behaviour = optionEntries[i].view;
            if (!behaviour)
                continue;

            IPresentationOptionsBoxView view = behaviour as IPresentationOptionsBoxView;
            if (view == null)
                continue;

            view.SetVisibleImmediate(false);
        }
    }

    public void HideAllOptionsBoxesExcept(IPresentationOptionsBoxView target)
    {
        if (optionEntries == null)
            return;

        for (int i = 0; i < optionEntries.Length; i++)
        {
            MonoBehaviour behaviour = optionEntries[i].view;
            if (!behaviour)
                continue;

            IPresentationOptionsBoxView view = behaviour as IPresentationOptionsBoxView;
            if (view == null)
                continue;

            if (ReferenceEquals(view, target))
                continue;

            view.SetVisibleImmediate(false);
        }
    }

    #region Validate

    private void OnValidate()
    {
        if (optionEntries == null || optionEntries.Length == 0)
            return;

        ValidateDuplicateOptionsKinds();
        ValidateOptionsEntryViews();
    }

    private void ValidateDuplicateOptionsKinds()
    {
        for (int i = 0; i < optionEntries.Length; i++)
        {
            for (int j = i + 1; j < optionEntries.Length; j++)
            {
                if (optionEntries[i].kind != optionEntries[j].kind)
                    continue;

                Debug.LogWarning(
                    $"[DialogueBoxHost] Duplicate entry for OptionsBoxKind.{optionEntries[i].kind}. indices={i}, {j}",
                    this);
            }
        }
    }

    private void ValidateOptionsEntryViews()
    {
        for (int i = 0; i < optionEntries.Length; i++)
        {
            MonoBehaviour behaviour = optionEntries[i].view;

            if (behaviour == null)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] Options entry view is null. index={i}, kind={optionEntries[i].kind}",
                    this);

                continue;
            }

            if (behaviour is IPresentationOptionsBoxView)
                continue;

            Debug.LogWarning(
                $"[DialogueBoxHost] Options entry view must implement IPresentationOptionsBoxView. index={i}, kind={optionEntries[i].kind}, go={behaviour.name}",
                behaviour);
        }
    }

    #endregion
}
