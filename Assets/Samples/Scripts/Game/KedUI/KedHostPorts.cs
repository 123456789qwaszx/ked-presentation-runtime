using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ked.Progression;
using Ked.Progression.Save;
using Ked.Samples.UI;
using UnityEngine;

// 새 예제 UI(Ked.Samples.UI)가 묻는 호스트 계약의 구현. UI 어셈블리는 기본 어셈블리를 모르므로,
// 호스트가 가진 것(회차 입구 · 수동 저장 유즈케이스 · 예제 전환)을 여기서 감싸 넘긴다.
// 반대로 호스트가 UI에 알리는 것(회차 끝)도 여기서 UI의 말로 옮긴다.
// 저장 포맷·진행 규칙은 바꾸지 않는다 — 기존 ManualSaveFlow · SaveSlotService · IPlaythroughEntry를 그대로 부른다.

/// <summary>타이틀의 회차 입구. 연출 단독은 NodePlaythrough, 진행·저장은 ProgressionLauncher.</summary>
public sealed class KedTitlePort : IKedTitlePort
{
    private readonly IPlaythroughEntry _entry;
    private readonly Func<bool> _hasResumePoint;
    private readonly Action _switchExample;

    public KedTitlePort(
        IPlaythroughEntry entry,
        Func<bool> hasResumePoint,
        KedExampleCard example,
        Action switchExample)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _hasResumePoint = hasResumePoint;
        _switchExample = switchExample;
        Example = example;
    }

    public KedExampleCard Example { get; }

    public bool HasResumePoint
    {
        get
        {
            try
            {
                return _hasResumePoint != null && _hasResumePoint();
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Ked UI] 이어하기 기록을 확인하지 못했다 — 이어하기를 막는다. {error.Message}");
                return false;
            }
        }
    }

    public bool CanSwitchExample => _switchExample != null;

    public void StartNew() => _entry.StartNew();

    public void Resume() => _entry.Resume();

    public void SwitchExample() => _switchExample?.Invoke();

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

/// <summary>
/// 회차 끝 — 끝을 정한 쪽(연출 단독 NodePlaythrough · 진행 SceneRunner)이 부르면 UI에 끝 카드를 띄운다.
/// 진행 타입(챕터 · 에피소드 · 상태)은 여기서 UI의 끝 내용(KedPlaythroughEnd)으로 옮긴다 — UI는 진행 런타임을 모른다.
/// </summary>
public sealed class KedPlaythroughEndView : IEpisodeEndView, IChapterEndView
{
    private readonly KedUIHost _ui;

    public KedPlaythroughEndView(KedUIHost ui)
    {
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    }

    // 노드 하나가 회차다. 이름은 비워 둔다 — UI가 예제 카드의 제목으로 채운다.
    public void Show()
    {
        _ui.ShowPlaythroughEnd(new KedPlaythroughEnd
        {
            Kind = KedPlaythroughEnd.EndKind.Episode,
        });
    }

    public void Show(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
    {
        var stats = new List<KedPlaythroughEnd.Stat>(chapter.Stats.Count);

        foreach (StatDefinition stat in chapter.Stats)
        {
            if (state == null || !state.Stats.TryGetValue(stat.Key, out int value))
                continue;

            stats.Add(new KedPlaythroughEnd.Stat(
                string.IsNullOrWhiteSpace(stat.DisplayName) ? stat.Key : stat.DisplayName,
                value));
        }

        _ui.ShowPlaythroughEnd(new KedPlaythroughEnd
        {
            Kind = KedPlaythroughEnd.EndKind.Chapter,
            Title = string.IsNullOrWhiteSpace(chapter.DisplayName) ? chapter.ChapterId : chapter.DisplayName,
            LastEpisode = lastEpisode?.Title,
            Stats = stats,
        });
    }
}

/// <summary>수동 저장 슬롯 — ManualSaveFlow(저장·불러오기·갈라지기)와 SaveSlotService(목록·본문 읽기).</summary>
public sealed class KedSaveSlotPort : IKedSaveSlotPort
{
    private readonly ManualSaveFlow _flow;
    private readonly SaveSlotService _slots;

    public KedSaveSlotPort(ManualSaveFlow flow, SaveSlotService slots)
    {
        _flow = flow ?? throw new ArgumentNullException(nameof(flow));
        _slots = slots ?? throw new ArgumentNullException(nameof(slots));
    }

    public bool CanSave => _flow.CanSave;

    // 진행 저장은 읽을 수 없는 목록(옛 형식·손상)을 예외로 알린다. 화면은 빈 목록으로 열고 이유를 남긴다.
    public bool TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots)
    {
        try
        {
            slots = _slots.Slots;
            return true;
        }
        catch (InvalidDataException error)
        {
            Debug.LogWarning(
                $"[수동 저장] 저장 목록을 읽지 못해 빈 목록으로 연다 — {error.Message} " +
                "옛 형식이면 저장 폴더의 save-slots.json과 save-slot-data/를 치우면 된다.");

            slots = Array.Empty<SaveSlotEntry>();
            return false;
        }
    }

    // 본문을 실제로 읽어 본다(콘텐츠 버전이 다르면 SaveSlotService.Load가 거부한다). 화면은 읽을 수 없는 슬롯을 막는다.
    public bool CanLoad(string slotId)
    {
        try
        {
            return _slots.Load(slotId) != null;
        }
        catch (Exception error)
        {
            Debug.Log($"[수동 저장] 슬롯 {slotId}은 지금 불러올 수 없다 — {error.Message}");
            return false;
        }
    }

    public bool TryCreate() => _flow.TryCreate();

    public bool TryOverwrite(string slotId) => _flow.TryOverwrite(slotId);

    public Task LoadAsync(string slotId, Action onTransitionStarted) =>
        _flow.LoadAsync(slotId, onTransitionStarted);

    public bool CanForkFrom(DialogueLogEntry entry) => _flow.CanForkFrom(entry);

    public Task ForkFromBacklogAsync(DialogueLogEntry entry, Action onTransitionStarted) =>
        _flow.ForkFromBacklogAsync(entry, onTransitionStarted);
}
