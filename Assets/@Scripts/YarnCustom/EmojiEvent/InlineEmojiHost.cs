// using UnityEngine;
//
// public sealed class InlineEmojiHost : MonoBehaviour, InlineEventMarkupHandler.IInlineEmojiHost
// {
//     private YarnCommandBridge _commandBridge;
//
//     public void Initialize(YarnCommandBridge commandBridge)
//     {
//         _commandBridge = commandBridge;
//     }
//
//     public void PlayEmojiCue(string characterKey, string cue)
//     {
//         if (_commandBridge == null)
//             return;
//
//         if (string.IsNullOrWhiteSpace(cue))
//         {
//             _commandBridge.HideInlineEmojiByCharacterNow(characterKey);
//             return;
//         }
//
//         _commandBridge.PlayInlineEmojiByCharacterNow(characterKey, cue);
//     }
// }