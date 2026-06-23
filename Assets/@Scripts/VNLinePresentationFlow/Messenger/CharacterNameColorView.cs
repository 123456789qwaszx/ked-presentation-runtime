using System.Collections.Generic;
using TMPro;
using UnityEngine;
#nullable enable

namespace Yarn.Unity.Samples
{
    public class CharacterNameColorView : DialoguePresenterBase
    {
        [SerializeField] SerializableDictionary<string, Color> characterColors = new();
        [SerializeField] Color defaultColor = Color.white;

        
        [SerializeField] List<TMP_Text> texts = new();

        public override YarnTask OnDialogueStartedAsync()
        {
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {

            Color color;

            if (string.IsNullOrEmpty(line.CharacterName) || !characterColors.TryGetValue(line.CharacterName, out color))
            {
                color = defaultColor;
            }

            foreach (var text in texts)
            {
                text.color = color;
            }

            return YarnTask.CompletedTask;
        }
    }
}