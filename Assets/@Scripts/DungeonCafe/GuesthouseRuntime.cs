// using UnityEngine;
//
// // 상위(에피소드 플레이어, 세이브 시스템)에서는 CampaignFlow 만 잡으면 된다.
// public sealed class GuesthouseRuntime
// {
//     public GuesthouseContentDB Content { get; private set; }
//     public ServiceSessionFlow Session { get; private set; }
//     public CampaignFlow Campaign { get; private set; }
//
//     public GuesthouseRuntime(
//         GuesthouseContentDB contentDB,
//         ServiceSessionFlow serviceSessionFlow,
//         CampaignFlow campaign)
//     {
//         Content = contentDB;
//         Session = serviceSessionFlow;
//         Campaign = campaign;
//     }
// }
