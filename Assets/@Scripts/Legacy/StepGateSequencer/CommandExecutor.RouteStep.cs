// using System.Collections.Generic;
//
// public partial class CommandExecutor
// {
//     public void PlayStep(NodeSpec node, int stepIndex, CommandRunScope scope)
//     {
//         CloseActiveTicketIfOpen(CommandRunTicketCloseReason.Superseded);
//
//         int runId = _runId;
//         _activeScope = scope;
//
//         CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
//         _activeScope.CleanupStep(policy);
//
//         List<ISequenceCommand> commands = BuildCommandsFromStep(node, stepIndex);
//         int commandCount = commands.Count;
//
//         var ticket = new CommandRunTicket(commandCount);
//         _activeTicket = ticket;
//
//         ResetToken();
//         _activeScope.Token = _cts.Token;
//
//         _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, runId, ticket));
//     }
//     
//     private List<ISequenceCommand> BuildCommandsFromStep(NodeSpec node, int stepIndex)
//     {
//         var list = new List<ISequenceCommand>();
//
//         if (node == null || node.steps == null || node.steps.Count == 0)
//             return list;
//
//         if (stepIndex < 0 || stepIndex >= node.steps.Count)
//             return list;
//
//         StepSpec step = node.steps[stepIndex];
//
//         return BuildCommandsFromSpecs(step.compiled);
//     }
// }
