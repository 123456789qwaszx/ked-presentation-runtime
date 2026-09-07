using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// 세션 관리자:
// - 게임 클라가 서버 동기화를 할 때 사용할 "게스트 계정 + 로그인 토큰"을 관리(account.json)
//
// EnsureTokenAsync:
// 유효한 토큰 -> 그대로 /
// 없거나 만료 임박 -> 로그인 /
// 계정 없음 -> 가입부터 /
// 서버가 안 닿으면 null - 이번 동기화만 포기
public sealed class GuestSession
{
    // 만료(24h) 전에 미리 갈아탄다 - 요청 도중 만료를 넘는 경계를 피하는 용도.
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    private readonly ServerApi _api;
    private readonly string _accountPath;
    private AccountFile _account;

    public GuestSession(ServerApi api, string accountPath)
    {
        _api = api;
        _accountPath = accountPath;

        string json = AtomicFile.ReadAllTextOrNull(accountPath);
        _account = json == null 
            ? null 
            : SaveJson.Deserialize<AccountFile>(json);
    }

    public long? UserId => _account?.UserId;

    // 401을 받고 호출.
    // - 서버 재시작으로 sessions가 비면 expiresAt이 남았어도 토큰을 비움.
    public void InvalidateToken()
    {
        _account.Token = null;
        Persist();
    }

    // 토큰을 붙여 한 번 부른다. 401이면(서버 재시작 등) 토큰을 버리고 새로 받아 한 번 더.
    // 토큰을 못 받으면 NetworkError 결과 — 부르는 쪽은 "닿지 않았다"로 본다.
    public async Task<ApiResult<T>> CallAsync<T>(Func<string, Task<ApiResult<T>>> call)
    {
        string token = await EnsureTokenAsync();

        if (token == null)
            return ApiResult<T>.Network("토큰 없음");

        ApiResult<T> result = await call(token);

        if (result.Status != 401)
            return result;

        InvalidateToken();
        token = await EnsureTokenAsync();

        return token == null ? result : await call(token);
    }

    // 토큰 확보는 단일 비행 — 동시에 몇이 부르든 가입·로그인은 한 번. 아니면 계정이 둘 생겨 뒤가 앞을 덮는다.
    private Task<string> _ensuring;

    public async Task<string> EnsureTokenAsync()
    {
        // 계정도 있고 토큰도 유효:
        // - 기존 토큰 반환.(서버요청 없음)
        if (HasUsableToken())
            return _account.Token;

        if (_ensuring == null)
            _ensuring = EnsureTokenCoreAsync();

        try
        {
            return await _ensuring;
        }
        finally
        {
            // 대기자 여럿이 각자 null로 두어도 무해 — 다음 호출은 HasUsableToken이 먼저 답한다.
            _ensuring = null;
        }
    }

    private async Task<string> EnsureTokenCoreAsync()
    {
        // account.json 자체가 없음(_account == null):
        // A-1) await SignUpAsync()실행. 성공 후 _account 생성.
        // A-2) return await LoginAsync()실행. 토큰 발급.
        //
        // B-1) await SignUpAsync()실패.
        // B-2) 진행도 동기화만 포기. 게임 진행 자체는 가능.
        if (_account == null && !await SignUpAsync()) 
            return null;

        // 계정은 있는데 토큰이 없음/만료:
        // - 재 로그인 및 새 토큰 발급.
        return await LoginAsync();
    }

    private bool HasUsableToken()
    {
        if (_account?.Token == null)
            return false;

        DateTimeOffset expiresAt = DateTimeOffset.Parse(
            _account.ExpiresAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        return DateTimeOffset.UtcNow + ExpiryMargin < expiresAt;
    }

    private async Task<bool> SignUpAsync()
    {
        // 이 두 값만으로 계정 표현. (게스트는 account.json을 잃으면 계정 상실. 찾을 방법 없음.)
        string username = "guest-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        string password = Guid.NewGuid().ToString("N");

        ApiResult<UserResponseDto> result = await _api.SignUpAsync(username, password);

        if (!result.Ok)
        {
            LogFailure("가입", result);
            return false;
        }

        _account = new AccountFile
        {
            Username = username,
            Password = password,
            UserId = result.Body.Id,
        };

        Persist();

        Debug.Log($"[계정] 게스트 계정 생성 — {username} (userId {result.Body.Id})");

        return true;
    }

    private async Task<string> LoginAsync()
    {
        ApiResult<LoginResponseDto> result = await _api.LoginAsync(_account.Username, _account.Password);

        if (!result.Ok)
        {
            // 401 = 서버에서 계정이 사라졌다(DB 재생성 등).
            // 클라는 계정이 있다고 알고있음에도 서버가 거부된 상황
            // e.g.) 개발 중 DB날려먹음. 테스트 DB 재생성. 서버 데이터 초기화 등
            // 따라서 account.json을 신뢰하지 않고 버림. 다음에 동기화 시 새 게스트.
            if (result.Status == 401)
            {
                Debug.LogWarning($"[계정] '{_account.Username}' 로그인 거부 - 계정을 버린다.");
                _account = null;
                File.Delete(_accountPath);
            }
            else
            {
                LogFailure("로그인", result);
            }

            return null;
        }

        _account.Token = result.Body.Token;
        _account.ExpiresAtUtc = result.Body.ExpiresAt;

        Persist();

        return _account.Token;
    }

    private void Persist() =>
        AtomicFile.WriteAllText(_accountPath, SaveJson.SerializePretty(_account));

    private static void LogFailure<T>(string what, ApiResult<T> result)
    {
        if (result.NetworkError)
            Debug.Log($"[계정] {what} — 서버에 닿지 않는다 (오프라인이면 정상).");
        else
            Debug.LogWarning($"[계정] {what} 실패 — HTTP {result.Status} {result.ErrorCode}");
    }
}