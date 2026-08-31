// 호출 결과.
// 못 닿음(NetworkError) / 2xx(Ok) / 4xx-5xx(ErrorCode)
public sealed class ApiResult<T>
{
    public bool NetworkError { get; private set; }
    public long Status { get; private set; }
    public T Body { get; private set; }
    public string ErrorCode { get; private set; }
    public string RawBody { get; private set; }

    public bool Ok => !NetworkError
                      && Status >= 200 
                      && Status < 300;

    public static ApiResult<T> Network(string message) =>
        new ApiResult<T> { NetworkError = true, RawBody = message };

    public static ApiResult<T> Success(long status, T body, string raw) =>
        new ApiResult<T> { Status = status, Body = body, RawBody = raw };

    public static ApiResult<T> Failure(long status, string code, string raw) =>
        new ApiResult<T> { Status = status, ErrorCode = code, RawBody = raw };
}