// using UnityEngine;
//
// public sealed class VNPlaytimeTracker : MonoBehaviour
// {
//     private float _startedRealtime;
//     private int _loadedBaseSeconds;
//     private bool _running;
//
//     public int CurrentPlaytimeSeconds
//     {
//         get
//         {
//             if (!_running)
//                 return _loadedBaseSeconds;
//
//             float elapsed = Time.realtimeSinceStartup - _startedRealtime;
//             return _loadedBaseSeconds + Mathf.Max(0, Mathf.FloorToInt(elapsed));
//         }
//     }
//
//     public void StartNew()
//     {
//         _loadedBaseSeconds = 0;
//         _startedRealtime = Time.realtimeSinceStartup;
//         _running = true;
//     }
//
//     public void ResumeFromSave(int savedSeconds)
//     {
//         _loadedBaseSeconds = Mathf.Max(0, savedSeconds);
//         _startedRealtime = Time.realtimeSinceStartup;
//         _running = true;
//     }
//
//     public void Stop()
//     {
//         _loadedBaseSeconds = CurrentPlaytimeSeconds;
//         _running = false;
//     }
// }