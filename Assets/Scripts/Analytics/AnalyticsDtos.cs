using Newtonsoft.Json;

public sealed class AnalyticsPlaythroughDto
{
    public long PlaythroughId { get; set; }
    public string ClientPlaythroughId { get; set; }
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
