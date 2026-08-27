using UnityEngine;
using UnityEngine.EventSystems;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidAttackHoldInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RaidBattleController battle;

        public void Bind(RaidBattleController controller)
        {
            battle = controller;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (battle == null || battle.Mode != RaidBattleMode.Manual)
            {
                return;
            }

            battle.BeginManualRaidAttack();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            battle?.EndManualRaidAttack();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerPress == gameObject)
            {
                battle?.EndManualRaidAttack();
            }
        }

        private void OnDisable()
        {
            battle?.EndManualRaidAttack();
        }
    }
}
