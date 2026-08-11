using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UnitDataLink : MonoBehaviour
    {
        [Header("캐릭터 데이터 연결")]
        [Tooltip("이 캐릭터 프리팹의 원본 정적 데이터를 보관하는 UnitDataSO입니다.")]
        [SerializeField] private UnitDataSO unitData;

        public UnitDataSO UnitData => unitData;
        public string UnitId => unitData == null ? string.Empty : unitData.UnitId;
        public bool HasData => unitData != null;
    }
}