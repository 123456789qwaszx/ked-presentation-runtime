using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

// "내가 재생 중인 에셋은 서버의 몇 버전인가" (D-015).
//
// 에셋 바이트의 SHA-256을 GET /content/chapters/{id}/versions의 checksum과 대조한다.
// 업로드 원본과 같은 파일이므로 일치하고, 어긋나 있으면 일치 실패로 드러난다 — 아무 버전이나 고르지 않는다.
public sealed class ChapterVersionResolver
{
    private readonly ServerApi _api;
    private readonly string _checksum;

    // 에셋은 빌드에 박혀 안 바뀐다 — 한 번 찾은 버전은 앱 수명 동안 유효하다.
    private readonly Dictionary<string, int> _resolved = new Dictionary<string, int>(StringComparer.Ordinal);

    public ChapterVersionResolver(ServerApi api, TextAsset chapterAsset)
    {
        _api = api;

        using (var sha = SHA256.Create())
            _checksum = BitConverter.ToString(sha.ComputeHash(chapterAsset.bytes)).Replace("-", "").ToLowerInvariant();
    }

    // 서버의 버전 번호. null이면 닿지 않았거나 일치하는 버전이 없다 — 이번 동기화를 접는다.
    public async Task<int?> ResolveAsync(string chapterId)
    {
        if (_resolved.TryGetValue(chapterId, out int cached))
            return cached;

        ApiResult<List<ChapterVersionInfoDto>> result = await _api.GetChapterVersionsAsync(chapterId);

        if (!result.Ok)
        {
            if (!result.NetworkError)
                Debug.LogWarning($"[동기화] '{chapterId}' 버전 목록 조회 실패 — HTTP {result.Status} {result.ErrorCode}");

            return null;
        }

        foreach (ChapterVersionInfoDto info in result.Body)
        {
            if (string.Equals(info.Checksum, _checksum, StringComparison.OrdinalIgnoreCase))
            {
                _resolved[chapterId] = info.Version;

                Debug.Log($"[동기화] '{chapterId}' = 서버 v{info.Version} (checksum 일치)");

                return info.Version;
            }
        }

        Debug.LogWarning(
            $"[동기화] '{chapterId}' 에셋과 일치하는 서버 버전이 없다 — 이 에셋을 서버에 수입해야 동기화가 시작된다 (D-015).");

        return null;
    }
}
