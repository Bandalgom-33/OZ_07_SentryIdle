using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatEntityAnchors), typeof(CombatGridPosition))]
    public sealed class UnitFacingView : MonoBehaviour
    {
        private CombatEntityAnchors anchors;
        private CombatGridPosition gridPosition;
        private Transform visualRoot;
        private Transform attackPoint;
        private Transform effectPoint;
        private Vector3 baseVisualPosition;
        private Vector3 baseAttackPosition;
        private Vector3 baseEffectPosition;
        private Quaternion baseVisualRotation = Quaternion.identity;
        private Quaternion baseAttackRotation = Quaternion.identity;
        private Quaternion baseEffectRotation = Quaternion.identity;
        private bool basePoseCached;
        private bool subscribed;

        private void Awake()
        {
            ResolveReferences();
            CacheBasePose();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheBasePose();
            Subscribe();

            if (gridPosition != null && gridPosition.IsInitialized)
            {
                ApplyFacing(gridPosition.FacingDirection);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (anchors == null)
            {
                anchors = GetComponent<CombatEntityAnchors>();
            }

            if (gridPosition == null)
            {
                gridPosition = GetComponent<CombatGridPosition>();
            }

            if (anchors == null)
            {
                return;
            }

            visualRoot = visualRoot != null ? visualRoot : anchors.VisualRoot;
            attackPoint = attackPoint != null ? attackPoint : anchors.AttackPoint;
            effectPoint = effectPoint != null ? effectPoint : anchors.EffectPoint;
        }

        private void CacheBasePose()
        {
            if (basePoseCached || visualRoot == null)
            {
                return;
            }

            baseVisualPosition = visualRoot.localPosition;
            baseVisualRotation = visualRoot.localRotation;

            if (attackPoint != null && !attackPoint.IsChildOf(visualRoot))
            {
                baseAttackPosition = attackPoint.localPosition;
                baseAttackRotation = attackPoint.localRotation;
            }

            if (effectPoint != null && !effectPoint.IsChildOf(visualRoot))
            {
                baseEffectPosition = effectPoint.localPosition;
                baseEffectRotation = effectPoint.localRotation;
            }

            basePoseCached = true;
        }

        private void Subscribe()
        {
            if (subscribed || gridPosition == null)
            {
                return;
            }

            gridPosition.OnFacingChanged += HandleFacingChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (gridPosition != null)
            {
                gridPosition.OnFacingChanged -= HandleFacingChanged;
            }

            subscribed = false;
        }

        private void HandleFacingChanged(CombatGridPosition changedPosition)
        {
            if (changedPosition == gridPosition)
            {
                ApplyFacing(changedPosition.FacingDirection);
            }
        }

        private void ApplyFacing(GridFacingDirection facing)
        {
            if (!basePoseCached)
            {
                ResolveReferences();
                CacheBasePose();

                if (!basePoseCached)
                {
                    return;
                }
            }

            Quaternion facingRotation = Quaternion.Euler(0f, FacingYaw(facing), 0f);
            ApplyAnchor(visualRoot, baseVisualPosition, baseVisualRotation, facingRotation);

            if (attackPoint != null && !attackPoint.IsChildOf(visualRoot))
            {
                ApplyAnchor(attackPoint, baseAttackPosition, baseAttackRotation, facingRotation);
            }

            if (effectPoint != null && !effectPoint.IsChildOf(visualRoot))
            {
                ApplyAnchor(effectPoint, baseEffectPosition, baseEffectRotation, facingRotation);
            }
        }

        private static void ApplyAnchor(Transform anchor, Vector3 basePosition, Quaternion baseRotation, Quaternion facingRotation)
        {
            if (anchor == null)
            {
                return;
            }

            anchor.localPosition = facingRotation * basePosition;
            anchor.localRotation = facingRotation * baseRotation;
        }

        private static float FacingYaw(GridFacingDirection facing)
        {
            switch (facing)
            {
                case GridFacingDirection.East:
                    return 90f;
                case GridFacingDirection.South:
                    return 180f;
                case GridFacingDirection.West:
                    return 270f;
                default:
                    return 0f;
            }
        }
    }
}
