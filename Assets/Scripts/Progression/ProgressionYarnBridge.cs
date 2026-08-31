using System;
using System.Collections.Generic;
using Ked.Progression;
using Yarn.Unity;

/// <summary>
/// 진행 층이 Yarn 변수 저장소에 하는 일 모음소.
/// 변수 테이블의 계층이 둘이고 수명과 역할 다름.
/// ([1]은 해금 플래그, 본 엔딩등의 영구 상태로써, 이 프로젝트에서 다루지 않는 계층.)
///
/// [2] 에피소드 상태:
/// - 호감도 / 능력치. 진행 코어가 쥐고, 바뀌는 자리는 오직 "간선 커밋"
///  여기서는 심기만 함.
/// 대사가 "if $호감도"로 읽을 수 있게 하는 것이 전부이고,
/// Yarn에서 바뀐 값은 돌려받지 않는다.
/// 
/// [3] 연출 실행 상태:
/// 작가가 툴에서 만든 것. Yarn이 온전히 쥔다.
/// 수명이 챕터라 챕터 진입에서 선언 초기값으로 되돌린다.
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
    ///
    /// <b>Clear()는 계층을 가리지 않는다</b> — [2]도 같이 지워진다. 그래서 이 뒤에
    /// 반드시 <see cref="PublishStats"/>가 따라와야 하고, 그 순서를 지키는 것은
    /// 챕터가 바뀌는 것을 아는 쪽(ProgressionDriver)의 일이다.
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

    /// <summary>
    /// [2]를 Yarn으로 추가. 에피소드 노드를 틀기 직전.
    ///
    /// 선언 지점은, YarnVariableCheckpoint.Capture()보다 앞이어야 한다
    /// 그래야 롤백 리플레이가 같은 값에서 다시 출발.
    ///
    /// 값만 건너가고 <b>정의는 건너가지 않는다.</b> 그래서 bool 스탯도 숫자로 심긴다 —
    /// Yarn 쪽 선언이 bool이면 그 뒤로 읽히지 않는다. 그 어긋남은
    /// <c>ProgressionContentPreflight</c>가 재생 전에 잡는다.
    /// </summary>
    public void PublishStats(
        IReadOnlyList<StatDefinition> definitions, IReadOnlyDictionary<string, int> values)
    {
        if (_storage == null || definitions == null || values == null)
            return;

        for (int i = 0; i < definitions.Count; i++)
        {
            StatDefinition definition = definitions[i];

            if (!values.TryGetValue(definition.Key, out int value))
                continue;

            string name = NameOf(definition.Key);

            // 깃발은 bool로 심는다. 숫자로 심으면 저장소가 그 변수의 런타임 타입을
            // float으로 도장해 버려서, 작가가 "<<declare $깃발 = false>>"로 적어 둔
            // 변수를 그 뒤로 읽을 수 없다 — 조용히 다른 분기를 탄다.
            if (definition.Type == StatType.Bool)
                _storage.SetValue(name, value != 0);
            else
                _storage.SetValue(name, (float)value);
        }
    }

    //저작 쪽은 "$" 없이 적고(기획자 언어)
    // Yarn 저장소는 "$"로 센다.
    private static string NameOf(string statKey) =>
        statKey.StartsWith("$", StringComparison.Ordinal)
            ? statKey
            : "$" + statKey;
}