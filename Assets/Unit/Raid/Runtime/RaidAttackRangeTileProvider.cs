using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidAttackRangeTileProvider : MonoBehaviour, IAttackRangeTileProvider
    {
        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            boardRuntime = GetComponent<RaidBoardRuntime>();
        }

        private void OnEnable()
        {
            if (boardRuntime == null)
            {
                boardRuntime = GetComponent<RaidBoardRuntime>();
            }

            AttackRangeTileService.Register(this);
        }

        private void OnDisable()
        {
            AttackRangeTileService.Unregister(this);
        }

        public bool TryGetAttackRangeTile(Vector2Int tileCoordinate, out AttackRangeTile tile)
        {
            tile = default;

            RaidBoard board = boardRuntime != null ? boardRuntime.Board : null;
            if (board == null || !board.TryGetTile(tileCoordinate, out RaidTile raidTile) || raidTile.Surface == RaidTileSurface.Void)
            {
                return false;
            }

            float height = GetSurfaceHeight(raidTile);
            Vector3 worldPosition = board.TileToWorld(tileCoordinate, height + 0.025f);
            Vector3 scale = new Vector3(board.TileSize * 0.9f, 0.035f, board.TileSize * 0.9f);
            tile = new AttackRangeTile(worldPosition, scale);
            return true;
        }

        private float GetSurfaceHeight(RaidTile tile)
        {
            if (battle == null || battle.Config == null)
            {
                return tile.Deploy == RaidTileDeploy.HighGround ? 0.82f : 0.08f;
            }

            return tile.Deploy == RaidTileDeploy.HighGround ? battle.Config.HighGroundDeployHeight : battle.Config.GroundDeployHeight;
        }
    }
}
