using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(menuName = "CPS/Portraits/Build Settings", fileName = "PortraitBuildSettings")]
public sealed class PortraitBuildSettings : ScriptableObject
{
    [Header("Scan Folders")]
    [Tooltip("Portrait 스프라이트를 스캔할 폴더들")]
    public List<string> scanFolders = new List<string>
    {
        "Assets/Art/Portraits/Playable",
        "Assets/Art/Portraits/NPC",
    };

    [Header("Output")]
    [Tooltip("생성된 DB를 저장할 경로")]
    public string generatedDbPath = "Assets/Data/Generated/PortraitGeneratedDB.asset";

    [Header("Validation")]
    [Tooltip("구조적 오류(중복/파싱 불가 등) 발생 시 빌드 실패")]
    public bool strictMode = false;
}