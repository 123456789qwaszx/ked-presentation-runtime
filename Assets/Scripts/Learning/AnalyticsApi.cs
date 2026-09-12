using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

// vn-play-analytics 학습 서버의 작은 HTTP 계약.
// 기존 ServerApi의 인증·저장·복구 계약과 섞지 않는다.
public sealed class AnalyticsApi
{
    private readonly string _baseUrl;

    public AnalyticsApi(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Analytics 서버 주소가 비어 있다.", nameof(baseUrl));

        _baseUrl = baseUrl.Trim().TrimEnd('/');
    }

    public async Task<AnalyticsApiResult<List<AnalyticsChapterSummaryDto>>> FindChaptersAsync(
        string chapterKey)
    {
        string path = "/chapters?chapterKey=" + Uri.EscapeDataString(chapterKey);

        using (var request = UnityWebRequest.Get(_baseUrl + path))
        {
            request.timeout = 10;
            await AwaitOperation(request.SendWebRequest());
            return ReadResponse<List<AnalyticsChapterSummaryDto>>(request, emptyListOnNull: true);
        }
    }

    public async Task<AnalyticsApiResult<AnalyticsPlaythroughDto>> CreateOrGetPlaythroughAsync(
        string chapterKey,
        string clientPlaythroughId)
    {
        string rawRequest = JsonConvert.SerializeObject(new
        {
            chapterKey,
            clientPlaythroughId,
        });

        using (var request = CreateJsonRequest(
                   _baseUrl + "/playthroughs",
                   UnityWebRequest.kHttpVerbPOST,
                   rawRequest))
        {
            await AwaitOperation(request.SendWebRequest());
            return ReadResponse<AnalyticsPlaythroughDto>(request);
        }
    }

    public async Task<AnalyticsApiResult<AnalyticsCheckpointDto>> SaveCheckpointAsync(
        long playthroughId,
        string episodeKey,
        bool chapterCompleted,
        string snapshotJson)
    {
        string rawRequest = JsonConvert.SerializeObject(new
        {
            episodeKey,
            chapterCompleted,
            snapshotJson,
        });

        string url = _baseUrl + $"/playthroughs/{playthroughId}/checkpoint";

        using (var request = CreateJsonRequest(
                   url,
                   UnityWebRequest.kHttpVerbPUT,
                   rawRequest))
        {
            await AwaitOperation(request.SendWebRequest());
            return ReadResponse<AnalyticsCheckpointDto>(request);
        }
    }

    public async Task<AnalyticsApiResult<AnalyticsCheckpointDto>> GetCheckpointAsync(
        long playthroughId)
    {
        string url = _baseUrl + $"/playthroughs/{playthroughId}/checkpoint";

        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            await AwaitOperation(request.SendWebRequest());

            if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return AnalyticsApiResult<AnalyticsCheckpointDto>
                    .Network(request.error);
            }

            // 회차는 있지만 아직 서버 checkpoint가 없는 정상 상태.
            if (request.responseCode == 204)
            {
                return AnalyticsApiResult<AnalyticsCheckpointDto>
                    .Success(204, null, request.downloadHandler.text);
            }

            return ReadResponse<AnalyticsCheckpointDto>(request);
        }
    }

    public async Task<AnalyticsApiResult<bool>> ReplaceChoicesAsync(
        long playthroughId,
        IReadOnlyList<AnalyticsChoiceItemDto> choices)
    {
        string rawRequest = JsonConvert.SerializeObject(new
        {
            choices,
        });

        string url = _baseUrl + $"/playthroughs/{playthroughId}/choices";

        using (var request = CreateJsonRequest(
                   url,
                   UnityWebRequest.kHttpVerbPUT,
                   rawRequest))
        {
            await AwaitOperation(request.SendWebRequest());
            return ReadNoContentResponse(request);
        }
    }

    private static UnityWebRequest CreateJsonRequest(
        string url,
        string method,
        string rawRequest)
    {
        var request = new UnityWebRequest(url, method)
        {
            timeout = 10,
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(rawRequest)),
            downloadHandler = new DownloadHandlerBuffer(),
        };

        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private static AnalyticsApiResult<bool> ReadNoContentResponse(
        UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            return AnalyticsApiResult<bool>.Network(request.error);
        }

        long status = request.responseCode;
        string raw = request.downloadHandler.text;

        if (status >= 200 && status < 300)
            return AnalyticsApiResult<bool>.Success(status, true, raw);

        AnalyticsErrorResponseDto errorResponse = TryReadError(raw);

        return AnalyticsApiResult<bool>.Failure(
            status,
            errorResponse?.ErrorCode,
            errorResponse?.Message,
            raw);
    }

    private static AnalyticsApiResult<T> ReadResponse<T>(
        UnityWebRequest request,
        bool emptyListOnNull = false)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            return AnalyticsApiResult<T>.Network(request.error);
        }

        long status = request.responseCode;
        string raw = request.downloadHandler.text;

        if (status >= 200 && status < 300)
        {
            try
            {
                T body = JsonConvert.DeserializeObject<T>(raw);

                if (body == null)
                {
                    if (emptyListOnNull && typeof(T) == typeof(List<AnalyticsChapterSummaryDto>))
                    {
                        object empty = new List<AnalyticsChapterSummaryDto>();
                        return AnalyticsApiResult<T>.Success(status, (T)empty, raw);
                    }

                    return AnalyticsApiResult<T>
                        .Failure(status, "INVALID_RESPONSE", "응답 본문이 비어 있다.", raw);
                }

                return AnalyticsApiResult<T>.Success(status, body, raw);
            }
            catch (JsonException error)
            {
                return AnalyticsApiResult<T>
                    .Failure(status, "INVALID_RESPONSE", error.Message, raw);
            }
        }

        AnalyticsErrorResponseDto errorResponse = TryReadError(raw);

        return AnalyticsApiResult<T>.Failure(
            status,
            errorResponse?.ErrorCode,
            errorResponse?.Message,
            raw);
    }

    private static AnalyticsErrorResponseDto TryReadError(string raw)
    {
        try
        {
            return JsonConvert.DeserializeObject<AnalyticsErrorResponseDto>(raw);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Task AwaitOperation(UnityWebRequestAsyncOperation operation)
    {
        if (operation.isDone)
            return Task.CompletedTask;

        var completion = new TaskCompletionSource<bool>();
        operation.completed += _ => completion.TrySetResult(true);
        return completion.Task;
    }
}

public sealed class AnalyticsChapterSummaryDto
{
    public long ChapterId { get; set; }
    public string ChapterKey { get; set; }
    public string Title { get; set; }
}

public sealed class AnalyticsPlaythroughDto
{
    public long PlaythroughId { get; set; }
    public string ClientPlaythroughId { get; set; }
}

public sealed class AnalyticsCheckpointDto
{
    public long CheckpointId { get; set; }
    public long PlaythroughId { get; set; }
    public string EpisodeKey { get; set; }
    public bool ChapterCompleted { get; set; }
    public string SnapshotJson { get; set; }
    public string SavedAt { get; set; }
}

public sealed class AnalyticsChoiceItemDto
{
    [JsonProperty("episodeKey")]
    public string EpisodeKey { get; set; }

    [JsonProperty("optionIndex")]
    public int OptionIndex { get; set; }
}

public sealed class AnalyticsErrorResponseDto
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
}

public sealed class AnalyticsApiResult<T>
{
    public bool IsSuccess { get; }
    public bool NetworkError { get; }
    public long Status { get; }
    public T Body { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public string RawBody { get; }

    private AnalyticsApiResult(
        bool isSuccess,
        bool networkError,
        long status,
        T body,
        string errorCode,
        string errorMessage,
        string rawBody)
    {
        IsSuccess = isSuccess;
        NetworkError = networkError;
        Status = status;
        Body = body;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RawBody = rawBody;
    }

    public static AnalyticsApiResult<T> Success(long status, T body, string rawBody) =>
        new(true, false, status, body, null, null, rawBody);

    public static AnalyticsApiResult<T> Network(string message) =>
        new(false, true, 0, default, null, message, null);

    public static AnalyticsApiResult<T> Failure(
        long status,
        string errorCode,
        string errorMessage,
        string rawBody) =>
        new(false, false, status, default, errorCode, errorMessage, rawBody);
}
