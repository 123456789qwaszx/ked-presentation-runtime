using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    // 실행 중인 Scene의 Runtime wrapper.
    //
    // 진행 데이터의 source of truth는 SceneProgression이다.
    // Transaction은 실행 중에만 필요한 replay request/restore input만 소유한다.
    public sealed class SceneRunContext
    {
        public SceneProgress Progress { get; }
        
        // - null: New Game.
        // - 빈 목록: 장면 루트 자체가 저장 위치.
        public IReadOnlyList<ScenePathStep> RestorePath { get; }

        public bool ReplayPending { get; private set; }
        
        public SceneRunContext(
            SceneProgress progression,
            IReadOnlyList<ScenePathStep> restorePath = null)
        {
            Progress = progression;
            RestorePath = restorePath;
        }

        internal bool RequestReplay()
        {
            if (ReplayPending)
                return false;

            ReplayPending = true;
            return true;
        }
        
        internal void ClearReplayRequest()
        {
            ReplayPending = false;
        }
    }
}