using UnityEngine;

namespace Harukerryzi.Clash
{
    [CreateAssetMenu(fileName = "ClashFighterConfig", menuName = "Clash/Fighter Config")]
    public sealed class ClashFighterConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "Fighter";
        [SerializeField, Min(0f)] private float powerPerMash = 1f;
        [SerializeField, Min(0f)] private float aiMashesPerSecond = 4f;
        [SerializeField, Range(0f, 1f)] private float aiMashRandomness = 0.2f;

        public string DisplayName => displayName;
        public float PowerPerMash => powerPerMash;
        public float AiMashesPerSecond => aiMashesPerSecond;
        public float AiMashRandomness => aiMashRandomness;
    }
}
