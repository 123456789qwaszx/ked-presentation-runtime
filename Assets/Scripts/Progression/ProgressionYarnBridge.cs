using System;
using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 진행 층이 Yarn 변수 저장소에 하는 일 모음소.
///
/// Yarn 저장소는 [3] 연출 실행 상태의 집이다 — 작가가 툴에서 만든 것, Yarn이 온전히 쥔다.
/// 수명이 챕터라 챕터 진입에서 선언 초기값으로 되돌린다.
///
/// [2] 진행 스탯은 여기 오지 않는다. 진행 코어만 알고, 대사에서 읽는 것도 금지 —
/// 스탯 분기는 그래프 간선으로 올린다.
/// </summary>
public sealed class ProgressionYarnBridge
{
    private readonly VariableStorageBehaviour _storage;

    public ProgressionYarnBridge(VariableStorageBehaviour storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// [3]을 챕터 초기 상태로 되돌린다.
    /// 챕터가 다르면 같은 이름이라도 아무 연관이 없다.
    /// 이으려면 작가가 이전 챕터의 마지막 값을 새 챕터의 초기값으로 직접 지정.
    ///
    /// 챕터 안에서는 에피소드 경계를 자유롭게 넘는다.
    /// </summary>
    public void BeginChapter(YarnProject project)
    {
        _storage.Clear();

        // 초기값 선언(<<declare>>)
        foreach (KeyValuePair<string, IConvertible> declared in project.InitialValues)
        {
            switch (declared.Value)
            {
                case string text: _storage.SetValue(declared.Key, text); break;
                case bool flag: _storage.SetValue(declared.Key, flag); break;
                default: _storage.SetValue(declared.Key, Convert.ToSingle(declared.Value)); break;
            }
        }
    }

    // [3] 통덤프 — 장면 끝에 굽는다. 저장소에 [2]가 없으니 필터링 없이 전부.
    public YarnVariableSnapshot Capture()
    {
        (Dictionary<string, float> floats,
         Dictionary<string, string> strings,
         Dictionary<string, bool> bools) = _storage.GetAllVariables();

        var snapshot = new YarnVariableSnapshot();

        foreach (KeyValuePair<string, float> pair in floats) snapshot.Floats[pair.Key] = pair.Value;
        foreach (KeyValuePair<string, string> pair in strings) snapshot.Strings[pair.Key] = pair.Value;
        foreach (KeyValuePair<string, bool> pair in bools) snapshot.Bools[pair.Key] = pair.Value;

        return snapshot;
    }

    // 덤프를 declare 초기값 위에 덮는다 — BeginChapter 뒤에. Clear하지 않으므로 덤프에 없는
    // 신규 declare는 초기값으로 남는다 ([2] Restore가 저장 후 추가된 스탯을 다루는 것과 같은 문장).
    public void Restore(YarnVariableSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        foreach (KeyValuePair<string, float> pair in snapshot.Floats) _storage.SetValue(pair.Key, pair.Value);
        foreach (KeyValuePair<string, string> pair in snapshot.Strings) _storage.SetValue(pair.Key, pair.Value);
        foreach (KeyValuePair<string, bool> pair in snapshot.Bools) _storage.SetValue(pair.Key, pair.Value);
    }
}