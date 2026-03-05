using UnityEngine;

namespace Story.Data
{
    [CreateAssetMenu(fileName = "WandererStats", menuName = "Story/Wanderer Stats")]
    public class WandererStatsSO : ScriptableObject
    {
        [Header("Starting Values")]
        [Range(1, 200)] public int maxHealth   = 100;
        [Range(1, 200)] public int maxPower = 100;
        [Range(1, 200)] public int maxSanity   = 100;

        [Header("Starting Values (can differ from max)")]
        [Range(1, 200)] public int startHealth   = 100;
        [Range(1, 200)] public int startPower = 60;
        [Range(1, 200)] public int startSanity   = 100;
    }
}
