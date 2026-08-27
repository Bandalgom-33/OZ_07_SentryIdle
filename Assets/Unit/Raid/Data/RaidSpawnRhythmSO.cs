using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [Serializable]
    public sealed class RaidSpawnBeatPattern
    {
        [Tooltip("각 숫자는 1 Beat에서 동시에 소환할 몬스터 수입니다. 0은 해당 Beat를 쉽니다. 배열 끝까지 재생한 뒤 처음부터 반복합니다.")]
        [SerializeField] private int[] spawnCounts = Array.Empty<int>();

        public int BeatCount => spawnCounts != null ? spawnCounts.Length : 0;

        public int GetSpawnCount(int beatIndex)
        {
            if (spawnCounts == null || spawnCounts.Length == 0)
            {
                return 0;
            }

            int index = beatIndex % spawnCounts.Length;

            if (index < 0)
            {
                index += spawnCounts.Length;
            }

            return Mathf.Clamp(spawnCounts[index], 0, 8);
        }

        public void Validate()
        {
            if (spawnCounts == null)
            {
                spawnCounts = Array.Empty<int>();
                return;
            }

            for (int i = 0; i < spawnCounts.Length; i++)
            {
                spawnCounts[i] = Mathf.Clamp(spawnCounts[i], 0, 8);
            }
        }
    }

    [CreateAssetMenu(fileName = "RaidSpawnRhythm", menuName = "EndlessGuard/Raid/Spawn Rhythm")]
    public sealed class RaidSpawnRhythmSO : ScriptableObject
    {
        [Header("Beat")]
        [Tooltip("웨이브 리듬 계산에만 사용하는 BPM입니다. 특정 AudioClip을 재생하거나 참조하지 않습니다.")]
        [Min(30f)] [SerializeField] private float bpm = 147.65625f;
        [Tooltip("한 마디의 Beat 수입니다. 현재 레퍼런스 리듬은 4/4 기준입니다.")]
        [Range(1, 16)] [SerializeField] private int beatsPerBar = 4;
        [Tooltip("Raid 전투 시작 후 첫 Beat Spawn까지의 대기 시간입니다.")]
        [Min(0f)] [SerializeField] private float startDelaySeconds = 0.1045f;

        [Header("Phase 1")]
        [SerializeField] private RaidSpawnBeatPattern phase1 = new RaidSpawnBeatPattern();

        [Header("Phase 2")]
        [SerializeField] private RaidSpawnBeatPattern phase2 = new RaidSpawnBeatPattern();

        [Header("Phase 3")]
        [SerializeField] private RaidSpawnBeatPattern phase3 = new RaidSpawnBeatPattern();

        public float Bpm => Mathf.Max(30f, bpm);
        public int BeatsPerBar => Mathf.Clamp(beatsPerBar, 1, 16);
        public float StartDelaySeconds => Mathf.Max(0f, startDelaySeconds);
        public float SecondsPerBeat => 60f / Bpm;

        public int GetSpawnCount(RaidPhase phase, int beatIndex)
        {
            RaidSpawnBeatPattern pattern = GetPattern(phase);
            return pattern != null ? pattern.GetSpawnCount(beatIndex) : 0;
        }

        public int GetPatternBeatCount(RaidPhase phase)
        {
            RaidSpawnBeatPattern pattern = GetPattern(phase);
            return pattern != null ? pattern.BeatCount : 0;
        }

        private RaidSpawnBeatPattern GetPattern(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return phase1;
                case RaidPhase.Phase2:
                    return phase2;
                case RaidPhase.Phase3:
                    return phase3;
                default:
                    return phase1;
            }
        }

        private void OnValidate()
        {
            bpm = Mathf.Max(30f, bpm);
            beatsPerBar = Mathf.Clamp(beatsPerBar, 1, 16);
            startDelaySeconds = Mathf.Max(0f, startDelaySeconds);
            phase1?.Validate();
            phase2?.Validate();
            phase3?.Validate();
        }
    }
}
