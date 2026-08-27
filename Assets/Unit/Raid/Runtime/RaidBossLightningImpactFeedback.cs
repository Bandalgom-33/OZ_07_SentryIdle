using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    internal sealed class RaidBossLightningImpactFeedback : MonoBehaviour
    {
        private const float MaximumShakeEnergy = 0.14f;
        private const float ShakeDecayPerSecond = 0.62f;
        private const float MaximumFlashAlpha = 0.12f;
        private const float FlashDecayPerSecond = 1.65f;
        private const float PositionShakeScale = 1f;
        private const float RotationShakeScale = 4.2f;

        private Camera targetCamera;
        private Transform cameraTransform;
        private Image flashImage;
        private Vector3 appliedPositionOffset;
        private Quaternion appliedRotationOffset = Quaternion.identity;
        private Vector3 lastBasePosition;
        private Quaternion lastBaseRotation = Quaternion.identity;
        private bool cameraOffsetApplied;
        private float shakeEnergy;
        private float flashAlpha;
        private float shakeClock;

        public static RaidBossLightningImpactFeedback EnsureInstalled(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            RaidBossLightningImpactFeedback feedback = host.GetComponent<RaidBossLightningImpactFeedback>();
            if (feedback == null)
            {
                feedback = host.AddComponent<RaidBossLightningImpactFeedback>();
            }

            return feedback;
        }

        private void Awake()
        {
            ResolveCamera();
            EnsureFlashOverlay();
            enabled = false;
        }

        private void LateUpdate()
        {
            float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            RemoveAppliedCameraOffset();

            shakeClock += deltaTime;
            shakeEnergy = Mathf.MoveTowards(shakeEnergy, 0f, ShakeDecayPerSecond * deltaTime);
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, FlashDecayPerSecond * deltaTime);

            ApplyCameraShake();
            ApplyFlash();

            if (shakeEnergy <= 0.0001f && flashAlpha <= 0.0001f)
            {
                RemoveAppliedCameraOffset();
                ApplyFlash(0f);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            RemoveAppliedCameraOffset();
            ApplyFlash(0f);
        }

        private void OnDestroy()
        {
            RemoveAppliedCameraOffset();
        }

        public void PlayMapImpact(int strikeIndex, int strikeCount)
        {
            float energy = strikeIndex == 0 || strikeIndex == strikeCount - 1 ? 0.034f : 0.024f;
            AddImpact(energy, strikeIndex == 0 || strikeIndex == strikeCount - 1 ? 0.024f : 0.012f);
        }

        public void PlayTargetImpact(int strikeIndex, int strikeCount)
        {
            bool peakStrike = strikeIndex == 0 || strikeIndex == strikeCount - 1;
            AddImpact(peakStrike ? 0.11f : 0.085f, peakStrike ? 0.105f : 0.075f);
        }

        public void StopImmediate()
        {
            shakeEnergy = 0f;
            flashAlpha = 0f;
            RemoveAppliedCameraOffset();
            ApplyFlash(0f);
            enabled = false;
        }

        private void AddImpact(float shake, float flash)
        {
            ResolveCamera();
            EnsureFlashOverlay();

            float safeShake = Mathf.Clamp(shake, 0f, MaximumShakeEnergy);
            float safeFlash = Mathf.Clamp(flash, 0f, MaximumFlashAlpha);
            shakeEnergy = Mathf.Clamp(Mathf.Max(shakeEnergy, safeShake) + safeShake * 0.18f, 0f, MaximumShakeEnergy);
            flashAlpha = Mathf.Clamp(Mathf.Max(flashAlpha, safeFlash), 0f, MaximumFlashAlpha);
            enabled = true;
        }

        private void ResolveCamera()
        {
            if (targetCamera != null && cameraTransform != null)
            {
                return;
            }

            targetCamera = Camera.main;
            cameraTransform = targetCamera != null ? targetCamera.transform : null;
        }

        private void ApplyCameraShake()
        {
            if (cameraTransform == null || shakeEnergy <= 0f)
            {
                return;
            }

            float energy = shakeEnergy * shakeEnergy / MaximumShakeEnergy;
            float offsetX = Mathf.Sin(shakeClock * 47.3f + 0.61f) * energy * PositionShakeScale;
            float offsetY = Mathf.Sin(shakeClock * 39.1f + 2.17f) * energy * PositionShakeScale * 0.58f;
            float roll = Mathf.Sin(shakeClock * 43.7f + 1.33f) * energy * RotationShakeScale;
            float pitch = Mathf.Sin(shakeClock * 35.9f + 2.71f) * energy * RotationShakeScale * 0.54f;

            appliedPositionOffset = new Vector3(offsetX, offsetY, 0f);
            appliedRotationOffset = Quaternion.Euler(pitch, 0f, roll);
            lastBasePosition = cameraTransform.localPosition;
            lastBaseRotation = cameraTransform.localRotation;
            cameraTransform.localPosition = lastBasePosition + appliedPositionOffset;
            cameraTransform.localRotation = lastBaseRotation * appliedRotationOffset;
            cameraOffsetApplied = true;
        }

        private void RemoveAppliedCameraOffset()
        {
            if (cameraTransform != null && cameraOffsetApplied)
            {
                Vector3 expectedPosition = lastBasePosition + appliedPositionOffset;
                Quaternion expectedRotation = lastBaseRotation * appliedRotationOffset;
                if ((cameraTransform.localPosition - expectedPosition).sqrMagnitude <= 0.0004f)
                {
                    cameraTransform.localPosition = lastBasePosition;
                }

                if (Quaternion.Angle(cameraTransform.localRotation, expectedRotation) <= 0.25f)
                {
                    cameraTransform.localRotation = lastBaseRotation;
                }
            }

            appliedPositionOffset = Vector3.zero;
            appliedRotationOffset = Quaternion.identity;
            cameraOffsetApplied = false;
        }

        private void EnsureFlashOverlay()
        {
            if (flashImage != null)
            {
                return;
            }

            Transform existing = transform.Find("LightningScreenFlash");
            if (existing != null)
            {
                flashImage = existing.GetComponentInChildren<Image>(true);
                if (flashImage != null)
                {
                    flashImage.raycastTarget = false;
                    ApplyFlash(0f);
                    return;
                }
            }

            GameObject canvasObject = new GameObject("LightningScreenFlash", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

            GameObject imageObject = new GameObject("Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            flashImage = imageObject.GetComponent<Image>();
            flashImage.raycastTarget = false;
            ApplyFlash(0f);
        }

        private void ApplyFlash()
        {
            ApplyFlash(flashAlpha);
        }

        private void ApplyFlash(float alpha)
        {
            if (flashImage == null)
            {
                return;
            }

            float safeAlpha = Mathf.Clamp01(alpha);
            flashImage.color = new Color(0.82f, 0.9f, 1f, safeAlpha);
            flashImage.enabled = safeAlpha > 0.0001f;
        }
    }
}
