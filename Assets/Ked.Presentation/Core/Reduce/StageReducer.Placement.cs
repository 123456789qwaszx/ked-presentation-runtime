namespace Ked.Presentation.Core
{
    /// <summary>
    /// size — 깊이. 깊이 레벨의 값을 그대로 놓는다(NodeTargets.TrySetDepth). 여기는 토큰 해석과 배선만 맡는다.
    /// </summary>
    public static partial class StageReducer
    {
        private static bool ApplySize(
            StageState state, in StageCommand cmd, StageReducerTuning tuning,
            string depthToken, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            if (!NodeTargets.TrySetDepth(
                    slotKey, depthToken, tuning.DepthLevel,
                    out NodeTarget depthYTarget, out NodeTarget depthScaleTarget,
                    out reason))
            {
                return false;
            }

            state.Apply(depthYTarget);
            state.Apply(depthScaleTarget);
            return true;
        }
    }
}
