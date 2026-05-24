using System;
using System.Collections.Generic;
using UnityEngine;

public static class EpisodeSelectionModelBuilder
{
    private const float MainStepX = 400f;
    private const float MainY = 0f;
    private const float BranchOffsetY = 220f;
    private const float NextOffsetX = 400f;

    public static EpisodeSelectionPanelModel Build(
        int chapterId,
        string selectedEpisodeId,
        IEpisodeProgress progress,
        IEpisodePlayLookup lookup)
    {
        ChapterMetaModel chapterMeta = BuildChapterMeta(chapterId, lookup);

        if (lookup == null ||
            !lookup.TryGetChapter(chapterId, out ChapterSpec chapter) ||
            chapter == null ||
            chapter.episodes == null ||
            chapter.episodes.Length == 0)
        {
            return new EpisodeSelectionPanelModel(
                chapterId,
                chapterMeta,
                new EpisodeGraphModel(Array.Empty<EpisodeNodeModel>()),
                ""
            );
        }

        VisibleSet visible = BuildVisibleSet(chapter, progress, lookup);

        if (visible.MainIds.Count == 0)
        {
            return new EpisodeSelectionPanelModel(
                chapterId,
                chapterMeta,
                new EpisodeGraphModel(Array.Empty<EpisodeNodeModel>()),
                ""
            );
        }

        PositionMap positions = CalculatePositions(visible, lookup);

        List<EpisodeNodeModel> nodes = MaterializeNodes(
            visible,
            positions,
            progress,
            lookup,
            selectedEpisodeId
        );

        string effectiveSelected = ResolveSelectedEpisodeId(
            visible.MainIds,
            selectedEpisodeId,
            progress
        );

        return new EpisodeSelectionPanelModel(
            chapterId,
            chapterMeta,
            new EpisodeGraphModel(nodes),
            effectiveSelected
        );
    }

    private sealed class VisibleSet
    {
        public readonly List<string> MainIds = new();
        public readonly HashSet<string> AllVisible = new(StringComparer.Ordinal);
        public readonly Dictionary<string, EpisodeNodeKind> Types = new(StringComparer.Ordinal);

        public void AddMain(string id)
        {
            MainIds.Add(id);
            AllVisible.Add(id);
            Types[id] = EpisodeNodeKind.Main;
        }

        public void AddBranch(string id)
        {
            AllVisible.Add(id);
            Types[id] = EpisodeNodeKind.Branch;
        }

        public void AddBranchChain(string id)
        {
            AllVisible.Add(id);
            Types[id] = EpisodeNodeKind.BranchChain;
        }

        public void AddEnding(string id)
        {
            AllVisible.Add(id);
            Types[id] = EpisodeNodeKind.Ending;
        }
    }

    private static VisibleSet BuildVisibleSet(
        ChapterSpec chapter,
        IEpisodeProgress progress,
        IEpisodePlayLookup lookup)
    {
        VisibleSet visible = new VisibleSet();

        if (progress == null || lookup == null)
            return visible;

        List<string> mainLine = BuildMainLineIds(chapter, lookup);

        for (int i = 0; i < mainLine.Count; i++)
        {
            string id = mainLine[i];

            if (progress.IsEpisodeUnlocked(id))
                visible.AddMain(id);
        }

        if (visible.MainIds.Count == 0)
            return visible;

        List<string> branchStarts = new List<string>();

        for (int i = 0; i < visible.MainIds.Count; i++)
        {
            string mainId = visible.MainIds[i];

            if (!lookup.TryGetEpisode(mainId, out EpisodeSpec episode) || episode == null)
                continue;

            AddBranchIfUnlocked(episode.branchUpperTo);
            AddBranchIfUnlocked(episode.branchMiddleTo);
            AddBranchIfUnlocked(episode.branchLowerTo);

            void AddBranchIfUnlocked(string branchId)
            {
                if (string.IsNullOrEmpty(branchId))
                    return;

                if (!progress.IsEpisodeUnlocked(branchId))
                    return;

                if (visible.AllVisible.Contains(branchId))
                    return;

                visible.AddBranch(branchId);
                branchStarts.Add(branchId);
            }
        }

        for (int i = 0; i < branchStarts.Count; i++)
            ExpandBranchChain(branchStarts[i], visible, progress, lookup);

        List<string> allIds = new List<string>(visible.AllVisible);

        for (int i = 0; i < allIds.Count; i++)
        {
            string id = allIds[i];

            if (!lookup.TryGetEpisode(id, out EpisodeSpec episode) || episode == null)
                continue;

            if (string.IsNullOrEmpty(episode.next))
                continue;

            if (!progress.IsEpisodeUnlocked(episode.next))
                continue;

            if (visible.AllVisible.Contains(episode.next))
                continue;

            if (!lookup.TryGetEpisode(episode.next, out EpisodeSpec nextEpisode) || nextEpisode == null)
                continue;

            if (nextEpisode.isEnding)
                visible.AddEnding(episode.next);
        }

        return visible;
    }

    private static void ExpandBranchChain(
        string startId,
        VisibleSet visible,
        IEpisodeProgress progress,
        IEpisodePlayLookup lookup)
    {
        string current = startId;

        for (int guard = 0; guard < 64; guard++)
        {
            if (!lookup.TryGetEpisode(current, out EpisodeSpec episode) || episode == null)
                break;

            if (string.IsNullOrEmpty(episode.next))
                break;

            string nextId = episode.next;

            if (visible.AllVisible.Contains(nextId))
                break;

            if (!progress.IsEpisodeUnlocked(nextId))
                break;

            if (!lookup.TryGetEpisode(nextId, out EpisodeSpec nextEpisode) || nextEpisode == null)
                break;

            if (nextEpisode.isEnding)
                break;

            visible.AddBranchChain(nextId);
            current = nextId;
        }
    }

    private sealed class PositionMap
    {
        private readonly Dictionary<string, Vector2> _positions = new(StringComparer.Ordinal);

        public void Set(string id, Vector2 pos)
        {
            _positions[id] = pos;
        }

        public bool TryGet(string id, out Vector2 pos)
        {
            return _positions.TryGetValue(id, out pos);
        }

        public Vector2 Get(string id)
        {
            return _positions.TryGetValue(id, out Vector2 pos)
                ? pos
                : Vector2.zero;
        }
    }

    private static PositionMap CalculatePositions(
        VisibleSet visible,
        IEpisodePlayLookup lookup)
    {
        PositionMap positions = new PositionMap();
        float extraX = 0f;

        for (int i = 0; i < visible.MainIds.Count; i++)
        {
            string id = visible.MainIds[i];

            Vector2 pos = new Vector2(i * MainStepX + extraX, MainY);
            positions.Set(id, pos);

            if (!lookup.TryGetEpisode(id, out EpisodeSpec episode) || episode == null)
                continue;

            if (IsHub(episode))
            {
                int columns = EstimateHubBranchColumns(lookup, episode);
                extraX += MainStepX * columns;
            }
        }

        for (int i = 0; i < visible.MainIds.Count; i++)
        {
            string mainId = visible.MainIds[i];

            if (!lookup.TryGetEpisode(mainId, out EpisodeSpec episode) || episode == null)
                continue;

            if (!positions.TryGet(mainId, out Vector2 mainPos))
                continue;

            PositionBranches(episode, mainPos, positions, visible);
        }

        foreach (string id in visible.AllVisible)
        {
            if (!visible.Types.TryGetValue(id, out EpisodeNodeKind type))
                continue;

            if (type != EpisodeNodeKind.Branch)
                continue;

            PositionBranchChain(id, positions, visible, lookup);
        }

        foreach (string id in visible.AllVisible)
        {
            if (!visible.Types.TryGetValue(id, out EpisodeNodeKind type))
                continue;

            if (type != EpisodeNodeKind.Ending)
                continue;

            string ownerId = FindEndingOwner(id, visible, lookup);
            if (string.IsNullOrEmpty(ownerId))
                continue;

            if (!positions.TryGet(ownerId, out Vector2 ownerPos))
                continue;

            positions.Set(id, ownerPos + new Vector2(NextOffsetX, 0f));
        }

        return positions;
    }

    private static void PositionBranches(
        EpisodeSpec hubEpisode,
        Vector2 hubPos,
        PositionMap positions,
        VisibleSet visible)
    {
        List<string> branches = new List<string>(3);

        if (!string.IsNullOrEmpty(hubEpisode.branchUpperTo) &&
            visible.AllVisible.Contains(hubEpisode.branchUpperTo))
        {
            branches.Add(hubEpisode.branchUpperTo);
        }

        if (!string.IsNullOrEmpty(hubEpisode.branchMiddleTo) &&
            visible.AllVisible.Contains(hubEpisode.branchMiddleTo))
        {
            branches.Add(hubEpisode.branchMiddleTo);
        }

        if (!string.IsNullOrEmpty(hubEpisode.branchLowerTo) &&
            visible.AllVisible.Contains(hubEpisode.branchLowerTo))
        {
            branches.Add(hubEpisode.branchLowerTo);
        }

        if (branches.Count == 0)
            return;

        float yStep = branches.Count >= 3
            ? BranchOffsetY
            : BranchOffsetY * 0.45f;

        if (!string.IsNullOrEmpty(hubEpisode.branchUpperTo) &&
            visible.AllVisible.Contains(hubEpisode.branchUpperTo))
        {
            float y = branches.Count == 1 ? 0f : yStep;
            positions.Set(hubEpisode.branchUpperTo, hubPos + new Vector2(MainStepX, y));
        }

        if (!string.IsNullOrEmpty(hubEpisode.branchMiddleTo) &&
            visible.AllVisible.Contains(hubEpisode.branchMiddleTo))
        {
            positions.Set(hubEpisode.branchMiddleTo, hubPos + new Vector2(MainStepX, 0f));
        }

        if (!string.IsNullOrEmpty(hubEpisode.branchLowerTo) &&
            visible.AllVisible.Contains(hubEpisode.branchLowerTo))
        {
            float y = branches.Count == 1 ? 0f : -yStep;
            positions.Set(hubEpisode.branchLowerTo, hubPos + new Vector2(MainStepX, y));
        }
    }

    private static void PositionBranchChain(
        string branchStartId,
        PositionMap positions,
        VisibleSet visible,
        IEpisodePlayLookup lookup)
    {
        if (!positions.TryGet(branchStartId, out Vector2 currentPos))
            return;

        string current = branchStartId;

        for (int guard = 0; guard < 64; guard++)
        {
            if (!lookup.TryGetEpisode(current, out EpisodeSpec episode) || episode == null)
                break;

            if (string.IsNullOrEmpty(episode.next))
                break;

            string nextId = episode.next;

            if (!visible.AllVisible.Contains(nextId))
                break;

            if (!visible.Types.TryGetValue(nextId, out EpisodeNodeKind nextType))
                break;

            if (nextType != EpisodeNodeKind.BranchChain)
                break;

            Vector2 nextPos = currentPos + new Vector2(MainStepX, 0f);
            positions.Set(nextId, nextPos);

            current = nextId;
            currentPos = nextPos;
        }
    }

    private static string FindEndingOwner(
        string endingId,
        VisibleSet visible,
        IEpisodePlayLookup lookup)
    {
        foreach (string id in visible.AllVisible)
        {
            if (!lookup.TryGetEpisode(id, out EpisodeSpec episode) || episode == null)
                continue;

            if (episode.next == endingId)
                return id;
        }

        return "";
    }

    private static List<EpisodeNodeModel> MaterializeNodes(
        VisibleSet visible,
        PositionMap positions,
        IEpisodeProgress progress,
        IEpisodePlayLookup lookup,
        string selectedEpisodeId)
    {
        List<EpisodeNodeModel> nodes = new List<EpisodeNodeModel>(visible.AllVisible.Count);

        string effectiveSelected = ResolveSelectedEpisodeId(
            visible.MainIds,
            selectedEpisodeId,
            progress
        );

        string currentEpisodeId = ResolveCurrentEpisodeId(visible.MainIds, progress);

        foreach (string id in visible.AllVisible)
        {
            if (!lookup.TryGetEpisode(id, out EpisodeSpec episode) || episode == null)
                continue;

            Vector2 pos = positions.Get(id);

            bool completed = progress != null && progress.IsEpisodeCompleted(id);
            bool selected = !string.IsNullOrEmpty(effectiveSelected) && id == effectiveSelected;
            bool isCurrent = !string.IsNullOrEmpty(currentEpisodeId) && id == currentEpisodeId;

            string indexText = BuildIndexTextFromIdOrOrder(episode);
            string title = ResolveEpisodeTitle(episode);

            visible.Types.TryGetValue(id, out EpisodeNodeKind kind);

            EpisodeAttachmentModel? lower = null;

            if (kind == EpisodeNodeKind.Main)
                lower = BuildAttachmentUnlockedOnly(lookup, progress, episode.attachmentLowerTo);

            nodes.Add(new EpisodeNodeModel(
                episodeId: id,
                kind: kind,
                indexText: indexText,
                title: title,
                anchoredPos: pos,
                locked: false,
                interactable: true,
                selected: selected,
                isCurrent: isCurrent,
                completed: completed,
                upperAttachment: null,
                lowerAttachment: lower
            ));
        }

        return nodes;
    }

    private static bool IsHub(EpisodeSpec episode)
    {
        if (episode == null)
            return false;

        bool hasBranch =
            !string.IsNullOrEmpty(episode.branchUpperTo) ||
            !string.IsNullOrEmpty(episode.branchMiddleTo) ||
            !string.IsNullOrEmpty(episode.branchLowerTo);

        return hasBranch && string.IsNullOrEmpty(episode.next);
    }

    private static List<string> BuildMainLineIds(
        ChapterSpec chapter,
        IEpisodePlayLookup lookup)
    {
        if (chapter == null || chapter.episodes == null || chapter.episodes.Length == 0)
            return new List<string>(0);

        EpisodeSpec[] episodes = (EpisodeSpec[])chapter.episodes.Clone();

        Array.Sort(
            episodes,
            (a, b) => (a?.order ?? 0).CompareTo(b?.order ?? 0))
        ;

        string startId = "";

        for (int i = 0; i < episodes.Length; i++)
        {
            EpisodeSpec episode = episodes[i];

            if (episode == null)
                continue;

            if (string.IsNullOrEmpty(episode.episodeId))
                continue;

            if (lookup.TryGetEpisode(episode.episodeId, out _))
            {
                startId = episode.episodeId;
                break;
            }
        }

        if (string.IsNullOrEmpty(startId))
            return new List<string>(0);

        List<string> result = new List<string>(episodes.Length);
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);

        string current = startId;

        for (int guard = 0; guard < 64; guard++)
        {
            if (string.IsNullOrEmpty(current))
                break;

            if (!visited.Add(current))
                break;

            if (!lookup.TryGetEpisode(current, out EpisodeSpec episode) || episode == null)
                break;

            if (episode.isEnding)
                break;

            result.Add(current);

            if (!string.IsNullOrEmpty(episode.next))
            {
                current = episode.next;
                continue;
            }

            string merge = FindMergeFromBranches(
                lookup,
                episode.branchUpperTo,
                episode.branchLowerTo,
                episode.branchMiddleTo
            );

            if (!string.IsNullOrEmpty(merge) && merge != current)
            {
                current = merge;
                continue;
            }

            break;
        }

        return result;
    }

    private static string FindMergeFromBranches(
        IEpisodePlayLookup lookup,
        string branchUpper,
        string branchLower,
        string branchMiddle)
    {
        List<string> starts = new List<string>(3);

        if (!string.IsNullOrEmpty(branchUpper))
            starts.Add(branchUpper);

        if (!string.IsNullOrEmpty(branchLower))
            starts.Add(branchLower);

        if (!string.IsNullOrEmpty(branchMiddle))
            starts.Add(branchMiddle);

        if (starts.Count < 2)
            return "";

        HashSet<string> intersection = null;

        for (int i = 0; i < starts.Count; i++)
        {
            HashSet<string> chain = CollectNextChainSet(lookup, starts[i], 64);

            if (chain.Count == 0)
                continue;

            if (intersection == null)
            {
                intersection = chain;
            }
            else
            {
                intersection.IntersectWith(chain);

                if (intersection.Count == 0)
                    return "";
            }
        }

        if (intersection == null || intersection.Count == 0)
            return "";

        string bestId = "";
        int bestOrder = int.MaxValue;

        foreach (string id in intersection)
        {
            if (!lookup.TryGetEpisode(id, out EpisodeSpec episode) || episode == null)
                continue;

            if (episode.order < bestOrder)
            {
                bestOrder = episode.order;
                bestId = id;
            }
        }

        return bestId;
    }

    private static HashSet<string> CollectNextChainSet(
        IEpisodePlayLookup lookup,
        string startId,
        int guardMax)
    {
        HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(startId))
            return set;

        string current = startId;

        for (int guard = 0; guard < guardMax; guard++)
        {
            if (string.IsNullOrEmpty(current))
                break;

            if (!set.Add(current))
                break;

            if (!lookup.TryGetEpisode(current, out EpisodeSpec episode) || episode == null)
                break;

            if (episode.isEnding)
                break;

            if (string.IsNullOrEmpty(episode.next))
                break;

            current = episode.next;
        }

        return set;
    }

    private static int EstimateHubBranchColumns(
        IEpisodePlayLookup lookup,
        EpisodeSpec hubEpisode)
    {
        if (lookup == null || hubEpisode == null)
            return 1;

        List<string> starts = new List<string>(3);

        if (!string.IsNullOrEmpty(hubEpisode.branchUpperTo))
            starts.Add(hubEpisode.branchUpperTo);

        if (!string.IsNullOrEmpty(hubEpisode.branchMiddleTo))
            starts.Add(hubEpisode.branchMiddleTo);

        if (!string.IsNullOrEmpty(hubEpisode.branchLowerTo))
            starts.Add(hubEpisode.branchLowerTo);

        if (starts.Count == 0)
            return 1;

        string merge = FindMergeFromBranches(
            lookup,
            hubEpisode.branchUpperTo,
            hubEpisode.branchLowerTo,
            hubEpisode.branchMiddleTo
        );

        int best = 1;

        for (int i = 0; i < starts.Count; i++)
        {
            int depth = CountDepthUntilMergeExclusive(
                lookup,
                starts[i],
                merge,
                64
            );

            if (depth > best)
                best = depth;
        }

        return best;
    }

    private static int CountDepthUntilMergeExclusive(
        IEpisodePlayLookup lookup,
        string startId,
        string mergeId,
        int guardMax)
    {
        if (lookup == null || string.IsNullOrEmpty(startId))
            return 1;

        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);

        string current = startId;
        int depth = 0;

        for (int guard = 0; guard < guardMax; guard++)
        {
            if (string.IsNullOrEmpty(current))
                break;

            if (!visited.Add(current))
                break;

            if (!string.IsNullOrEmpty(mergeId) && current == mergeId)
                break;

            if (!lookup.TryGetEpisode(current, out EpisodeSpec episode) || episode == null)
                break;

            if (episode.isEnding)
                break;

            depth++;

            if (string.IsNullOrEmpty(episode.next))
                break;

            if (!string.IsNullOrEmpty(mergeId) && episode.next == mergeId)
                break;

            current = episode.next;
        }

        return depth < 1 ? 1 : depth;
    }

    private static EpisodeAttachmentModel? BuildAttachmentUnlockedOnly(
        IEpisodePlayLookup lookup,
        IEpisodeProgress progress,
        string targetEpisodeId)
    {
        if (lookup == null || progress == null)
            return null;

        if (string.IsNullOrEmpty(targetEpisodeId))
            return null;

        if (!progress.IsEpisodeUnlocked(targetEpisodeId))
            return null;

        if (!lookup.TryGetEpisode(targetEpisodeId, out EpisodeSpec target) || target == null)
            return null;

        return new EpisodeAttachmentModel(
            hostEpisodeId: target.episodeId,
            displayTitle: ResolveEpisodeTitle(target),
            isInteractable: true
        );
    }

    private static string ResolveEpisodeTitle(EpisodeSpec episode)
    {
        if (episode == null)
            return "";

        if (episode.isEnding && !string.IsNullOrEmpty(episode.endingTitle))
            return episode.endingTitle;

        if (!string.IsNullOrEmpty(episode.displayName))
            return episode.displayName;

        return episode.episodeId;
    }

    private static string ResolveSelectedEpisodeId(
        List<string> mainIds,
        string requestedSelectedEpisodeId,
        IEpisodeProgress progress)
    {
        if (!string.IsNullOrEmpty(requestedSelectedEpisodeId) &&
            Contains(mainIds, requestedSelectedEpisodeId))
        {
            return requestedSelectedEpisodeId;
        }

        string current = ResolveCurrentEpisodeId(mainIds, progress);
        if (!string.IsNullOrEmpty(current))
            return current;

        if (progress != null)
        {
            for (int i = 0; i < mainIds.Count; i++)
            {
                string id = mainIds[i];

                if (progress.IsEpisodeUnlocked(id))
                    return id;
            }
        }

        return mainIds.Count > 0 ? mainIds[0] : "";
    }

    private static string ResolveCurrentEpisodeId(
        List<string> mainIds,
        IEpisodeProgress progress)
    {
        if (progress == null)
            return "";

        for (int i = 0; i < mainIds.Count; i++)
        {
            string id = mainIds[i];

            if (progress.IsEpisodeUnlocked(id) &&
                !progress.IsEpisodeCompleted(id))
            {
                return id;
            }
        }

        return "";
    }

    private static bool Contains(List<string> list, string id)
    {
        if (list == null)
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == id)
                return true;
        }

        return false;
    }

    private static ChapterMetaModel BuildChapterMeta(
        int chapterId,
        IEpisodePlayLookup lookup)
    {
        string chapterTitle = $"챕터 {chapterId}";
        string eraText = "성력 996년";

        if (lookup != null &&
            lookup.TryGetChapter(chapterId, out ChapterSpec chapter) &&
            chapter != null)
        {
            if (!string.IsNullOrEmpty(chapter.displayName))
                chapterTitle = chapter.displayName;

            if (!string.IsNullOrEmpty(chapter.eraText))
                eraText = chapter.eraText;
        }

        return new ChapterMetaModel(
            chapterIndex: $"챕터 {chapterId}",
            eraText: eraText,
            chapterTitle: chapterTitle
        );
    }

    private static string BuildIndexTextFromIdOrOrder(EpisodeSpec episode)
    {
        if (episode == null)
            return "??";

        string fromId = ExtractEpisodeIndexFromId(episode.episodeId);
        if (!string.IsNullOrEmpty(fromId))
            return fromId;

        return episode.order.ToString("00");
    }

    private static string ExtractEpisodeIndexFromId(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return "";

        int epPos = -1;

        for (int i = 0; i < episodeId.Length - 1; i++)
        {
            if (episodeId[i] == 'E' && episodeId[i + 1] == 'p')
            {
                epPos = i + 2;
                break;
            }
        }

        if (epPos < 0 || epPos >= episodeId.Length)
            return "";

        int start = epPos;
        int digitEnd = start;

        while (digitEnd < episodeId.Length)
        {
            char c = episodeId[digitEnd];

            if (c < '0' || c > '9')
                break;

            digitEnd++;
        }

        if (digitEnd == start)
            return "";

        int end = digitEnd;

        if (end < episodeId.Length)
        {
            char c = episodeId[end];

            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                end++;
        }

        return episodeId.Substring(start, end - start);
    }
}