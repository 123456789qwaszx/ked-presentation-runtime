using System;

/// <summary>
/// 샷의 축 — 전환의 단위다. 한 축 = 한 전환: 명령은 제가 적은 축만 다시 조준하고, 적지 않은 축은 하던 전환을 제 길이로 마친다.
/// 초점은 줌에 실린다(층 산개의 기준이라 줌과 한 몸이다).
/// </summary>
[Flags]
public enum ShotAxes
{
    None = 0,

    /// <summary>렌즈 — 줌과 초점.</summary>
    Zoom = 1 << 0,

    /// <summary>렌즈 — 팬(화면 밀기).</summary>
    Pan = 1 << 1,

    /// <summary>헤드 — 롤.</summary>
    Roll = 1 << 2,

    /// <summary>몸 — 달리.</summary>
    Dolly = 1 << 3,

    /// <summary>몸 — 트랙.</summary>
    Track = 1 << 4,

    Lens = Zoom | Pan,
    Body = Dolly | Track,
    All = Lens | Roll | Body,
}
