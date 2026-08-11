using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatEntityAnchors : MonoBehaviour
    {
        [Header("공통 프리팹 기준점")]
        [Tooltip("캐릭터 또는 몬스터의 모델, 스프라이트와 임시 캡슐 비주얼을 배치하는 부모 Transform입니다.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("투사체와 기본 공격 효과가 시작되는 기준점입니다.")]
        [SerializeField] private Transform attackPoint;

        [Tooltip("피격, 버프, 사망 등의 전투 효과를 표시하는 기준점입니다.")]
        [SerializeField] private Transform effectPoint;

        [Tooltip("캐릭터 HP와 스킬게이지처럼 개체 위에 표시되는 UI의 기준점입니다.")]
        [SerializeField] private Transform uiAnchor;

        public Transform VisualRoot => visualRoot;
        public Transform AttackPoint => attackPoint;
        public Transform EffectPoint => effectPoint;
        public Transform UIAnchor => uiAnchor;
        public bool IsComplete => visualRoot != null && attackPoint != null && effectPoint != null && uiAnchor != null;
    }
}