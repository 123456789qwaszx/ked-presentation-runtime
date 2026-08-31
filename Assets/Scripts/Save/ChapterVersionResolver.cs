using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

// 지금 Unity에서 플레이 중인 챕터 파일의 버전을 서버에서 확인.
// 에셋 바이트의 SHA-256을 GET /content/chapters/{id}/versions의 checksum과 대조.
public sealed class ChapterVersionResolver
{
    private readonly ServerApi _api;
    private readonly string _checksum;

    // 한 번 찾은 버전은 앱 수명 동안만 유효.
    private readonly Dictionary<string, int> _resolved = new(StringComparer.Ordinal);

    public ChapterVersionResolver(ServerApi api, TextAsset chapterAsset)
    {
        _api = api;

        // Dispose 자동 호출하여 정리 해줌.
        using var sha = SHA256.Create();

        _checksum = BitConverter.ToString(sha.ComputeHash(chapterAsset.bytes))
            .Replace("-", "")
            .ToLowerInvariant();
    }

    // 서버의 버전 번호.
    // null이면 닿지 않았거나 일치하는 버전이 없음. 이번 동기화를 접는다.
    public async Task<int?> ResolveAsync(string chapterId)
    {
        if (_resolved.TryGetValue(chapterId, out int cached))
            return cached;

        // 성공 시 Chapter 버전 정보를 여러개 받음.
        ApiResult<List<ChapterVersionInfoDto>> result = await _api.GetChapterVersionsAsync(chapterId);

        if (!result.Ok)
        {
            // HTTP/API 오류 -> "버전 목록 조회 실패"
            if (!result.NetworkError)
                Debug.LogWarning($"[동기화] '{chapterId}' 버전 목록 조회 실패 — HTTP {result.Status} {result.ErrorCode}");

            // 네트워크 오류 -> 로그 없이 null
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
            $"[동기화] '{chapterId}' 로컬 컨텐츠를 서버에서 찾을 수 없음. 에셋을 서버에 수입시켜야 동기화 가능.");

        return null;
    }
}