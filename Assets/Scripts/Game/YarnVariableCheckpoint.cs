using System.Collections.Generic;
using Yarn.Unity;

// 장면 진입 시점의 변수 스냅샷.
//
// 롤백은 스냅샷 복원이 아니라 "시작 에피소드로 부터의 결정론적 리플레이".
// 장면 도중 <<set>>으로 변수가 바뀐 채 리플레이하면 <<if>>가 다른 분기를 타고,
// 시크 표적 라인이 영영 나오지 않을 수 있음.
// 그래서 장면을 시작할 때 변수를 통째로 찍고, 리플레이 직전에 되돌림.
public sealed class YarnVariableCheckpoint
{
    private readonly VariableStorageBehaviour _storage;

    private Dictionary<string, float> _floats;
    private Dictionary<string, string> _strings;
    private Dictionary<string, bool> _bools;

    public YarnVariableCheckpoint(VariableStorageBehaviour storage)
    {
        _storage = storage;
    }

    public bool HasCapture => _floats != null;

    public void Capture()
    {
        if (_storage == null)
            return;

        (Dictionary<string, float> floats,
         Dictionary<string, string> strings,
         Dictionary<string, bool> bools) = _storage.GetAllVariables();

        // InMemoryVariableStorage는 새 딕셔너리를 주지만,
        // 커스텀 저장소에서 살아있는 참조를 줄 경우 고려.
        _floats = new Dictionary<string, float>(floats);
        _strings = new Dictionary<string, string>(strings);
        _bools = new Dictionary<string, bool>(bools);
    }

    public void Restore()
    {
        if (_storage == null || !HasCapture)
            return;

        _storage.SetAllVariables(_floats, _strings, _bools);
    }
}