// using System;
// using System.Collections;
// using UnityEngine;
//
// [Serializable]
// public sealed class EmojiByCharacterCommandSpec : CommandSpecBase
// {
//     public string characterKey;
//     public string emojiCue;
// }
//
// public sealed class EmojiByCharacterCommand : CommandBase
// {
//     private readonly EmojiByCharacterCommandSpec _spec;
//     private readonly IEmojiCommandEmitter _emitter;
//
//     public EmojiByCharacterCommand(
//         EmojiByCharacterCommandSpec spec,
//         IEmojiCommandEmitter emitter)
//     {
//         _spec = spec;
//         _emitter = emitter;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         if (!scope.CastRegistry.TryGetRole(_spec.characterKey, out string roleKey))
//         {
//             Debug.LogWarning($"[EmojiByCharacter] No cast role found for character='{_spec.characterKey}'");
//             yield break;
//         }
//
//         _emitter.Emit(roleKey, _spec.emojiCue, scope);
//     }
// }