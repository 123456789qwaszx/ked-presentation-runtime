using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace Ked.Save
{
    // "내가 재생 중인 에셋은 서버의 몇 버전인가" (M7-0, D-015).
    //
    // 클라의 콘텐츠는 에셋 참조(TextAsset)로 실려 있고 버전 번호는 서버가 수입 시 붙인다 —
    // 클라는 자기 버전을 모른다. 그래서 **에셋 바이트의 SHA-256**을 계산해
    // GET /content/chapters/{id}/versions 의 checksum과 대조한다. 업로드 원본과 같은
    // 파일이므로 반드시 일치하고(서버 common/Checksum과 같은 알고리즘), 에셋과 서버가
    // 어긋나 있으면 조용히 틀리는 대신 **일치 실패로 드러난다** — 그게 이 방식을 고른 이유다.
    public sealed class ChapterVersionResolver
    {
        private readonly ServerApi _api;
        private readonly string _checksum;

        // 성공한 해석은 앱 수명 동안 유효하다 — 에셋은 빌드에 박혀 안 바뀐다.
        private readonly Dictionary<string, int> _resolved =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public ChapterVersionResolver(ServerApi api, TextAsset chapterAsset)
        {
            _api = api;

            // TextAsset.bytes = 임포트된 원본 파일 바이트 — 서버에 올린 그 바이트다.
            // 조립 시점(메인 스레드)에 한 번 계산해 두면 이후는 Unity API가 필요 없다.
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(chapterAsset.bytes);
                _checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        // 서버의 버전 번호, 또는 null(닿지 않음 / 일치하는 버전 없음).
        // null이면 호출자는 이번 동기화를 접는다 — 큐는 남고, 다음 기회에 다시 온다.
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

            // 에셋이 서버 어느 버전과도 다르다 — 수입을 안 했거나, 수입 후 에셋이 바뀌었다.
            // 여기서 멈추는 것이 설계다: 아무 버전이나 골라 보내면 세이브가 엉뚱한 콘텐츠에 묶인다.
            Debug.LogWarning(
                $"[동기화] '{chapterId}' 에셋(sha256 {_checksum.Substring(0, 12)}…)과 일치하는 서버 버전이 없다. " +
                "이 에셋을 서버에 수입해야 동기화가 시작된다 (D-015).");

            return null;
        }
    }
}
