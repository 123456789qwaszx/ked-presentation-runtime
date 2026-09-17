using System;
using System.Collections.Generic;
using Ked.Progression;
using Yarn.Unity;

/// <summary>
/// 대사가 챕터 스탯을 <b>읽는</b> 길 — <c>&lt;&lt;if stat("trust") &gt;= 3&gt;&gt;</c>.
///
/// ⛔ 변수(<c>$trust</c>)로 심지 않는다. 그러면 <c>&lt;&lt;set $trust = 5&gt;&gt;</c>가 함께 유효해지고,
/// 대사 중의 스탯 쓰기는 세이브/로드 복귀와 도달성 증명이 못 보는 뒷길이 된다.
/// 함수에는 왼쪽 변이 없으므로 <b>금지가 아니라 불가능</b>하다.
///
/// 읽는 값은 <see cref="ProgressionDriver.CurrentState"/>, 즉 간선 관문이 보는 것과 같은
/// WorkingState다. 둘이 갈리면 "선택지는 잠겼는데 대사 조건은 통과"가 생긴다.
/// </summary>
public sealed class ProgressionStatFunction
{
    // Yarn 쪽 표기의 주인은 저작 도구의 YarnSyntax.StatFunction 한 곳이다.
    // 이름을 바꾸려면 양쪽을 함께 바꾼다.
    public const string FunctionName = "stat";

    private readonly Func<ProgressionState> _currentState;

    public ProgressionStatFunction(Func<ProgressionState> currentState)
    {
        _currentState = currentState;
    }

    public void Register(IActionRegistration registration)
    {
        registration.AddFunction<string, int>(FunctionName, Read);
    }

    // 깃발도 정수 0/1이다 — 별도 타입을 두지 않는다.
    private int Read(string key)
    {
        ProgressionState state = _currentState();

        if (state == null)
        {
            throw new InvalidOperationException(
                $"[진행] 진행이 돌고 있지 않은데 {FunctionName}(\"{key}\")를 물었다. " +
                "진행 층 밖에서 재생하는 노드는 스탯을 읽을 수 없다.");
        }

        try
        {
            return state.GetStat(key);
        }
        catch (KeyNotFoundException error)
        {
            // 조용히 0으로 떨어뜨리지 않는다. 오타 난 조건이 "언제나 통과하는 관문"으로
            // 바뀌는 것이 이 계층에서 가장 찾기 힘든 버그다.
            throw new KeyNotFoundException(
                $"[진행] 대사가 정의되지 않은 스탯 '{key}'를 물었다. " +
                "챕터 JSON의 Stats[].Key와 같은 이름이어야 한다.", error);
        }
    }
}
