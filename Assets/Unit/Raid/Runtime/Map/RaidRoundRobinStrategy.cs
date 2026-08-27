using System;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidRoundRobinStrategy : IRaidPathStrategy
    {
        private readonly uint[] sequences;

        public RaidRoundRobinStrategy(int keyCount)
        {
            if (keyCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(keyCount));
            }

            sequences = new uint[keyCount];
        }

        public int Select(in RaidPathCandidates candidates)
        {
            if (candidates.Count < 1)
            {
                throw new InvalidOperationException($"선택 가능한 Path가 없습니다. Key: {candidates.Key}");
            }

            if (candidates.Key < 0 || candidates.Key >= sequences.Length)
            {
                throw new InvalidOperationException($"Path Strategy Key가 범위를 벗어났습니다. Key: {candidates.Key}");
            }

            uint sequence = sequences[candidates.Key]++;
            uint offset = unchecked((uint)candidates.Key) + sequence;
            return (int)(offset % (uint)candidates.Count);
        }

        public void Reset()
        {
            Array.Clear(sequences, 0, sequences.Length);
        }
    }
}