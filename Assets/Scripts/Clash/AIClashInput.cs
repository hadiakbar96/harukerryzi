using UnityEngine;

namespace Harukerryzi.Clash
{
    public sealed class AIClashInput : MonoBehaviour, IClashInput
    {
        [SerializeField, Min(0f)] private float mashesPerSecond = 4f;
        [SerializeField, Range(0f, 1f)] private float mashRandomness = 0.2f;
        [SerializeField] private AnimationCurve difficultyOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        private float elapsedTime;
        private float nextMashTime;
        private int bufferedMashes;

        public void Configure(float newMashesPerSecond, float newMashRandomness)
        {
            mashesPerSecond = Mathf.Max(0f, newMashesPerSecond);
            mashRandomness = Mathf.Clamp01(newMashRandomness);
            ScheduleNextMash(Time.time);
        }

        private void OnEnable()
        {
            elapsedTime = 0f;
            bufferedMashes = 0;
            ScheduleNextMash(Time.time);
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            if (mashesPerSecond <= 0f || Time.time < nextMashTime)
            {
                return;
            }

            bufferedMashes++;
            ScheduleNextMash(Time.time);
        }

        public bool ConsumeMash()
        {
            if (bufferedMashes <= 0)
            {
                return false;
            }

            bufferedMashes--;
            return true;
        }

        private void ScheduleNextMash(float fromTime)
        {
            if (mashesPerSecond <= 0f)
            {
                nextMashTime = float.PositiveInfinity;
                return;
            }

            float normalizedTime = Mathf.Clamp01(elapsedTime / 30f);
            float difficultyMultiplier = Mathf.Max(0.01f, difficultyOverTime.Evaluate(normalizedTime));
            float interval = 1f / (mashesPerSecond * difficultyMultiplier);
            float randomScale = Random.Range(1f - mashRandomness, 1f + mashRandomness);
            nextMashTime = fromTime + interval * randomScale;
        }
    }
}
