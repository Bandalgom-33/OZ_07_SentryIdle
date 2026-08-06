using System.Collections.Generic;
using System.Reflection;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class PassiveCompatibilityEditorGUI
    {
        private static readonly UnitClass[] UnitClassOptions =
        {
            UnitClass.Vanguard,
            UnitClass.Guard,
            UnitClass.Defender,
            UnitClass.Supporter,
            UnitClass.Sniper,
            UnitClass.Specialist
        };

        private static readonly GUIContent[] UnitClassContents = CreateContents(UnitClassOptions);

        public static void DrawUnitRestrictions(SerializedProperty allowedClasses, SerializedProperty allowedSubclasses)
        {
            EditorGUILayout.LabelField("캐릭터 분류 제한", EditorStyles.boldLabel);

            DrawUnitClassList(allowedClasses);

            List<UnitSubclass> subclassCandidates = BuildSubclassCandidates(allowedClasses);
            DrawUnitSubclassList(allowedSubclasses, subclassCandidates);
        }

        private static void DrawUnitClassList(SerializedProperty property)
        {
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("허용 상위 분류", EditorStyles.miniBoldLabel);

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                UnitClass current = (UnitClass)element.intValue;
                int selectedIndex = FindIndex(UnitClassOptions, current);

                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                    element.intValue = (int)UnitClassOptions[0];
                }

                EditorGUILayout.BeginHorizontal();

                int newIndex = EditorGUILayout.Popup(
                    new GUIContent($"상위 분류 {i + 1}"),
                    selectedIndex,
                    UnitClassContents);

                if (newIndex != selectedIndex)
                {
                    element.intValue = (int)UnitClassOptions[newIndex];
                }

                if (GUILayout.Button("제거", GUILayout.Width(48f)))
                {
                    property.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            UnitClass nextClass = FindFirstUnusedClass(property);
            bool canAdd = nextClass != UnitClass.None;

            using (new EditorGUI.DisabledScope(!canAdd))
            {
                if (GUILayout.Button("허용 상위 분류 추가"))
                {
                    int newIndex = property.arraySize;
                    property.arraySize++;
                    property.GetArrayElementAtIndex(newIndex).intValue = (int)nextClass;
                }
            }

            if (HasDuplicateValues(property))
            {
                EditorGUILayout.HelpBox("같은 상위 분류가 중복 등록되어 있습니다.", MessageType.Warning);
            }

            if (property.arraySize == 0)
            {
                EditorGUILayout.HelpBox("상위 분류 제한이 비어 있어 모든 캐릭터 상위 분류를 허용합니다.", MessageType.Info);
            }
        }

        private static void DrawUnitSubclassList(SerializedProperty property, List<UnitSubclass> candidates)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("허용 세부 분류", EditorStyles.miniBoldLabel);

            if (candidates.Count == 0)
            {
                EditorGUILayout.HelpBox("허용 상위 분류를 추가하면 해당 분류의 세부 분류만 선택할 수 있습니다.", MessageType.Info);

                if (property.arraySize > 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(property, new GUIContent("현재 세부 분류 제한"), true);
                    }

                    if (GUILayout.Button("현재 세부 분류 제한 모두 제거"))
                    {
                        property.ClearArray();
                    }
                }

                return;
            }

            GUIContent[] candidateContents = CreateContents(candidates);

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                UnitSubclass current = (UnitSubclass)element.intValue;
                int selectedIndex = FindIndex(candidates, current);

                EditorGUILayout.BeginHorizontal();

                if (selectedIndex < 0)
                {
                    EditorGUILayout.LabelField(
                        $"세부 분류 {i + 1}",
                        $"{GetDisplayName(current)} (현재 상위 분류와 불일치)");
                }
                else
                {
                    int newIndex = EditorGUILayout.Popup(
                        new GUIContent($"세부 분류 {i + 1}"),
                        selectedIndex,
                        candidateContents);

                    if (newIndex != selectedIndex)
                    {
                        element.intValue = (int)candidates[newIndex];
                    }
                }

                if (GUILayout.Button("제거", GUILayout.Width(48f)))
                {
                    property.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            UnitSubclass nextSubclass = FindFirstUnusedSubclass(property, candidates);
            bool canAdd = nextSubclass != UnitSubclass.None;

            using (new EditorGUI.DisabledScope(!canAdd))
            {
                if (GUILayout.Button("허용 세부 분류 추가"))
                {
                    int newIndex = property.arraySize;
                    property.arraySize++;
                    property.GetArrayElementAtIndex(newIndex).intValue = (int)nextSubclass;
                }
            }

            if (HasInvalidSubclasses(property, candidates))
            {
                EditorGUILayout.HelpBox("현재 허용 상위 분류에 속하지 않는 세부 분류가 있습니다. 해당 항목을 제거하세요.", MessageType.Warning);
            }

            if (HasDuplicateValues(property))
            {
                EditorGUILayout.HelpBox("같은 세부 분류가 중복 등록되어 있습니다.", MessageType.Warning);
            }

            if (property.arraySize == 0)
            {
                EditorGUILayout.HelpBox("세부 분류 제한이 비어 있어 선택한 상위 분류의 모든 세부 분류를 허용합니다.", MessageType.Info);
            }
        }

        private static List<UnitSubclass> BuildSubclassCandidates(SerializedProperty allowedClasses)
        {
            List<UnitSubclass> candidates = new List<UnitSubclass>();

            for (int i = 0; i < allowedClasses.arraySize; i++)
            {
                UnitClass unitClass = (UnitClass)allowedClasses.GetArrayElementAtIndex(i).intValue;
                IReadOnlyList<UnitSubclass> subclasses = UnitClassRules.GetSubclasses(unitClass);

                for (int j = 0; j < subclasses.Count; j++)
                {
                    UnitSubclass subclass = subclasses[j];

                    if (subclass != UnitSubclass.None && !candidates.Contains(subclass))
                    {
                        candidates.Add(subclass);
                    }
                }
            }

            return candidates;
        }

        private static UnitClass FindFirstUnusedClass(SerializedProperty property)
        {
            for (int i = 0; i < UnitClassOptions.Length; i++)
            {
                UnitClass candidate = UnitClassOptions[i];

                if (!ContainsValue(property, (int)candidate))
                {
                    return candidate;
                }
            }

            return UnitClass.None;
        }

        private static UnitSubclass FindFirstUnusedSubclass(SerializedProperty property, IReadOnlyList<UnitSubclass> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                UnitSubclass candidate = candidates[i];

                if (!ContainsValue(property, (int)candidate))
                {
                    return candidate;
                }
            }

            return UnitSubclass.None;
        }

        private static bool ContainsValue(SerializedProperty property, int value)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).intValue == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDuplicateValues(SerializedProperty property)
        {
            HashSet<int> usedValues = new HashSet<int>();

            for (int i = 0; i < property.arraySize; i++)
            {
                int value = property.GetArrayElementAtIndex(i).intValue;

                if (!usedValues.Add(value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasInvalidSubclasses(SerializedProperty property, IReadOnlyList<UnitSubclass> candidates)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                UnitSubclass current = (UnitSubclass)property.GetArrayElementAtIndex(i).intValue;

                if (FindIndex(candidates, current) < 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindIndex<T>(IReadOnlyList<T> values, T target)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < values.Count; i++)
            {
                if (comparer.Equals(values[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private static GUIContent[] CreateContents<T>(IReadOnlyList<T> values)
        {
            GUIContent[] contents = new GUIContent[values.Count];

            for (int i = 0; i < values.Count; i++)
            {
                contents[i] = new GUIContent(GetDisplayName(values[i]));
            }

            return contents;
        }

        private static string GetDisplayName<T>(T value)
        {
            FieldInfo field = typeof(T).GetField(value.ToString());
            InspectorNameAttribute inspectorName = field?.GetCustomAttribute<InspectorNameAttribute>();

            if (inspectorName != null)
            {
                return inspectorName.displayName;
            }

            return ObjectNames.NicifyVariableName(value.ToString());
        }
    }
}