using UnityEngine;

// 메인 흐름과 완전히 분리된 전용 Executor/Context로 SequenceSpecSO 하나를
// "포인터"로 재생하는 전용 러너.
// PresentationSession의 단일 소유자 모델(Start 시 이전 실행을 자동 종료)을 그대로 빌려 쓰되,
// Route 해석이나 메인 advance gate 보고 책임은 갖지 않는다.
public sealed class OverlaySequenceRunner : MonoBehaviour
{
    private PresentationSession _session;

    public void Initialize(PresentationSession dedicatedSession)
    {
        _session = dedicatedSession;
    }

    private void Update()
    {
        _session?.Tick();
    }

    public void Play(SequenceSpecSO sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning("[OverlaySequenceRunner] sequence is null.");
            return;
        }

        // Route는 디버그 표기용 메타데이터일 뿐, 실제로 무엇을 resolve하지 않는다.
        _session.Play(sequence);
    }

    public void PlayByKey(string sequenceKey, SequenceCatalogSO catalog)
    {
        if (catalog == null || !catalog.TryGetSequence(sequenceKey, out SequenceSpecSO sequence))
        {
            Debug.LogWarning($"[OverlaySequenceRunner] sequenceKey not found: '{sequenceKey}'");
            return;
        }

        Play(sequence);
    }

    public void EndImmediately() => _session?.EndImmediately();
}