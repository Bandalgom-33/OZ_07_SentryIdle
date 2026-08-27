using System;
using System.Collections.Generic;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal readonly struct RaidPathCandidates
    {
        private readonly int[] indices;
        private readonly IReadOnlyList<RaidTravelPath> paths;
        private readonly int start;

        public int Key { get; }
        public int Count { get; }

        public RaidPathCandidates(int key, int[] indices, int start, int count, IReadOnlyList<RaidTravelPath> paths)
        {
            if (key < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (start < 0 || count < 0 || count > indices.Length || start > indices.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Path 후보 범위가 올바르지 않습니다.");
            }

            Key = key;
            this.indices = indices;
            this.paths = paths;
            this.start = start;
            Count = count;
        }

        public int GetIndex(int candidateIndex)
        {
            ValidateCandidate(candidateIndex);
            return indices[start + candidateIndex];
        }

        public RaidTravelPath GetPath(int candidateIndex)
        {
            int pathIndex = GetIndex(candidateIndex);

            if (pathIndex < 0 || pathIndex >= paths.Count)
            {
                throw new InvalidOperationException($"Path Index가 범위를 벗어났습니다. Index: {pathIndex}");
            }

            return paths[pathIndex];
        }

        private void ValidateCandidate(int candidateIndex)
        {
            if (candidateIndex < 0 || candidateIndex >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(candidateIndex), candidateIndex, "Path 후보 Index가 범위를 벗어났습니다.");
            }
        }
    }
}