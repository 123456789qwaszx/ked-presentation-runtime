using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// 게스트 계정과 토큰 (D-016) — account.json.
//
// 문은 EnsureTokenAsync 하나: 유효한 토큰 → 그대로 / 없거나 만료 임박 → 로그인 /
// 계정 없음 → 가입부터. 서버가 안 닿으면 null — 이번 동기화만 접는다.
public sealed class GuestSession
{
    // 만료(24h) 전에 미리 갈아탄다 — 요청 도중 만료를 넘는 경계를 피한다.
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    private readonly ServerApi _api;
    private readonly string _accountPath;
    private AccountFile _account;

    public GuestSession(ServerApi api, string accountPath)
    {
        _api = api;
        _accountPath = accountPath;

        string json = AtomicFile.ReadAllTextOrNull(accountPath);
        _account = json == null ? null : SaveJson.Deserialize<AccountFile>(json);
    }

    public long? UserId => _account?.UserId;

    // 401을 받은 호출자가 부른다 — 서버 재시작으로 sessions가 비면 expiresAt이 남았어도 토큰은 죽는다.
    public void InvalidateToken()
    {
        _account.Token = null;
        Persist();
    }

    public async Task<string> EnsureTokenAsync()
    {
        if (HasUsableToken())
            return _account.Token;

        if (_account == null && !await SignUpAsync())
            return null;

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
        // 이 두 값이 계정의 전부다 — account.json을 잃으면 계정도 잃는다(게스트의 계약).
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
            // 401 = 서버에서 계정이 사라졌다(DB 재생성 등). 버리면 다음 동기화가 새 게스트로 선다.
            if (result.Status == 401)
            {
                Debug.LogWarning($"[계정] '{_account.Username}' 로그인 거부 — 계정을 버린다.");
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
