using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

// 선택 통계 전용 HTTP. 로컬 저장 및 복원은 이 API를 사용하지 않는다.
public sealed class AnalyticsApi : IAnalyticsTransport
{
    private readonly string _baseUrl;

    public AnalyticsApi(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Analytics 서버 주소가 비어 있다.", nameof(baseUrl));

        _baseUrl = baseUrl.Trim().TrimEnd('/');
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
        UnityWebRequest request)
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

