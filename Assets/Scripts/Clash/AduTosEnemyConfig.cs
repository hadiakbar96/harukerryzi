using UnityEngine;

namespace Harukerryzi.Clash
{
    [CreateAssetMenu(fileName = "AduTosEnemy", menuName = "Clash/Adu Tos Enemy Config")]
    public sealed class AduTosEnemyConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "Enemy";
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0f)] private float baseMashPower = 10f;
        [SerializeField, Min(0f)] private float mashesPerSecond = 4f;
        [SerializeField, Range(0f, 1f)] private float mashRandomness = 0.2f;
        [SerializeField] private ClashItemConfig[] itemPool;
        [SerializeField] private Sprite enemyHandSprite;
        [SerializeField, Min(0.1f)] private float entranceHandScale = 1f;
        [SerializeField] private Vector2 entranceHandStartPosition = new(980f, -520f);
        [SerializeField] private Vector2 entranceHandTargetPosition = new(390f, -90f);
        [SerializeField] private Sprite clashHandsSprite;
        [SerializeField, Min(0.1f)] private float clashHandsScale = 1f;
        [SerializeField] private Sprite clashBackgroundSprite;
        [SerializeField, Min(0)] private int rewardOnWin = 20;
        [SerializeField, Min(0)] private int rewardOnLose = 20;

        public string DisplayName => displayName;
        public int Level => level;
        public float BaseMashPower => baseMashPower;
        public float MashesPerSecond => mashesPerSecond;
        public float MashRandomness => mashRandomness;
        public ClashItemConfig[] ItemPool => itemPool;
        public Sprite EnemyHandSprite => enemyHandSprite;
        public float EntranceHandScale => entranceHandScale;
        public Vector2 EntranceHandStartPosition => entranceHandStartPosition;
        public Vector2 EntranceHandTargetPosition => entranceHandTargetPosition;
        public Sprite ClashHandsSprite => clashHandsSprite;
        public float ClashHandsScale => clashHandsScale;
        public Sprite ClashBackgroundSprite => clashBackgroundSprite;
        public int RewardOnWin => rewardOnWin;
        public int RewardOnLose => rewardOnLose;
    }
}
