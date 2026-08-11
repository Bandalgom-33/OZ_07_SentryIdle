using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OZ.SentryIdle.UI.Samples
{
    // 비활성 페이지까지 카드 선택을 하나만 유지함
    public sealed class UICharacterSampleSelectionGroup : MonoBehaviour
    {
        // Toggle과 해제할 콜백을 함께 저장함
        private readonly List<ToggleBinding> bindings = new List<ToggleBinding>();

        // 선택 해제 중 중복 처리를 막음
        private bool isUpdatingSelection;

        private void OnEnable()
        {
            BindToggles();
            KeepSingleSelection();
        }

        private void OnDisable()
        {
            UnbindToggles();
        }

        private void BindToggles()
        {
            // 꺼진 페이지의 카드도 함께 연결함
            UnbindToggles();
            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (Toggle toggle in toggles)
            {
                if (toggle == null)
                {
                    continue;
                }

                Toggle currentToggle = toggle;
                UnityAction<bool> callback = isOn => HandleSelectionChanged(currentToggle, isOn);
                currentToggle.onValueChanged.AddListener(callback);
                bindings.Add(new ToggleBinding(currentToggle, callback));
            }
        }

        private void UnbindToggles()
        {
            foreach (ToggleBinding binding in bindings)
            {
                if (binding.Toggle != null)
                {
                    binding.Toggle.onValueChanged.RemoveListener(binding.Callback);
                }
            }

            bindings.Clear();
        }

        private void KeepSingleSelection()
        {
            Toggle selectedToggle = null;
            foreach (ToggleBinding binding in bindings)
            {
                Toggle toggle = binding.Toggle;
                if (toggle == null || !toggle.isOn)
                {
                    continue;
                }

                if (selectedToggle == null)
                {
                    selectedToggle = toggle;
                    continue;
                }

                toggle.isOn = false;
            }
        }

        private void HandleSelectionChanged(Toggle selectedToggle, bool isOn)
        {
            if (!isOn || isUpdatingSelection)
            {
                return;
            }

            // 새 카드가 켜지면 다른 페이지 선택도 해제함
            isUpdatingSelection = true;
            foreach (ToggleBinding binding in bindings)
            {
                Toggle toggle = binding.Toggle;
                if (toggle != null && toggle != selectedToggle && toggle.isOn)
                {
                    toggle.isOn = false;
                }
            }

            isUpdatingSelection = false;
        }

        private sealed class ToggleBinding
        {
            public ToggleBinding(Toggle toggle, UnityAction<bool> callback)
            {
                Toggle = toggle;
                Callback = callback;
            }

            public Toggle Toggle { get; }
            public UnityAction<bool> Callback { get; }
        }
    }
}
