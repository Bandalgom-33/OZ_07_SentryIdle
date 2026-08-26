using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidCrackScarView
    {
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int GrooveAlphaId = Shader.PropertyToID("_GrooveAlpha");
        private static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private readonly RaidBoardView boardView;
        private readonly RaidTileVisualSetSO visualSet;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private GameObject root;
        private Mesh mesh;
        private MeshRenderer renderer;
        private bool committed;
        private float visualScale = 1f;

        public RaidCrackScarView(RaidBoardRuntime boardRuntime)
        {
            if (boardRuntime == null)
            {
                throw new ArgumentNullException(nameof(boardRuntime));
            }

            boardView = boardRuntime.BoardView != null ? boardRuntime.BoardView : throw new InvalidOperationException("Raid Board View가 없습니다.");
            visualSet = boardView.VisualSet != null ? boardView.VisualSet : throw new InvalidOperationException("Raid Tile Visual Set이 없습니다.");
        }

        public void Begin(RaidPhaseTransitionPlan plan, RaidBoard board, float effectScale)
        {
            Dispose();
            visualScale = Mathf.Clamp(effectScale, 0.5f, 2f);

            if (plan == null || board == null || visualSet.CollapseScarMaterial == null)
            {
                return;
            }

            List<Vector3> vertices = new List<Vector3>(512);
            List<int> triangles = new List<int>(768);
            List<Vector2> uvs = new List<Vector2>(512);
            List<Vector2> detailUvs = new List<Vector2>(512);
            List<Color> colors = new List<Color>(512);
            BuildScarMesh(plan, board, boardView.transform, vertices, triangles, uvs, detailUvs, colors);

            if (vertices.Count == 0 || triangles.Count == 0)
            {
                return;
            }

            mesh = new Mesh { name = $"RaidCrackScar_{plan.FromPhase}_{plan.ToPhase}" };
            mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, detailUvs);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();

            root = new GameObject($"RaidCrackScar_{plan.FromPhase}_{plan.ToPhase}");
            root.layer = boardView.gameObject.layer;
            root.transform.SetParent(boardView.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = visualSet.CollapseScarMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 50;

            SetVisual(0f, 0.42f * visualScale, 1f);
            committed = false;
        }

        public void Update(float elapsed)
        {
            if (renderer == null || committed)
            {
                return;
            }

            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 1.62f, elapsed));
            float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.25f, 2.35f, elapsed));
            float glow = Mathf.Lerp(0.42f, 0.18f, settle) * visualScale;
            SetVisual(reveal, glow, 1f);
        }

        public void Commit()
        {
            if (root == null || renderer == null || mesh == null)
            {
                return;
            }

            committed = true;
            SetVisual(1f, 0.16f * visualScale, 1f);
            boardView.RegisterPersistentEffect(root, mesh);
            root = null;
            mesh = null;
            renderer = null;
        }

        public void ClearPersistent()
        {
            boardView.ClearPersistentEffects();
        }

        public void Dispose()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }

            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }

            root = null;
            mesh = null;
            renderer = null;
            committed = false;
            visualScale = 1f;
        }

        private void SetVisual(float reveal, float glow, float groove)
        {
            if (renderer == null)
            {
                return;
            }

            properties.SetFloat(RevealProgressId, Mathf.Clamp01(reveal));
            properties.SetFloat(GlowStrengthId, Mathf.Clamp01(glow));
            properties.SetFloat(GrooveAlphaId, Mathf.Clamp01(groove));
            renderer.SetPropertyBlock(properties);
        }

        private static void BuildScarMesh(RaidPhaseTransitionPlan plan, RaidBoard board, Transform space, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector2> detailUvs, List<Color> colors)
        {
            IReadOnlyList<RaidCollapseCluster> clusters = plan.Clusters;
            Vector2 impact = CalculateImpactGrid(plan, board);
            float maxDistance = Mathf.Max(1f, Mathf.Sqrt(board.Width * board.Width + board.Height * board.Height));

            for (int clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
            {
                RaidCollapseCluster cluster = clusters[clusterIndex];

                if (cluster.TileCount < 3)
                {
                    continue;
                }

                List<BoundaryCandidate> candidates = CollectCandidates(plan, board, cluster);

                if (candidates.Count == 0)
                {
                    continue;
                }

                SortByImpactDistance(candidates, impact);
                int scarCount = cluster.TileCount >= 12 && candidates.Count >= 4 ? 2 : 1;

                for (int i = 0; i < scarCount; i++)
                {
                    int candidateIndex = i == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt((candidates.Count - 1) * 0.62f), 1, candidates.Count - 1);
                    BoundaryCandidate candidate = candidates[candidateIndex];
                    int seed = Hash(plan.SourceMap.VisualKey, cluster.Id * 47 + i * 13, candidate.Survivor.x + candidate.Survivor.y * board.Width);
                    float distance01 = Mathf.Clamp01(Vector2.Distance(candidate.Survivor, impact) / maxDistance);
                    float revealStart = Mathf.Clamp01(0.06f + distance01 * 0.42f + i * 0.05f);
                    float revealSpan = Mathf.Lerp(0.40f, 0.28f, distance01);
                    bool addBranch = cluster.TileCount >= 5;
                    AddMainScar(candidate, plan, board, space, seed, revealStart, revealSpan, addBranch, vertices, triangles, uvs, detailUvs, colors);
                }
            }
        }

        private static List<BoundaryCandidate> CollectCandidates(RaidPhaseTransitionPlan plan, RaidBoard board, RaidCollapseCluster cluster)
        {
            List<BoundaryCandidate> result = new List<BoundaryCandidate>(16);
            bool[] seen = new bool[board.Count];

            for (int i = 0; i < cluster.Tiles.Count; i++)
            {
                Vector2Int collapsed = cluster.Tiles[i];

                for (int d = 0; d < Directions.Length; d++)
                {
                    Vector2Int survivor = collapsed + Directions[d];

                    if (!board.IsInside(survivor) || plan.IsCollapsing(survivor))
                    {
                        continue;
                    }

                    int index = survivor.y * board.Width + survivor.x;

                    if (seen[index])
                    {
                        continue;
                    }

                    RaidTile targetTile = plan.TargetMap.GetTile(index);

                    if (targetTile.Surface == RaidTileSurface.Void)
                    {
                        continue;
                    }

                    seen[index] = true;
                    result.Add(new BoundaryCandidate(survivor, collapsed));
                }
            }

            return result;
        }

        private static void SortByImpactDistance(List<BoundaryCandidate> candidates, Vector2 impact)
        {
            for (int i = 1; i < candidates.Count; i++)
            {
                BoundaryCandidate value = candidates[i];
                float valueDistance = ((Vector2)value.Survivor - impact).sqrMagnitude;
                int j = i - 1;

                while (j >= 0)
                {
                    float currentDistance = ((Vector2)candidates[j].Survivor - impact).sqrMagnitude;

                    if (currentDistance <= valueDistance)
                    {
                        break;
                    }

                    candidates[j + 1] = candidates[j];
                    j--;
                }

                candidates[j + 1] = value;
            }
        }

        private static void AddMainScar(BoundaryCandidate candidate, RaidPhaseTransitionPlan plan, RaidBoard board, Transform space, int seed, float revealStart, float revealSpan, bool addBranch, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector2> detailUvs, List<Color> colors)
        {
            RaidTile targetTile = plan.TargetMap.GetTile(candidate.Survivor.y * board.Width + candidate.Survivor.x);
            float surfaceHeight = targetTile.IsBridge ? RaidDungeonMetrics.BridgeTopHeight : RaidDungeonMetrics.FloorTopHeight;
            float overlayHeight = surfaceHeight + RaidDungeonMetrics.SurfaceOverlayLift + 0.055f;
            Vector3 survivorWorld = board.TileToWorld(candidate.Survivor, overlayHeight);
            Vector3 collapsedWorld = board.TileToWorld(candidate.Collapsed, overlayHeight);
            Vector3 inward = survivorWorld - collapsedWorld;
            inward.y = 0f;

            if (inward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            inward.Normalize();
            Vector3 boundary = Vector3.Lerp(collapsedWorld, survivorWorld, 0.56f) + inward * board.TileSize * 0.06f;
            float length = board.TileSize * Mathf.Lerp(1.08f, 1.50f, Mathf.Abs(Signed01(seed ^ 0x3C6EF372)));
            Vector3[] mainPoints = BuildLightningPoints(boundary, inward, length, board.TileSize, seed, 8, 24f, space);
            AddRibbonLine(mainPoints, board.TileSize, seed, revealStart, revealSpan, false, 1f, vertices, triangles, uvs, detailUvs, colors);

            if (!addBranch || mainPoints.Length < 5)
            {
                return;
            }

            int branchPoint = Mathf.Clamp(2 + Mathf.Abs(seed % 3), 2, mainPoints.Length - 3);
            Vector3 branchOriginWorld = space.TransformPoint(mainPoints[branchPoint]);
            Vector3 mainDirectionWorld = space.TransformPoint(mainPoints[branchPoint + 1]) - branchOriginWorld;
            mainDirectionWorld.y = 0f;

            if (mainDirectionWorld.sqrMagnitude < 0.0001f)
            {
                mainDirectionWorld = inward;
            }
            else
            {
                mainDirectionWorld.Normalize();
            }

            float branchSign = Signed01(seed ^ 0x27D4EB2D) >= 0f ? 1f : -1f;
            float branchAngle = Mathf.Lerp(42f, 66f, Mathf.Abs(Signed01(seed ^ 0x165667B1))) * branchSign;
            Vector3 branchDirection = Quaternion.AngleAxis(branchAngle, Vector3.up) * mainDirectionWorld;
            float branchLength = length * Mathf.Lerp(0.32f, 0.48f, Mathf.Abs(Signed01(seed ^ unchecked((int)0x85EBCA6B))));
            Vector3[] branchPoints = BuildLightningPoints(branchOriginWorld, branchDirection, branchLength, board.TileSize, seed ^ 0x5F356495, 5, 30f, space);
            float branchStart = Mathf.Clamp01(revealStart + revealSpan * (branchPoint / (float)(mainPoints.Length - 1)));
            float branchSpan = Mathf.Min(revealSpan * 0.34f, 0.96f - branchStart);

            if (branchSpan > 0.03f)
            {
                AddRibbonLine(branchPoints, board.TileSize, seed ^ 0x5F356495, branchStart, branchSpan, true, 0.78f, vertices, triangles, uvs, detailUvs, colors);
            }
        }

        private static Vector3[] BuildLightningPoints(Vector3 startWorld, Vector3 forwardWorld, float totalLength, float tileSize, int seed, int pointCount, float maxTurnDegrees, Transform space)
        {
            Vector3[] points = new Vector3[pointCount];
            Vector3 current = startWorld;
            Vector3 baseForward = forwardWorld.normalized;
            Vector3 heading = baseForward;
            points[0] = space.InverseTransformPoint(current);
            float baseSegment = totalLength / Mathf.Max(1, pointCount - 1);
            float turnSign = Signed01(seed ^ unchecked((int)0x9E3779B9)) >= 0f ? 1f : -1f;

            for (int i = 1; i < pointCount; i++)
            {
                float raw = Signed01(seed ^ (i * 1103515245 + 12345));
                float turn = Mathf.Lerp(9f, maxTurnDegrees, Mathf.Abs(raw)) * (raw >= 0f ? 1f : -1f) * turnSign;

                if ((i & 1) == 0)
                {
                    turn *= -0.72f;
                }

                Vector3 turned = Quaternion.AngleAxis(turn, Vector3.up) * heading;
                heading = Vector3.Slerp(turned.normalized, baseForward, 0.34f).normalized;
                float segmentScale = Mathf.Lerp(0.78f, 1.18f, Mathf.Abs(Signed01(seed ^ (i * 374761393))));
                current += heading * (baseSegment * segmentScale);
                current.y = startWorld.y;
                points[i] = space.InverseTransformPoint(current);
            }

            return points;
        }

        private static void AddRibbonLine(Vector3[] points, float tileSize, int seed, float revealStart, float revealSpan, bool branch, float alpha, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector2> detailUvs, List<Color> colors)
        {
            float totalLength = 0f;

            for (int i = 0; i < points.Length - 1; i++)
            {
                totalLength += Vector3.Distance(points[i], points[i + 1]);
            }

            float traversed = 0f;

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float segmentLength = Vector3.Distance(a, b);
                float start01 = totalLength > 0.0001f ? traversed / totalLength : 0f;
                float end01 = totalLength > 0.0001f ? (traversed + segmentLength) / totalLength : 1f;
                float progressStart = Mathf.Clamp01(revealStart + start01 * revealSpan);
                float progressEnd = Mathf.Clamp01(revealStart + end01 * revealSpan);
                float taper = Mathf.Lerp(1f, branch ? 0.52f : 0.62f, start01);
                float minWidth = branch ? 0.055f : 0.105f;
                float maxWidth = branch ? 0.085f : 0.155f;
                float randomWidth = Mathf.Lerp(minWidth, maxWidth, Mathf.Abs(Signed01(seed ^ ((i + 1) * 92821))));
                float width = tileSize * randomWidth * taper;
                AddRibbon(a, b, width, alpha, progressStart, progressEnd, start01, end01, vertices, triangles, uvs, detailUvs, colors);
                traversed += segmentLength;
            }
        }

        private static void AddRibbon(Vector3 a, Vector3 b, float width, float alpha, float revealStart, float revealEnd, float detailStart, float detailEnd, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector2> detailUvs, List<Color> colors)
        {
            Vector3 direction = b - a;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.000001f)
            {
                return;
            }

            direction.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);
            int start = vertices.Count;
            vertices.Add(a - side);
            vertices.Add(a + side);
            vertices.Add(b + side);
            vertices.Add(b - side);
            uvs.Add(new Vector2(0f, revealStart));
            uvs.Add(new Vector2(1f, revealStart));
            uvs.Add(new Vector2(1f, revealEnd));
            uvs.Add(new Vector2(0f, revealEnd));
            detailUvs.Add(new Vector2(0f, detailStart));
            detailUvs.Add(new Vector2(1f, detailStart));
            detailUvs.Add(new Vector2(1f, detailEnd));
            detailUvs.Add(new Vector2(0f, detailEnd));
            Color color = new Color(1f, 1f, 1f, alpha);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            triangles.Add(start + 0);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 0);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Vector2 CalculateImpactGrid(RaidPhaseTransitionPlan plan, RaidBoard board)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);

                    if (!plan.IsCollapsing(coordinate))
                    {
                        continue;
                    }

                    sum += coordinate;
                    count++;
                }
            }

            Vector2 center = count > 0 ? sum / count : new Vector2(board.Width * 0.5f, board.Height * 0.5f);
            Vector2 bossSide = new Vector2(board.Width * 0.5f, board.Height - 1f);
            return Vector2.Lerp(center, bossSide, 0.58f);
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

        private readonly struct BoundaryCandidate
        {
            public Vector2Int Survivor { get; }
            public Vector2Int Collapsed { get; }

            public BoundaryCandidate(Vector2Int survivor, Vector2Int collapsed)
            {
                Survivor = survivor;
                Collapsed = collapsed;
            }
        }
    }
}
