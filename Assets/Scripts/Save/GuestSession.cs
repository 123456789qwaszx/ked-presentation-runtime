using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace Ked.Save
{
    // 게스트 계정과 토큰 (M7, D-016) — account.json.
    //
    // 로그인 UI가 없는 지금, 신원은 "이 설치"다: 첫 동기화 때 guest-{12hex} 계정을
    // 서버에 만들어 파일로 보관하고, 이후로는 그 계정으로 로그인해 토큰을 유지한다.
    // 오프라인 첫 실행이어도 게임은 돈다 — 계정은 처음 서버가 닿는 순간 만들어진다.
    //
    // EnsureTokenAsync가 유일한 문이다: 유효한 토큰이 있으면 그것, 없으면 로그인,
    // 계정이 없으면 가입부터. 어디서든 서버가 안 닿으면 null — "지금은 못 보낸다"쪽으로.
    public sealed class GuestSession
    {
        // 만료(24h) 전에 미리 갈아탄다 — 요청이 나가는 도중 만료를 넘는 경계를 피한다.
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

        private readonly ServerApi _api;
        private readonly string _accountPath;
        private AccountFile _account;

        public GuestSession(ServerApi api, string accountPath)
        {
            _api = api;
            _accountPath = accountPath;
            _account = Read(accountPath);
        }

        private static AccountFile Read(string path)
        {
            string json = AtomicFile.ReadAllTextOrNull(path);

            if (json == null)
                return null;

            try
            {
                AccountFile account = SaveJson.Deserialize<AccountFile>(json);

                // 빈 껍데기(계정을 버린 흔적 — Persist 주석)도 "계정 없음"이다.
                return string.IsNullOrEmpty(account?.Username) ? null : account;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[계정] account.json 을 읽지 못했다 — 새 게스트로 취급.\n{error}");
                return null;
            }
        }

        public long? UserId => _account?.UserId;

        // 401을 받은 호출자가 부른다 — 다음 EnsureTokenAsync가 다시 로그인한다.
        // (서버 재시작으로 sessions가 비는 등, expiresAt이 남았어도 토큰은 죽을 수 있다.)
        public void InvalidateToken()
        {
            if (_account == null)
                return;

            _account.Token = null;
            _account.ExpiresAtUtc = null;
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
            if (_account?.Token == null || _account.ExpiresAtUtc == null)
                return false;

            if (!DateTimeOffset.TryParse(
                    _account.ExpiresAtUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTimeOffset expiresAt))
                return false;

            return DateTimeOffset.UtcNow + ExpiryMargin < expiresAt;
        }

        private async Task<bool> SignUpAsync()
        {
            // Guid 하나면 충분한 난수다 — username 은 식별용 12 hex, password 는 32 hex 전체.
            // 이 값이 곧 계정의 전부이고, account.json 을 잃으면 계정도 잃는다(D-016의 계약).
            string username = "guest-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            string password = Guid.NewGuid().ToString("N");

            ApiResult<UserResponseDto> result = await _api.SignUpAsync(username, password);

            if (!result.Ok)
            {
                // DUPLICATE(같은 12 hex 충돌)는 사실상 안 나오지만, 나오면 다음 동기화가
                // 새 이름으로 다시 시도한다 — 여기서 루프를 돌 만큼 급한 일이 아니다.
                Log("가입", result);
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
                // 401이면 서버 쪽에서 계정이 사라진 것(DB 재생성 등 — 개발 중엔 실제로 일어난다).
                // 계정을 버리면 다음 동기화가 새 게스트로 다시 선다. 로컬 세이브는 그대로다.
                if (result.Status == 401)
                {
                    Debug.LogWarning($"[계정] '{_account.Username}' 로그인 거부 — 계정을 버리고 다음에 새로 만든다.");
                    _account = null;
                    Persist();
                }
                else
                {
                    Log("로그인", result);
                }

                return null;
            }

            _account.UserId = result.Body.UserId;
            _account.Token = result.Body.Token;
            _account.ExpiresAtUtc = result.Body.ExpiresAt;

            Persist();

            return _account.Token;
        }

        private void Persist()
        {
            if (_account == null)
            {
                // 계정을 버리는 경우 — 빈 파일이 아니라 파일 자체를 없애는 것이 뜻에 맞지만,
                // 저장 층은 삭제를 하지 않는다(원자적 쓰기만). 빈 껍데기를 눕힌다.
                AtomicFile.WriteAllText(_accountPath, SaveJson.SerializePretty(new AccountFile()));
                return;
            }

            AtomicFile.WriteAllText(_accountPath, SaveJson.SerializePretty(_account));
        }

        private static void Log<T>(string what, ApiResult<T> result)
        {
            if (result.NetworkError)
                Debug.Log($"[계정] {what} — 서버에 닿지 않는다 (오프라인이면 정상).");
            else
                Debug.LogWarning($"[계정] {what} 실패 — HTTP {result.Status} {result.ErrorCode}");
        }
    }
}
