using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidCollapseCluster
    {
        private readonly Vector2Int[] tiles;

        public int Id { get; }
        public IReadOnlyList<Vector2Int> Tiles => tiles;
        public int TileCount => tiles.Length;
        public Vector2 Center { get; }

        public RaidCollapseCluster(int id, Vector2Int[] tiles)
        {
            Id = id;
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));

            if (tiles.Length == 0)
            {
                Center = Vector2.zero;
                return;
            }

            Vector2 sum = Vector2.zero;

            for (int i = 0; i < tiles.Length; i++)
            {
                sum += tiles[i];
            }

            Center = sum / tiles.Length;
        }
    }

    internal static class RaidCollapseClusterBuilder
    {
        private static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        public static RaidCollapseCluster[] Build(bool[] collapsingTiles, int width, int height)
        {
            if (collapsingTiles == null)
            {
                throw new ArgumentNullException(nameof(collapsingTiles));
            }

            if (width <= 0 || height <= 0 || collapsingTiles.Length != width * height)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "붕괴 Tile 배열과 Board 크기가 일치하지 않습니다.");
            }

            bool[] visited = new bool[collapsingTiles.Length];
            int[] queue = new int[collapsingTiles.Length];
            List<RaidCollapseCluster> clusters = new List<RaidCollapseCluster>();
            List<Vector2Int> tiles = new List<Vector2Int>(16);

            for (int start = 0; start < collapsingTiles.Length; start++)
            {
                if (!collapsingTiles[start] || visited[start])
                {
                    continue;
                }

                tiles.Clear();
                int head = 0;
                int tail = 0;
                queue[tail++] = start;
                visited[start] = true;

                while (head < tail)
                {
                    int current = queue[head++];
                    Vector2Int coordinate = new Vector2Int(current % width, current / width);
                    tiles.Add(coordinate);

                    for (int i = 0; i < Directions.Length; i++)
                    {
                        Vector2Int next = coordinate + Directions[i];

                        if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height)
                        {
                            continue;
                        }

                        int nextIndex = next.y * width + next.x;

                        if (!collapsingTiles[nextIndex] || visited[nextIndex])
                        {
                            continue;
                        }

                        visited[nextIndex] = true;
                        queue[tail++] = nextIndex;
                    }
                }

                clusters.Add(new RaidCollapseCluster(clusters.Count, tiles.ToArray()));
            }

            return clusters.ToArray();
        }
    }

    internal sealed class RaidCollapseView
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
        private readonly RaidBoardRuntime boardRuntime;
        private readonly RaidBoardView boardView;
        private readonly RaidTileVisualSetSO visualSet;
        private readonly List<ClusterVisual> clusterVisuals = new List<ClusterVisual>(16);
        private readonly List<GameObject> detachedInstances = new List<GameObject>(128);
        private readonly MaterialPropertyBlock crackProperties = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock auroraProperties = new MaterialPropertyBlock();
        private GameObject root;
        private Mesh crackMesh;
        private Mesh auroraMesh;
        private MeshRenderer crackRenderer;
        private MeshRenderer auroraRenderer;
        private bool active;
        private bool committed;
        private float visualScale = 1f;

        public RaidCollapseView(RaidBoardRuntime boardRuntime)
        {
            this.boardRuntime = boardRuntime != null ? boardRuntime : throw new ArgumentNullException(nameof(boardRuntime));
            boardView = boardRuntime.BoardView != null ? boardRuntime.BoardView : throw new InvalidOperationException("Raid Board View가 없습니다.");
            visualSet = boardView.VisualSet != null ? boardView.VisualSet : throw new InvalidOperationException("Raid Tile Visual Set이 없습니다.");
        }

        public void Begin(RaidPhaseTransitionPlan plan, RaidBoard board, float effectScale)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (visualSet.CollapseCrackMaterial == null || visualSet.CollapseBeamMaterial == null)
            {
                throw new InvalidOperationException("Raid Collapse Crack/Aurora Material이 연결되지 않았습니다.");
            }

            Dispose(false);
            visualScale = Mathf.Clamp(effectScale, 0.5f, 2f);
            root = new GameObject("RaidCollapseFX");
            root.transform.SetParent(boardView.transform, false);
            active = true;
            committed = false;
            clusterVisuals.Clear();
            detachedInstances.Clear();
            BuildClusterVisuals(plan, board);
            BuildCrack(plan, board);
            BuildAurora(plan, board);
            SetRendererIntensity(crackRenderer, crackProperties, 0f);
            SetRendererReveal(crackRenderer, crackProperties, 0f);
            SetRendererIntensity(auroraRenderer, auroraProperties, 0f);
        }

        public void Update(float elapsed, float duration)
        {
            if (!active)
            {
                return;
            }

            float crackRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.2f, elapsed));
            float crackFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.48f, 2.4f, elapsed));
            float crackReveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.96f, elapsed));
            SetRendererIntensity(crackRenderer, crackProperties, crackRise * crackFade * 1.08f * visualScale);
            SetRendererReveal(crackRenderer, crackProperties, crackReveal);

            float auroraRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.88f, elapsed));
            float auroraPulse = 0.92f + Mathf.Sin(elapsed * 8.7f) * 0.07f + Mathf.Sin(elapsed * 17.3f + 0.8f) * 0.05f;
            float auroraFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.88f, 2.68f, elapsed));
            SetRendererIntensity(auroraRenderer, auroraProperties, auroraRise * auroraFade * auroraPulse * 1.46f * visualScale);

            for (int i = 0; i < clusterVisuals.Count; i++)
            {
                clusterVisuals[i].Update(elapsed);
            }
        }

        public void MarkCommitted()
        {
            committed = true;
        }

        public void Dispose(bool restoreBoard)
        {
            bool shouldRestore = active && restoreBoard && !committed && boardRuntime.Board != null && boardRuntime.CurrentMapData != null;

            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }

            if (crackMesh != null)
            {
                UnityEngine.Object.Destroy(crackMesh);
            }

            if (auroraMesh != null)
            {
                UnityEngine.Object.Destroy(auroraMesh);
            }

            root = null;
            crackMesh = null;
            auroraMesh = null;
            crackRenderer = null;
            auroraRenderer = null;
            clusterVisuals.Clear();
            detachedInstances.Clear();
            active = false;
            committed = false;
            visualScale = 1f;

            if (shouldRestore)
            {
                boardRuntime.RefreshVisuals();
            }
        }

        private void BuildClusterVisuals(RaidPhaseTransitionPlan plan, RaidBoard board)
        {
            IReadOnlyList<RaidCollapseCluster> sourceClusters = plan.Clusters;
            int[] order = BuildFallOrder(sourceClusters);
            int[] sequence = new int[sourceClusters.Count];

            for (int i = 0; i < order.Length; i++)
            {
                sequence[order[i]] = i;
            }

            for (int clusterIndex = 0; clusterIndex < sourceClusters.Count; clusterIndex++)
            {
                RaidCollapseCluster cluster = sourceClusters[clusterIndex];
                Vector3 center = CalculateClusterWorldCenter(cluster, board);
                GameObject clusterObject = new GameObject($"CollapseCluster_{clusterIndex + 1:00}");
                Transform clusterRoot = clusterObject.transform;
                clusterRoot.SetParent(root.transform, true);
                clusterRoot.position = center;
                clusterRoot.rotation = Quaternion.identity;
                int before = detachedInstances.Count;

                for (int tileIndex = 0; tileIndex < cluster.TileCount; tileIndex++)
                {
                    boardView.DetachTileVisuals(cluster.Tiles[tileIndex], clusterRoot, detachedInstances);
                }

                if (detachedInstances.Count == before)
                {
                    UnityEngine.Object.Destroy(clusterObject);
                    continue;
                }

                int seed = Hash(plan.SourceMap.VisualKey, clusterIndex, cluster.TileCount);
                float signedX = Signed01(seed ^ 0x2F13A5B7);
                float signedZ = Signed01(seed ^ 0x51C7D24B);
                Vector3 drift = new Vector3(signedX * 1.08f, 0f, signedZ * 0.9f);
                Vector3 tiltAxis = new Vector3(0.55f + Mathf.Abs(signedZ), 0f, 0.45f + Mathf.Abs(signedX)).normalized;
                float preTilt = Mathf.Lerp(6.5f, 12.5f, Mathf.Abs(Signed01(seed ^ 0x7A5F16E3)));
                float finalTilt = Mathf.Lerp(26f, 46f, Mathf.Abs(Signed01(seed ^ 0x13B04D91)));
                float severity = Mathf.Clamp01((visualScale - 1f) / 0.5f);
                float fallStart = 1.08f + sequence[clusterIndex] * Mathf.Lerp(0.078f, 0.055f, severity);
                float fallDuration = (0.82f + Mathf.Min(cluster.TileCount, 10) * 0.025f) * Mathf.Lerp(1f, 0.86f, severity);
                float fallDistance = (10.4f + Mathf.Min(cluster.TileCount, 10) * 0.32f) * Mathf.Lerp(1f, 1.18f, severity);
                finalTilt *= Mathf.Lerp(1f, 1.18f, severity);
                clusterVisuals.Add(new ClusterVisual(clusterRoot, center, drift, tiltAxis, preTilt, finalTilt, fallStart, fallDuration, fallDistance, seed));
            }
        }

        private void BuildCrack(RaidPhaseTransitionPlan plan, RaidBoard board)
        {
            List<Vector3> vertices = new List<Vector3>(512);
            List<int> triangles = new List<int>(768);
            List<Vector2> uvs = new List<Vector2>(512);
            List<Color> colors = new List<Color>(512);
            IReadOnlyList<RaidCollapseCluster> clusters = plan.Clusters;

            for (int i = 0; i < clusters.Count; i++)
            {
                BuildClusterCracks(clusters[i], board, root.transform, plan.SourceMap.VisualKey, vertices, triangles, uvs, colors);
            }

            crackMesh = BuildMesh("RaidCollapseCrackMesh", vertices, triangles, uvs, colors);
            crackRenderer = CreateRenderer("CollapseCracks", crackMesh, visualSet.CollapseCrackMaterial, root.transform);
        }

        private void BuildAurora(RaidPhaseTransitionPlan plan, RaidBoard board)
        {
            List<Vector3> vertices = new List<Vector3>(1024);
            List<int> triangles = new List<int>(1536);
            List<Vector2> uvs = new List<Vector2>(1024);
            List<Color> colors = new List<Color>(1024);
            IReadOnlyList<RaidCollapseCluster> clusters = plan.Clusters;

            for (int i = 0; i < clusters.Count; i++)
            {
                BuildClusterAurora(clusters[i], board, root.transform, plan.SourceMap.VisualKey, vertices, triangles, uvs, colors);
            }

            auroraMesh = BuildMesh("RaidCollapseAuroraMesh", vertices, triangles, uvs, colors);
            auroraRenderer = CreateRenderer("CollapseAurora", auroraMesh, visualSet.CollapseBeamMaterial, root.transform);
        }

        private readonly struct FractureLine
        {
            private readonly Vector3[] points;

            public Vector3[] Points => points;
            public bool IsBranch { get; }
            public float AlphaScale { get; }
            public float RevealStart { get; }
            public float RevealSpan { get; }

            public FractureLine(Vector3[] points, bool isBranch, float alphaScale, float revealStart, float revealSpan)
            {
                this.points = points ?? throw new ArgumentNullException(nameof(points));
                IsBranch = isBranch;
                AlphaScale = alphaScale;
                RevealStart = Mathf.Clamp01(revealStart);
                RevealSpan = Mathf.Clamp(revealSpan, 0.01f, 1f - RevealStart);
            }
        }

        private static void BuildClusterCracks(RaidCollapseCluster cluster, RaidBoard board, Transform space, int visualKey, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors)
        {
            int seed = Hash(visualKey, cluster.Id, cluster.TileCount);
            List<FractureLine> lines = BuildFractureLines(cluster, board, space, seed);

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                FractureLine line = lines[lineIndex];
                Vector3[] points = line.Points;
                float totalLength = CalculateLineLength(points);
                float traversed = 0f;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    float segmentLength = Vector3.Distance(points[i], points[i + 1]);
                    float segmentStart = totalLength > 0.0001f ? traversed / totalLength : 0f;
                    float segmentEnd = totalLength > 0.0001f ? (traversed + segmentLength) / totalLength : 1f;
                    float revealStart = line.RevealStart + segmentStart * line.RevealSpan;
                    float revealEnd = line.RevealStart + segmentEnd * line.RevealSpan;
                    float widthMin = line.IsBranch ? 0.016f : 0.026f;
                    float widthMax = line.IsBranch ? 0.032f : 0.046f;
                    float width = board.TileSize * Mathf.Lerp(widthMin, widthMax, Mathf.Abs(Signed01(seed ^ lineIndex * 92821 ^ i * 48611)));
                    float alpha = line.AlphaScale * Mathf.Lerp(0.66f, 1f, Mathf.Abs(Signed01(seed ^ lineIndex * 21157 ^ i * 131071)));
                    AddRibbon(points[i], points[i + 1], width, alpha, revealStart, revealEnd, vertices, triangles, uvs, colors);
                    traversed += segmentLength;
                }
            }
        }

        private static void BuildClusterAurora(RaidCollapseCluster cluster, RaidBoard board, Transform space, int visualKey, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors)
        {
            int seed = Hash(visualKey, cluster.Id, cluster.TileCount);
            List<FractureLine> lines = BuildFractureLines(cluster, board, space, seed);

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                FractureLine line = lines[lineIndex];
                float alpha = line.AlphaScale * (line.IsBranch ? 0.44f : 0.62f);
                AddAuroraRibbon(line.Points, board, seed, lineIndex, line.IsBranch, alpha, vertices, triangles, uvs, colors);
            }
        }

        private static List<FractureLine> BuildFractureLines(RaidCollapseCluster cluster, RaidBoard board, Transform space, int seed)
        {
            List<FractureLine> lines = new List<FractureLine>(4);

            if (cluster.TileCount == 1)
            {
                Vector3 singleTileCenter = space.InverseTransformPoint(board.TileToWorld(cluster.Tiles[0], RaidDungeonMetrics.FloorTopHeight + RaidDungeonMetrics.SurfaceOverlayLift + 0.012f));
                lines.AddRange(CreateSingleTileFractures(singleTileCenter, board.TileSize, seed));
                return lines;
            }

            FindFarthestPair(cluster.Tiles, out Vector2Int startTile, out Vector2Int endTile);
            Vector3 start = JitteredTilePoint(board, space, startTile, seed, 0);
            Vector3 end = JitteredTilePoint(board, space, endTile, seed, 1);
            Vector3 clusterCenter = space.InverseTransformPoint(CalculateClusterWorldCenter(cluster, board) + Vector3.up * (RaidDungeonMetrics.FloorTopHeight + RaidDungeonMetrics.SurfaceOverlayLift + 0.012f));
            FractureLine main = CreateMainFracture(start, end, clusterCenter, board.TileSize, seed, cluster.TileCount);
            lines.Add(main);

            int branchCount = Mathf.Clamp(cluster.TileCount / 4, 1, 3);
            Vector3 mainDirection = (main.Points[main.Points.Length - 1] - main.Points[0]);
            mainDirection.y = 0f;
            if (mainDirection.sqrMagnitude < 0.0001f)
            {
                mainDirection = Vector3.right;
            }

            for (int branch = 0; branch < branchCount; branch++)
            {
                float anchorT = (branch + 1f) / (branchCount + 1f);
                Vector3 anchor = EvaluateFracture(main.Points, anchorT);
                lines.Add(CreateBranchFracture(anchor, mainDirection.normalized, board.TileSize, seed, branch));
            }

            return lines;
        }

        private static IEnumerable<FractureLine> CreateSingleTileFractures(Vector3 center, float tileSize, int seed)
        {
            List<FractureLine> lines = new List<FractureLine>(3);

            for (int i = 0; i < 3; i++)
            {
                float radians = (Mathf.Abs(Signed01(seed ^ i * 104729)) * 0.5f + 0.12f * i) * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
                float halfLength = tileSize * Mathf.Lerp(0.16f, 0.34f, Mathf.Abs(Signed01(seed ^ i * 130363 ^ 0x3C6EF372)));
                Vector3[] points =
                {
                    center - direction * halfLength,
                    center,
                    center + direction * (halfLength * 1.3f)
                };
                lines.Add(new FractureLine(points, i > 0, i == 0 ? 1f : 0.68f, i == 0 ? 0.02f : 0.24f + i * 0.08f, i == 0 ? 0.62f : 0.3f));
            }

            return lines;
        }

        private static FractureLine CreateMainFracture(Vector3 start, Vector3 end, Vector3 center, float tileSize, int seed, int tileCount)
        {
            int pointCount = Mathf.Clamp(tileCount + 2, 5, 9);
            Vector3[] points = new Vector3[pointCount];
            Vector3 direction = end - start;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.right;
            }

            direction.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, direction);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float centerBias = 1f - Mathf.Abs(t * 2f - 1f);
                Vector3 point = Vector3.Lerp(start, end, t);
                point = Vector3.Lerp(point, center, centerBias * 0.2f);
                float sideJitter = Signed01(seed ^ i * 374761393) * tileSize * Mathf.Lerp(0.1f, 0.24f, centerBias);
                float alongJitter = Signed01(seed ^ i * 668265263) * tileSize * 0.06f;
                point += side * sideJitter + direction * alongJitter;
                point.y = center.y;
                points[i] = point;
            }

            points[0] = start;
            points[pointCount - 1] = end;
            points[pointCount / 2] = Vector3.Lerp(points[pointCount / 2], center, 0.35f);
            return new FractureLine(points, false, 1f, 0.02f, 0.68f);
        }

        private static FractureLine CreateBranchFracture(Vector3 anchor, Vector3 mainDirection, float tileSize, int seed, int branchIndex)
        {
            Vector3 side = Vector3.Cross(Vector3.up, mainDirection).normalized;
            float sign = Signed01(seed ^ branchIndex * 59359 ^ 0x6C8E9CF5) >= 0f ? 1f : -1f;
            Vector3 branchDirection = (side * sign + mainDirection * Signed01(seed ^ branchIndex * 85991 ^ 0x254FF53A) * 0.28f).normalized;
            int pointCount = 4;
            Vector3[] points = new Vector3[pointCount];
            float length = tileSize * Mathf.Lerp(0.42f, 0.9f, Mathf.Abs(Signed01(seed ^ branchIndex * 65537 ^ 0x3BD39E10)));

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float bend = t * t;
                Vector3 point = anchor + branchDirection * length * t;
                point += side * sign * tileSize * 0.05f * bend;
                point += mainDirection * Signed01(seed ^ branchIndex * 1900813 ^ i * 92821) * tileSize * 0.035f;
                points[i] = point;
            }

            points[0] = anchor;
            return new FractureLine(points, true, 0.68f, 0.3f + branchIndex * 0.08f, 0.34f);
        }

        private static float CalculateLineLength(Vector3[] points)
        {
            if (points == null || points.Length < 2)
            {
                return 0f;
            }

            float length = 0f;

            for (int i = 0; i < points.Length - 1; i++)
            {
                length += Vector3.Distance(points[i], points[i + 1]);
            }

            return length;
        }

        private static Vector3 EvaluateFracture(Vector3[] points, float t)
        {
            if (points == null || points.Length == 0)
            {
                return Vector3.zero;
            }

            if (points.Length == 1)
            {
                return points[0];
            }

            float scaled = Mathf.Clamp01(t) * (points.Length - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, points.Length - 2);
            float localT = scaled - index;
            return Vector3.Lerp(points[index], points[index + 1], localT);
        }

        private static float CalculateAuroraHeight(RaidBoard board, int seed, int lineIndex, int pointIndex, float progress, bool branch)
        {
            float minimum = branch ? 0.36f : 0.58f;
            float maximum = branch ? 0.88f : 1.46f;
            float centerBoost = 1f - Mathf.Abs(progress * 2f - 1f);
            float randomness = Mathf.Abs(Signed01(seed ^ lineIndex * 486187739 ^ pointIndex * 25453 ^ 0x254FF53A));
            return board.TileSize * (Mathf.Lerp(minimum, maximum, randomness) + centerBoost * (branch ? 0.12f : 0.28f));
        }

        private static void AddAuroraRibbon(Vector3[] points, RaidBoard board, int seed, int lineIndex, bool branch, float alpha, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            AddAuroraRibbonLayer(points, board, seed, lineIndex, branch, 0, 0f, 1f, alpha, vertices, triangles, uvs, colors);
            AddAuroraRibbonLayer(points, board, seed, lineIndex, branch, 1, 0.075f, 0.76f, alpha * 0.58f, vertices, triangles, uvs, colors);
            AddAuroraRibbonLayer(points, board, seed, lineIndex, branch, 2, -0.065f, 0.64f, alpha * 0.46f, vertices, triangles, uvs, colors);
        }

        private static void AddAuroraRibbonLayer(Vector3[] points, RaidBoard board, int seed, int lineIndex, bool branch, int layerIndex, float lateralOffsetScale, float heightScale, float alpha, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors)
        {
            int start = vertices.Count;
            float cumulative = 0f;
            float invTileSize = 1f / Mathf.Max(0.001f, board.TileSize);

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 previous = points[Mathf.Max(0, i - 1)];
                Vector3 next = points[Mathf.Min(points.Length - 1, i + 1)];
                Vector3 tangent = next - previous;
                tangent.y = 0f;

                if (tangent.sqrMagnitude < 0.0001f)
                {
                    tangent = Vector3.right;
                }

                tangent.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, tangent);

                if (i > 0)
                {
                    cumulative += Vector3.Distance(points[i - 1], points[i]);
                }

                float progress = points.Length <= 1 ? 0f : i / (float)(points.Length - 1);
                float height = CalculateAuroraHeight(board, seed ^ layerIndex * 104729, lineIndex, i, progress, branch) * heightScale;
                float offsetNoise = Signed01(seed ^ lineIndex * 161803399 ^ layerIndex * 32452843 ^ i * 49999) * board.TileSize * 0.025f;
                float lateralOffset = board.TileSize * lateralOffsetScale + offsetNoise;
                Vector3 bottom = points[i] + side * lateralOffset;
                float topDrift = Signed01(seed ^ lineIndex * 486187739 ^ layerIndex * 15485863 ^ i * 65537) * board.TileSize * (branch ? 0.045f : 0.075f);
                Vector3 top = bottom + Vector3.up * height + side * topDrift;
                float u = cumulative * invTileSize * 0.72f;
                Color color = new Color(1f, 1f, 1f, alpha);
                vertices.Add(bottom);
                vertices.Add(top);
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
                colors.Add(color);
                colors.Add(color);
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                int a = start + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(b);
            }
        }

        private static Vector3 JitteredTilePoint(RaidBoard board, Transform space, Vector2Int tile, int seed, int index)
        {
            Vector3 point = board.TileToWorld(tile, RaidDungeonMetrics.FloorTopHeight + RaidDungeonMetrics.SurfaceOverlayLift + 0.01f);
            float jitterX = Signed01(seed ^ index * 374761393) * board.TileSize * 0.14f;
            float jitterZ = Signed01(seed ^ index * 668265263) * board.TileSize * 0.14f;
            point.x += jitterX;
            point.z += jitterZ;
            return space.InverseTransformPoint(point);
        }

        private static void AddRibbon(Vector3 a, Vector3 b, float width, float alpha, float revealStart, float revealEnd, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors)
        {
            Vector3 direction = b - a;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 side = Vector3.Cross(Vector3.up, direction.normalized) * width;
            int start = vertices.Count;
            vertices.Add(a - side);
            vertices.Add(a + side);
            vertices.Add(b + side);
            vertices.Add(b - side);
            uvs.Add(new Vector2(0f, revealStart));
            uvs.Add(new Vector2(1f, revealStart));
            uvs.Add(new Vector2(1f, revealEnd));
            uvs.Add(new Vector2(0f, revealEnd));
            Color color = new Color(1f, 1f, 1f, alpha);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Mesh BuildMesh(string name, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors)
        {
            Mesh mesh = new Mesh { name = name };

            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static MeshRenderer CreateRenderer(string name, Mesh mesh, Material material, Transform parent)
        {
            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            return AddRenderer(instance, mesh, material);
        }

        private static MeshRenderer AddRenderer(GameObject instance, Mesh mesh, Material material)
        {
            MeshFilter filter = instance.AddComponent<MeshFilter>();
            MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static void SetRendererIntensity(MeshRenderer renderer, MaterialPropertyBlock properties, float intensity)
        {
            if (renderer == null)
            {
                return;
            }

            properties.SetFloat(IntensityId, Mathf.Max(0f, intensity));
            renderer.SetPropertyBlock(properties);
        }

        private static void SetRendererReveal(MeshRenderer renderer, MaterialPropertyBlock properties, float progress)
        {
            if (renderer == null)
            {
                return;
            }

            properties.SetFloat(RevealProgressId, Mathf.Clamp01(progress));
            renderer.SetPropertyBlock(properties);
        }

        private static int[] BuildFallOrder(IReadOnlyList<RaidCollapseCluster> clusters)
        {
            int[] order = new int[clusters.Count];

            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            for (int i = 1; i < order.Length; i++)
            {
                int value = order[i];
                int j = i - 1;

                while (j >= 0 && CompareFallOrder(clusters[value], clusters[order[j]]) < 0)
                {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = value;
            }

            return order;
        }

        private static int CompareFallOrder(RaidCollapseCluster a, RaidCollapseCluster b)
        {
            if (a.TileCount != b.TileCount)
            {
                return a.TileCount.CompareTo(b.TileCount);
            }

            if (!Mathf.Approximately(a.Center.y, b.Center.y))
            {
                return b.Center.y.CompareTo(a.Center.y);
            }

            return a.Center.x.CompareTo(b.Center.x);
        }

        private static Vector3 CalculateClusterWorldCenter(RaidCollapseCluster cluster, RaidBoard board)
        {
            Vector3 sum = Vector3.zero;

            for (int i = 0; i < cluster.TileCount; i++)
            {
                sum += board.TileToWorld(cluster.Tiles[i]);
            }

            return cluster.TileCount > 0 ? sum / cluster.TileCount : Vector3.zero;
        }

        private static void FindFarthestPair(IReadOnlyList<Vector2Int> tiles, out Vector2Int a, out Vector2Int b)
        {
            a = tiles[0];
            b = tiles[tiles.Count - 1];
            int best = -1;

            for (int i = 0; i < tiles.Count; i++)
            {
                for (int j = i + 1; j < tiles.Count; j++)
                {
                    Vector2Int delta = tiles[j] - tiles[i];
                    int distance = delta.sqrMagnitude;

                    if (distance <= best)
                    {
                        continue;
                    }

                    best = distance;
                    a = tiles[i];
                    b = tiles[j];
                }
            }
        }

        private static int Hash(int a, int b, int c)
        {
            unchecked
            {
                int value = a;
                value = value * 397 ^ b;
                value = value * 397 ^ c;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value;
            }
        }

        private static float Signed01(int value)
        {
            unchecked
            {
                uint bits = (uint)value;
                bits ^= bits << 13;
                bits ^= bits >> 17;
                bits ^= bits << 5;
                return bits / (float)uint.MaxValue * 2f - 1f;
            }
        }

        private sealed class ClusterVisual
        {
            private readonly Transform root;
            private readonly Vector3 basePosition;
            private readonly Vector3 drift;
            private readonly Vector3 tiltAxis;
            private readonly float preTilt;
            private readonly float finalTilt;
            private readonly float fallStart;
            private readonly float fallDuration;
            private readonly float fallDistance;
            private readonly float shakePhase;

            public ClusterVisual(Transform root, Vector3 basePosition, Vector3 drift, Vector3 tiltAxis, float preTilt, float finalTilt, float fallStart, float fallDuration, float fallDistance, int seed)
            {
                this.root = root;
                this.basePosition = basePosition;
                this.drift = drift;
                this.tiltAxis = tiltAxis;
                this.preTilt = preTilt;
                this.finalTilt = finalTilt;
                this.fallStart = fallStart;
                this.fallDuration = fallDuration;
                this.fallDistance = fallDistance;
                shakePhase = Mathf.Abs(Signed01(seed ^ 0x5BE0CD19)) * Mathf.PI * 2f;
            }

            public void Update(float elapsed)
            {
                if (root == null)
                {
                    return;
                }

                float tiltStart = fallStart - 0.34f;
                float tilt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(tiltStart, fallStart, elapsed));
                float fall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fallStart, fallStart + fallDuration, elapsed));
                float fallCurve = fall * fall;
                float preShake = elapsed < fallStart ? Mathf.Sin(elapsed * 39f + shakePhase) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, fallStart, elapsed)) * 0.035f : 0f;
                Vector3 position = basePosition + new Vector3(preShake, 0f, -preShake * 0.65f);
                position += drift * fall * 0.9f;
                position.y -= fallDistance * fallCurve;
                root.position = position;
                float angle = preTilt * tilt + finalTilt * Mathf.Pow(fall, 1.25f);
                float yaw = Signed01((int)(shakePhase * 100000f)) * 8f * fall;
                root.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.AngleAxis(angle, tiltAxis);
            }
        }
    }
}
