using System;
using System.Collections.Generic;
using System.Reflection;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class PassiveTuningEditorGUI
    {
        private static readonly PassiveValueKey[] ValueKeys = (PassiveValueKey[])Enum.GetValues(typeof(PassiveValueKey));
        private static readonly PassiveRefKey[] RefKeys = (PassiveRefKey[])Enum.GetValues(typeof(PassiveRefKey));

        private static readonly Dictionary<PassiveValueKey, GUIContent> ValueLabelCache = new Dictionary<PassiveValueKey, GUIContent>();
        private static readonly Dictionary<PassiveRefKey, GUIContent> RefLabelCache = new Dictionary<PassiveRefKey, GUIContent>();

        public static void Draw(SerializedProperty passives, SerializedProperty passiveTunings, bool isEditingMultipleObjects, string ownerLabel)
        {
            EditorGUILayout.Space(8f);

            if (passives == null || passiveTunings == null)
            {
                EditorGUILayout.HelpBox("패시브 개별 설정 SerializedProperty를 찾지 못했습니다.", MessageType.Error);
                return;
            }

            passiveTunings.isExpanded = EditorGUILayout.Foldout(passiveTunings.isExpanded, "패시브 개별 설정", true);

            if (!passiveTunings.isExpanded)
            {
                return;
            }

            if (isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox($"여러 {ownerLabel} 데이터를 동시에 선택한 상태에서는 개체별 패시브 설정을 편집하지 않습니다.", MessageType.Info);
                return;
            }

            Synchronize(passives, passiveTunings);

            if (passives.arraySize == 0)
            {
                EditorGUILayout.HelpBox("선택한 패시브가 없습니다. 먼저 위의 패시브 능력 목록에서 패시브를 선택하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("패시브 기능은 공용으로 사용하고, 아래 숫자와 에셋 참조는 이 캐릭터 또는 몬스터에 개별 저장됩니다.", MessageType.Info);

            for (int i = 0; i < passiveTunings.arraySize; i++)
            {
                SerializedProperty tuning = passiveTunings.GetArrayElementAtIndex(i);
                SerializedProperty passiveProperty = tuning.FindPropertyRelative("passive");
                SerializedProperty valuesProperty = tuning.FindPropertyRelative("values");
                SerializedProperty refsProperty = tuning.FindPropertyRelative("refs");

                PassiveDataSO passive = passiveProperty.objectReferenceValue as PassiveDataSO;

                EditorGUILayout.Space(6f);

                string passiveName = passive != null && !string.IsNullOrWhiteSpace(passive.DisplayName) ? passive.DisplayName : "패시브";
                EditorGUILayout.LabelField($"{i + 1}. {passiveName}", EditorStyles.miniBoldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("패시브 정의", passive, typeof(PassiveDataSO), false);
                }

                bool hasValues = valuesProperty != null && valuesProperty.arraySize > 0;
                bool hasRefs = refsProperty != null && refsProperty.arraySize > 0;

                if (!hasValues && !hasRefs)
                {
                    EditorGUILayout.HelpBox("이 패시브에는 현재 개체별로 조정할 숫자나 에셋 참조가 없습니다.", MessageType.None);
                    continue;
                }

                DrawValues(valuesProperty);
                DrawReferences(refsProperty);
            }
        }

        private static void DrawValues(SerializedProperty values)
        {
            if (values == null || values.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("개별 수치", EditorStyles.miniBoldLabel);

            for (int i = 0; i < values.arraySize; i++)
            {
                SerializedProperty valueProperty = values.GetArrayElementAtIndex(i);
                SerializedProperty keyProperty = valueProperty.FindPropertyRelative("key");
                SerializedProperty numberProperty = valueProperty.FindPropertyRelative("value");

                PassiveValueKey key = (PassiveValueKey)keyProperty.intValue;

                if (IsIntegerValueKey(key))
                {
                    int minimum = GetIntegerMinimum(key);
                    int currentValue = Mathf.Max(minimum, Mathf.RoundToInt(numberProperty.floatValue));
                    int newValue = EditorGUILayout.IntField(GetValueLabel(key), currentValue);
                    numberProperty.floatValue = Mathf.Max(minimum, newValue);
                    continue;
                }

                EditorGUILayout.PropertyField(numberProperty, GetValueLabel(key));
            }
        }

        private static void DrawReferences(SerializedProperty refs)
        {
            if (refs == null || refs.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("개별 에셋 참조", EditorStyles.miniBoldLabel);

            for (int i = 0; i < refs.arraySize; i++)
            {
                SerializedProperty refProperty = refs.GetArrayElementAtIndex(i);
                SerializedProperty keyProperty = refProperty.FindPropertyRelative("key");
                SerializedProperty referenceProperty = refProperty.FindPropertyRelative("reference");

                PassiveRefKey key = (PassiveRefKey)keyProperty.intValue;

                switch (key)
                {
                    case PassiveRefKey.SummonPrefab:
                        DrawGameObjectReference(referenceProperty, GetRefLabel(key));
                        break;

                    default:
                        EditorGUILayout.PropertyField(referenceProperty, GetRefLabel(key));
                        break;
                }
            }
        }

        private static void DrawGameObjectReference(SerializedProperty property, GUIContent label)
        {
            GameObject current = property.objectReferenceValue as GameObject;
            GameObject selected = EditorGUILayout.ObjectField(label, current, typeof(GameObject), false) as GameObject;

            if (selected != current)
            {
                property.objectReferenceValue = selected;
            }
        }

        private static void Synchronize(SerializedProperty passives, SerializedProperty passiveTunings)
        {
            RemoveUnusedTunings(passives, passiveTunings);

            for (int passiveIndex = 0; passiveIndex < passives.arraySize; passiveIndex++)
            {
                PassiveDataSO passive = passives.GetArrayElementAtIndex(passiveIndex).objectReferenceValue as PassiveDataSO;

                if (passive == null)
                {
                    continue;
                }

                int tuningIndex = FindTuningIndex(passiveTunings, passive);

                if (tuningIndex < 0)
                {
                    tuningIndex = passiveTunings.arraySize;
                    passiveTunings.arraySize++;

                    SerializedProperty newTuning = passiveTunings.GetArrayElementAtIndex(tuningIndex);
                    newTuning.FindPropertyRelative("passive").objectReferenceValue = passive;
                    newTuning.FindPropertyRelative("values").ClearArray();
                    newTuning.FindPropertyRelative("refs").ClearArray();
                }

                SerializedProperty tuning = passiveTunings.GetArrayElementAtIndex(tuningIndex);

                SynchronizeValues(tuning, passive);
                SynchronizeReferences(tuning, passive);
            }

            SortTuningsByPassiveOrder(passives, passiveTunings);
        }

        private static void RemoveUnusedTunings(SerializedProperty passives, SerializedProperty passiveTunings)
        {
            for (int i = passiveTunings.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty tuning = passiveTunings.GetArrayElementAtIndex(i);
                PassiveDataSO passive = tuning.FindPropertyRelative("passive").objectReferenceValue as PassiveDataSO;

                if (passive == null || !ContainsPassive(passives, passive))
                {
                    passiveTunings.DeleteArrayElementAtIndex(i);
                }
            }
        }

        private static void SynchronizeValues(SerializedProperty tuning, PassiveDataSO passive)
        {
            SerializedProperty values = tuning.FindPropertyRelative("values");

            if (values == null)
            {
                return;
            }

            for (int i = values.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty valueProperty = values.GetArrayElementAtIndex(i);
                PassiveValueKey key = (PassiveValueKey)valueProperty.FindPropertyRelative("key").intValue;

                if (key == PassiveValueKey.None || !passive.TryGetDefaultValue(key, out _))
                {
                    values.DeleteArrayElementAtIndex(i);
                }
            }

            for (int i = 0; i < ValueKeys.Length; i++)
            {
                PassiveValueKey key = ValueKeys[i];

                if (key == PassiveValueKey.None || !passive.TryGetDefaultValue(key, out float defaultValue) || ContainsValueKey(values, key))
                {
                    continue;
                }

                int newIndex = values.arraySize;
                values.arraySize++;

                SerializedProperty newValue = values.GetArrayElementAtIndex(newIndex);
                newValue.FindPropertyRelative("key").intValue = (int)key;

                if (IsIntegerValueKey(key))
                {
                    int minimum = GetIntegerMinimum(key);
                    newValue.FindPropertyRelative("value").floatValue = Mathf.Max(minimum, Mathf.RoundToInt(defaultValue));
                    continue;
                }

                newValue.FindPropertyRelative("value").floatValue = defaultValue;
            }
        }

        private static void SynchronizeReferences(SerializedProperty tuning, PassiveDataSO passive)
        {
            SerializedProperty refs = tuning.FindPropertyRelative("refs");

            if (refs == null)
            {
                return;
            }

            for (int i = refs.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty refProperty = refs.GetArrayElementAtIndex(i);
                PassiveRefKey key = (PassiveRefKey)refProperty.FindPropertyRelative("key").intValue;

                if (key == PassiveRefKey.None || !passive.TryGetDefaultReference(key, out _))
                {
                    refs.DeleteArrayElementAtIndex(i);
                }
            }

            for (int i = 0; i < RefKeys.Length; i++)
            {
                PassiveRefKey key = RefKeys[i];

                if (key == PassiveRefKey.None || !passive.TryGetDefaultReference(key, out UnityEngine.Object defaultReference) || ContainsRefKey(refs, key))
                {
                    continue;
                }

                int newIndex = refs.arraySize;
                refs.arraySize++;

                SerializedProperty newRef = refs.GetArrayElementAtIndex(newIndex);
                newRef.FindPropertyRelative("key").intValue = (int)key;
                newRef.FindPropertyRelative("reference").objectReferenceValue = defaultReference;
            }
        }

        private static void SortTuningsByPassiveOrder(SerializedProperty passives, SerializedProperty passiveTunings)
        {
            int targetTuningIndex = 0;

            for (int passiveIndex = 0; passiveIndex < passives.arraySize; passiveIndex++)
            {
                PassiveDataSO passive = passives.GetArrayElementAtIndex(passiveIndex).objectReferenceValue as PassiveDataSO;

                if (passive == null)
                {
                    continue;
                }

                int currentIndex = FindTuningIndex(passiveTunings, passive);

                if (currentIndex >= 0 && currentIndex != targetTuningIndex)
                {
                    passiveTunings.MoveArrayElement(currentIndex, targetTuningIndex);
                }

                targetTuningIndex++;
            }
        }

        private static int FindTuningIndex(SerializedProperty passiveTunings, PassiveDataSO passive)
        {
            for (int i = 0; i < passiveTunings.arraySize; i++)
            {
                SerializedProperty tuning = passiveTunings.GetArrayElementAtIndex(i);

                if (tuning.FindPropertyRelative("passive").objectReferenceValue == passive)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ContainsPassive(SerializedProperty passives, PassiveDataSO passive)
        {
            for (int i = 0; i < passives.arraySize; i++)
            {
                if (passives.GetArrayElementAtIndex(i).objectReferenceValue == passive)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsValueKey(SerializedProperty values, PassiveValueKey key)
        {
            for (int i = 0; i < values.arraySize; i++)
            {
                SerializedProperty valueProperty = values.GetArrayElementAtIndex(i);

                if ((PassiveValueKey)valueProperty.FindPropertyRelative("key").intValue == key)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsRefKey(SerializedProperty refs, PassiveRefKey key)
        {
            for (int i = 0; i < refs.arraySize; i++)
            {
                SerializedProperty refProperty = refs.GetArrayElementAtIndex(i);

                if ((PassiveRefKey)refProperty.FindPropertyRelative("key").intValue == key)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIntegerValueKey(PassiveValueKey key)
        {
            return key == PassiveValueKey.SummonCostGain ||
                   key == PassiveValueKey.RandomTargetCount ||
                   key == PassiveValueKey.BurstAttackCount ||
                   key == PassiveValueKey.SummonCount ||
                   key == PassiveValueKey.MaxActiveSummons;
        }

        private static int GetIntegerMinimum(PassiveValueKey key)
        {
            return key == PassiveValueKey.SummonCostGain ? 0 : 1;
        }

        private static GUIContent GetValueLabel(PassiveValueKey key)
        {
            if (ValueLabelCache.TryGetValue(key, out GUIContent cached))
            {
                return cached;
            }

            GUIContent content = CreateLabel(typeof(PassiveValueKey), key.ToString(), "이 캐릭터 또는 몬스터가 실제로 사용할 개별 패시브 수치입니다.");

            ValueLabelCache.Add(key, content);
            return content;
        }

        private static GUIContent GetRefLabel(PassiveRefKey key)
        {
            if (RefLabelCache.TryGetValue(key, out GUIContent cached))
            {
                return cached;
            }

            GUIContent content = CreateLabel(typeof(PassiveRefKey), key.ToString(), "이 캐릭터 또는 몬스터가 해당 패시브에서 실제로 사용할 개별 에셋 참조입니다.");

            RefLabelCache.Add(key, content);
            return content;
        }

        private static GUIContent CreateLabel(Type enumType, string fieldName, string tooltip)
        {
            FieldInfo field = enumType.GetField(fieldName);
            InspectorNameAttribute inspectorName = field?.GetCustomAttribute<InspectorNameAttribute>();
            string label = inspectorName != null ? inspectorName.displayName : ObjectNames.NicifyVariableName(fieldName);

            return new GUIContent(label, tooltip);
        }
    }
}