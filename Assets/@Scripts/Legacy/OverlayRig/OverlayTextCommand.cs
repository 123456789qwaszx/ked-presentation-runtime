// using System;
// using System.Collections;
// using UnityEngine;
//
// [Serializable]
// [CommandMenuHint(
//     "Stage Overlay",
//     "Overlay Text",
//     Order = -934)]
// public sealed class OverlayTextCommandSpec : CommandSpecBase
// {
//     [Header("Overlay")]
//     public string rigKey;
//
//     [Header("Text")]
//     public string text;
// }
//
// public sealed class OverlayTextCommand : CommandBase
// {
//     private readonly OverlayTextCommandSpec _spec;
//
//     public OverlayTextCommand(OverlayTextCommandSpec spec)
//     {
//         _spec = spec;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         if (scope.OverlayRigs.TryGet(_spec.rigKey, out OverlayRigRefs refs))
//             refs.SetText(_spec.text);
//
//         yield break;
//     }
//
//     protected override void OnSkip(CommandRunScope scope)
//     {
//         if (scope.OverlayRigs.TryGet(_spec.rigKey, out OverlayRigRefs refs))
//             refs.SetText(_spec.text);
//     }
// }