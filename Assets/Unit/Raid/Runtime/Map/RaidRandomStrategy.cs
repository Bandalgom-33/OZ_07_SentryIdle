using System;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidRandomStrategy : IRaidPathStrategy
    {
        private readonly int seed;
        private readonly uint[] sequences;

        public RaidRandomStrategy(int seed, int keyCount)
        {
            if (keyCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(keyCount));
            }

            this.seed = seed;
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
            uint hash = unchecked((uint)seed);
            hash ^= unchecked((uint)candidates.Key) * 0x9E3779B9u;
            hash ^= sequence * 0x85EBCA6Bu;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            return (int)(hash % (uint)candidates.Count);
        }

        public void Reset()
        {
            Array.Clear(sequences, 0, sequences.Length);
        }
    }
}