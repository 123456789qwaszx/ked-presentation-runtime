using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

// Unity 클라이언트에서 서버 HTTP API를 호출하는 공통 통신 계층
// - UnityWebRequest를 Task로 감싼 얇은 층 (재시도/큐 판단은 부르는 쪽의 정책)
// - 오프라인이 정상 경로인 앱이라 네트워크 실패는 예외가 아님.
public sealed class ServerApi
{
    private readonly string _baseUrl;

    public ServerApi(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public Task<ApiResult<UserResponseDto>> SignUpAsync(string username, string password) =>
        SendAsync<UserResponseDto>("POST", "/users", Credentials(username, password), token: null);

    public Task<ApiResult<LoginResponseDto>> LoginAsync(string username, string password) =>
        SendAsync<LoginResponseDto>("POST", "/auth/login", Credentials(username, password), token: null);

    public Task<ApiResult<PlaythroughCreatedDto>> CreatePlaythroughAsync(
        long userId, PlaythroughCreateRequestDto request, string token) =>
        SendAsync<PlaythroughCreatedDto>("POST", $"/users/{userId}/playthroughs", request, token);

    // 공개 GET - /content GET은 토큰이 필요 없음. (로그인하지 않은 클라이언트도 조회할 수 있는 공개 엔드포인트)
    public Task<ApiResult<List<ChapterVersionInfoDto>>> GetChapterVersionsAsync(string chapterId) =>
        SendAsync<List<ChapterVersionInfoDto>>("GET", $"/content/chapters/{chapterId}/versions", null, token: null);

    public Task<ApiResult<SaveUploadResponseDto>> PutSaveAsync(
        long playthroughId, int slotNo, SaveUploadRequestDto request, string token) =>
        SendAsync<SaveUploadResponseDto>("PUT", $"/playthroughs/{playthroughId}/saves/{slotNo}", request, token);

    // ── 복구용 GET ──

    public Task<ApiResult<List<PlaythroughSummaryDto>>> GetPlaythroughsAsync(long userId, string token) =>
        SendAsync<List<PlaythroughSummaryDto>>("GET", $"/users/{userId}/playthroughs", null, token);

    public Task<ApiResult<SaveSlotDetailDto>> GetSaveAsync(long playthroughId, int slotNo, string token) =>
        SendAsync<SaveSlotDetailDto>("GET", $"/playthroughs/{playthroughId}/saves/{slotNo}", null, token);

    public Task<ApiResult<List<ChoiceHistoryItemDto>>> GetChoicesAsync(long playthroughId, int slotNo, string token) =>
        SendAsync<List<ChoiceHistoryItemDto>>("GET", $"/playthroughs/{playthroughId}/saves/{slotNo}/choices", null, token);

    // ── 즐겨찾기 — revision 없음, 마지막 PUT이 이긴다 ──

    public Task<ApiResult<BookmarkUpsertResponseDto>> PutBookmarkAsync(
        long userId, string clientBookmarkId, BookmarkUpsertRequestDto request, string token) =>
        SendAsync<BookmarkUpsertResponseDto>("PUT", $"/users/{userId}/bookmarks/{clientBookmarkId}", request, token);

    // 204, 본문 없음. 없어도·이미 지웠어도 204.
    public Task<ApiResult<object>> DeleteBookmarkAsync(long userId, string clientBookmarkId, string token) =>
        SendAsync<object>("DELETE", $"/users/{userId}/bookmarks/{clientBookmarkId}", null, token);

    public Task<ApiResult<List<BookmarkDetailDto>>> GetBookmarksAsync(long userId, string token) =>
        SendAsync<List<BookmarkDetailDto>>("GET", $"/users/{userId}/bookmarks", null, token);

    public Task<ApiResult<BookmarkDetailDto>> GetBookmarkAsync(long userId, string clientBookmarkId, string token) =>
        SendAsync<BookmarkDetailDto>("GET", $"/users/{userId}/bookmarks/{clientBookmarkId}", null, token);

    private static Dictionary<string, string> Credentials(string username, string password) =>
        new Dictionary<string, string> { ["username"] = username, ["password"] = password };

    private async Task<ApiResult<T>> SendAsync<T>(string method, string path, object body, string token)
    {
        // 요청 수명은 이 호출 안. 성공-실패-예외와 관계없이 끝나면 Dispose.
        using (var request = new UnityWebRequest(_baseUrl + path, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 10;

            if (body != null)
            {
                // 요청 본문을 JSON 바이트로.
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(SaveJson.Serialize(body)));
                
                // 서버에게 "이 본문은 JSON이라고 알림.
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (token != null)
            {
                // 서버에게 "Bearer 방식의 인증 토큰이라고 알림.
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            await AwaitOperation(request.SendWebRequest());

            if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return ApiResult<T>.Network(request.error);
            }

            long status = request.responseCode;
            string raw = request.downloadHandler.text;

            if (status >= 200 && status < 300)
                return ApiResult<T>.Success(status, SaveJson.Deserialize<T>(raw), raw);

            // 우리 서버 형식이 아닌 본문(프록시 HTML 등)은 null로 둠.
            // 억지로 읽다가 안 터지도록.
            string code = null;

            try
            {
                code = SaveJson.Deserialize<ErrorResponseDto>(raw)?.Code;
            }
            catch (Newtonsoft.Json.JsonException)
            {
            }

            return ApiResult<T>.Failure(status, code, raw);
        }
    }

    // Bridges Unity's completion callback to Task. (so the operation can be awaited.)
    // AsyncOperation invokes completed even if the handler is registered after completion.
    private static Task AwaitOperation(UnityWebRequestAsyncOperation operation)
    {
        var completion = new TaskCompletionSource<bool>();

        operation.completed += _ => completion.TrySetResult(true);

        return completion.Task;
    }
}