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

            if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return AnalyticsApiResult<List<AnalyticsChapterSummaryDto>>
                    .Network(request.error);
            }

            long status = request.responseCode;
            string raw = request.downloadHandler.text;

            if (status >= 200 && status < 300)
            {
                try
                {
                    List<AnalyticsChapterSummaryDto> body =
                        JsonConvert.DeserializeObject<List<AnalyticsChapterSummaryDto>>(raw);

                    return AnalyticsApiResult<List<AnalyticsChapterSummaryDto>>
                        .Success(status, body ?? new List<AnalyticsChapterSummaryDto>(), raw);
                }
                catch (JsonException error)
                {
                    return AnalyticsApiResult<List<AnalyticsChapterSummaryDto>>
                        .Failure(status, "INVALID_RESPONSE", error.Message, raw);
                }
            }

            AnalyticsErrorResponseDto errorResponse = TryReadError(raw);

            return AnalyticsApiResult<List<AnalyticsChapterSummaryDto>>.Failure(
                status,
                errorResponse?.ErrorCode,
                errorResponse?.Message,
                raw);
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

        using (var request = new UnityWebRequest(
                   _baseUrl + "/playthroughs",
                   UnityWebRequest.kHttpVerbPOST))
        {
            request.timeout = 10;
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(rawRequest));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await AwaitOperation(request.SendWebRequest());

            if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return AnalyticsApiResult<AnalyticsPlaythroughDto>
                    .Network(request.error);
            }

            long status = request.responseCode;
            string raw = request.downloadHandler.text;

            if (status >= 200 && status < 300)
            {
                try
                {
                    AnalyticsPlaythroughDto body =
                        JsonConvert.DeserializeObject<AnalyticsPlaythroughDto>(raw);

                    if (body == null)
                    {
                        return AnalyticsApiResult<AnalyticsPlaythroughDto>
                            .Failure(status, "INVALID_RESPONSE", "회차 응답 본문이 비어 있다.", raw);
                    }

                    return AnalyticsApiResult<AnalyticsPlaythroughDto>
                        .Success(status, body, raw);
                }
                catch (JsonException error)
                {
                    return AnalyticsApiResult<AnalyticsPlaythroughDto>
                        .Failure(status, "INVALID_RESPONSE", error.Message, raw);
                }
            }

            AnalyticsErrorResponseDto errorResponse = TryReadError(raw);

            return AnalyticsApiResult<AnalyticsPlaythroughDto>.Failure(
                status,
                errorResponse?.ErrorCode,
                errorResponse?.Message,
                raw);
        }
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
