using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Ked.Save
{
    // 서버 호출의 전부 (M7-6). UnityWebRequest를 Task로 감싼 얇은 층 —
    // 재시도·백오프·큐 판단은 여기 없다. 그건 부르는 쪽(ServerSyncSaveStore)의 정책이다.
    //
    // 모든 메서드는 던지지 않고 ApiResult로 돌아온다. 오프라인이 **정상 경로**인 앱이라
    // (비행기 모드가 완료 기준이다) 네트워크 실패를 예외로 다루면 호출부 전체가 try 그물이 된다.
    public sealed class ServerApi
    {
        private readonly string _baseUrl;

        public ServerApi(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public Task<ApiResult<UserResponseDto>> SignUpAsync(string username, string password) =>
            SendAsync<UserResponseDto>("POST", "/users",
                new Dictionary<string, string> { ["username"] = username, ["password"] = password },
                token: null);

        public Task<ApiResult<LoginResponseDto>> LoginAsync(string username, string password) =>
            SendAsync<LoginResponseDto>("POST", "/auth/login",
                new Dictionary<string, string> { ["username"] = username, ["password"] = password },
                token: null);

        public Task<ApiResult<PlaythroughCreatedDto>> CreatePlaythroughAsync(long userId, string token) =>
            SendAsync<PlaythroughCreatedDto>("POST", $"/users/{userId}/playthroughs", null, token);

        // 공개 GET (M6 — /content GET은 토큰이 필요 없다).
        public Task<ApiResult<List<ChapterVersionInfoDto>>> GetChapterVersionsAsync(string chapterId) =>
            SendAsync<List<ChapterVersionInfoDto>>(
                "GET", $"/content/chapters/{chapterId}/versions", null, token: null);

        public Task<ApiResult<SaveUploadResponseDto>> PutSaveAsync(
            long playthroughId, int slotNo, SaveUploadRequestDto request, string token) =>
            SendAsync<SaveUploadResponseDto>(
                "PUT", $"/playthroughs/{playthroughId}/saves/{slotNo}", request, token);

        private async Task<ApiResult<T>> SendAsync<T>(string method, string path, object body, string token)
        {
            using (var request = new UnityWebRequest(_baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();

                if (body != null)
                {
                    // 바이트를 직접 만든다 — UTF-8, 재인코딩 없음. 서버 검증에서 클라이언트측
                    // 재인코딩(PowerShell)이 두 번 사고를 냈다(F45). 여기는 그 여지가 없다.
                    byte[] payload = Encoding.UTF8.GetBytes(SaveJson.Serialize(body));
                    request.uploadHandler = new UploadHandlerRaw(payload);
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (token != null)
                    request.SetRequestHeader("Authorization", "Bearer " + token);

                request.timeout = 10;

                await AwaitOperation(request.SendWebRequest());

                // 연결 자체가 안 됐다 — 상태 코드도 본문도 없다. "오프라인"의 모양.
                if (request.result == UnityWebRequest.Result.ConnectionError
                    || request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    return ApiResult<T>.Network(request.error);
                }

                long status = request.responseCode;
                string raw = request.downloadHandler.text;

                if (status >= 200 && status < 300)
                {
                    // 204(logout) 등 빈 본문이면 default(T) — 호출부는 Ok만 본다.
                    T parsed = string.IsNullOrEmpty(raw) ? default : SaveJson.Deserialize<T>(raw);
                    return ApiResult<T>.Success(status, parsed, raw);
                }

                // 4xx/5xx — 공통 계약(D-004)의 code를 꺼내 둔다. 못 꺼내면 null인 채로.
                string code = null;

                try
                {
                    ErrorResponseDto error = SaveJson.Deserialize<ErrorResponseDto>(raw);
                    code = error?.Code;
                }
                catch (Exception)
                {
                    // 프록시가 낸 HTML 등 서버 형식이 아닌 본문 — RawBody로만 남는다.
                }

                return ApiResult<T>.Failure(status, code, raw);
            }
        }

        // 콜백 → Task. 완료 후 등록해도 곧장 불리는 것이 UnityWebRequestAsyncOperation의
        // 계약이라 경쟁이 없다. ChapterOptionsView가 쓰는 것과 같은 TCS 패턴.
        private static Task AwaitOperation(UnityWebRequestAsyncOperation operation)
        {
            var completion = new TaskCompletionSource<bool>();

            operation.completed += _ => completion.TrySetResult(true);

            return completion.Task;
        }
    }

    // 한 호출의 결과. 셋 중 하나다: 못 닿음(NetworkError) / 2xx(Ok) / 4xx·5xx(ErrorCode).
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
}
