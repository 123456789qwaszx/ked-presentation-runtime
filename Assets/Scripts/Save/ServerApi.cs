using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

// 서버 호출의 전부 (M7). UnityWebRequest를 Task로 감싼 얇은 층 — 재시도·큐 판단은 부르는 쪽의 정책이다.
// 던지지 않고 ApiResult로 돌아온다. 오프라인이 정상 경로인 앱이라 네트워크 실패는 예외가 아니다.
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

    public Task<ApiResult<PlaythroughCreatedDto>> CreatePlaythroughAsync(long userId, string token) =>
        SendAsync<PlaythroughCreatedDto>("POST", $"/users/{userId}/playthroughs", null, token);

    // 공개 GET — /content GET은 토큰이 필요 없다.
    public Task<ApiResult<List<ChapterVersionInfoDto>>> GetChapterVersionsAsync(string chapterId) =>
        SendAsync<List<ChapterVersionInfoDto>>("GET", $"/content/chapters/{chapterId}/versions", null, token: null);

    public Task<ApiResult<SaveUploadResponseDto>> PutSaveAsync(
        long playthroughId, int slotNo, SaveUploadRequestDto request, string token) =>
        SendAsync<SaveUploadResponseDto>("PUT", $"/playthroughs/{playthroughId}/saves/{slotNo}", request, token);

    private static Dictionary<string, string> Credentials(string username, string password) =>
        new Dictionary<string, string> { ["username"] = username, ["password"] = password };

    private async Task<ApiResult<T>> SendAsync<T>(string method, string path, object body, string token)
    {
        using (var request = new UnityWebRequest(_baseUrl + path, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 10;

            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(SaveJson.Serialize(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (token != null)
                request.SetRequestHeader("Authorization", "Bearer " + token);

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

            // 4xx/5xx — 공통 계약(D-004)의 code. 서버 형식이 아닌 본문(프록시 HTML 등)은 null.
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

    // 콜백 → Task. 완료 후 등록해도 곧장 불리는 것이 AsyncOperation의 계약이라 경쟁이 없다.
    private static Task AwaitOperation(UnityWebRequestAsyncOperation operation)
    {
        var completion = new TaskCompletionSource<bool>();

        operation.completed += _ => completion.TrySetResult(true);

        return completion.Task;
    }
}

// 한 호출의 결과. 셋 중 하나 — 못 닿음(NetworkError) / 2xx(Ok) / 4xx·5xx(ErrorCode).
public sealed class ApiResult<T>
{
    public bool NetworkError { get; private set; }
    public long Status { get; private set; }
    public T Body { get; private set; }
    public string ErrorCode { get; private set; }
    public string RawBody { get; private set; }

    public bool Ok => !NetworkError && Status >= 200 && Status < 300;

    public static ApiResult<T> Network(string message) =>
        new ApiResult<T> { NetworkError = true, RawBody = message };

    public static ApiResult<T> Success(long status, T body, string raw) =>
        new ApiResult<T> { Status = status, Body = body, RawBody = raw };

    public static ApiResult<T> Failure(long status, string code, string raw) =>
        new ApiResult<T> { Status = status, ErrorCode = code, RawBody = raw };
}
