using UnityEngine;
using Yarn.Unity;

public sealed class YarnVariableReader : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;

    public void LogCurrentState()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Debug.LogWarning("[YarnVariableReader] DialogueRunner or VariableStorage is null.", this);
            return;
        }

        var storage = dialogueRunner.VariableStorage;

        storage.TryGetValue("$favor", out float favor);
        storage.TryGetValue("$trust", out float trust);
        storage.TryGetValue("$anger", out float anger);
        storage.TryGetValue("$laru_patience", out float patience);
        storage.TryGetValue("$willow_debt", out float debt);
        storage.TryGetValue("$contract_signed", out bool contractSigned);

        Debug.Log(
            $"[YarnVariableReader] favor={favor}, trust={trust}, anger={anger}, " +
            $"patience={patience}, debt={debt}, contract={contractSigned}",
            this);
    }
    
    public void ApplyBonusTrust()
    {
        var storage = dialogueRunner.VariableStorage;

        storage.TryGetValue("$trust", out float trust);
        trust += 2f;

        storage.SetValue("$trust", trust);

        Debug.Log($"[YarnVariableReader] Applied trust bonus. trust={trust}", this);
    }
    
    public void ApplyBroadcastResultToYarn(float favorBonus, float riskPenalty)
    {
        var storage = dialogueRunner.VariableStorage;

        storage.TryGetValue("$favor", out float favor);
        storage.TryGetValue("$anger", out float anger);

        storage.SetValue("$favor", favor + favorBonus);
        storage.SetValue("$anger", anger + riskPenalty);

        Debug.Log(
            $"[YarnVariableReader] Broadcast result applied. favor={favor + favorBonus}, anger={anger + riskPenalty}",
            this);
    }
}