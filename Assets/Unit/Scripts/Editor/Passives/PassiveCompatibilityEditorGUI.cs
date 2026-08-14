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
            UnitClass.Sniper
        };

        private static readonly EnemySize[] EnemySizeOptions =
        {
            EnemySize.Small,
            EnemySize.Medium,
            EnemySize.Large
        };

        private static readonly GUIContent[] UnitClassContents = CreateContents(UnitClassOptions);

        private static readonly GUIContent[] EnemySizeContents = CreateContents(EnemySizeOptions);

        public static void DrawUnitRestrictions(SerializedProperty allowedClasses)
        {
            EditorGUILayout.LabelField("캐릭터 직군 패시브 풀", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "일반 캐릭터는 자신의 상위 직군 풀에 등록된 패시브만 선택할 수 있습니다. 스페셜리스트는 예외로 모든 캐릭터 패시브를 선택할 수 있습니다.",
                MessageType.Info);

            for (int i = 0; i < allowedClasses.arraySize; i++)
            {
                SerializedProperty element = allowedClasses.GetArrayElementAtIndex(i);
                UnitClass current = (UnitClass)element.intValue;
                int selectedIndex = FindIndex(UnitClassOptions, current);

                EditorGUILayout.BeginHorizontal();

                if (selectedIndex < 0)
                {
                    EditorGUILayout.LabelField($"직군 풀 {i + 1}", $"{GetDisplayName(current)} (사용하지 않는 분류)");
                }
                else
                {
                    int newIndex = EditorGUILayout.Popup(new GUIContent($"직군 풀 {i + 1}"), selectedIndex, UnitClassContents);

                    if (newIndex != selectedIndex)
                    {
                        element.intValue = (int)UnitClassOptions[newIndex];
                    }
                }

                if (GUILayout.Button("제거", GUILayout.Width(48f)))
                {
                    allowedClasses.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            UnitClass nextClass = FindFirstUnusedUnitClass(allowedClasses);
            bool canAdd = nextClass != UnitClass.None;

            using (new EditorGUI.DisabledScope(!canAdd))
            {
                if (GUILayout.Button("직군 풀 추가"))
                {
                    int newIndex = allowedClasses.arraySize;
                    allowedClasses.arraySize++;
                    allowedClasses.GetArrayElementAtIndex(newIndex).intValue = (int)nextClass;
                }
            }

            if (HasDuplicateValues(allowedClasses))
            {
                EditorGUILayout.HelpBox("같은 캐릭터 직군 풀이 중복 등록되어 있습니다.", MessageType.Warning);
            }

            if (allowedClasses.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "일반 직군에 배정되지 않은 패시브입니다. 스페셜리스트는 모든 캐릭터 패시브를 사용할 수 있으므로 이 패시브도 선택할 수 있습니다.",
                    MessageType.Info);
            }
        }

        public static void DrawEnemyRestrictions(SerializedProperty allowedSizes)
        {
            EditorGUILayout.LabelField("몬스터 크기 패시브 풀", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "몬스터는 자신의 크기와 일치하는 패시브 풀에서만 패시브를 선택할 수 있습니다.",
                MessageType.Info);

            for (int i = 0; i < allowedSizes.arraySize; i++)
            {
                SerializedProperty element = allowedSizes.GetArrayElementAtIndex(i);
                EnemySize current = (EnemySize)element.intValue;
                int selectedIndex = FindIndex(EnemySizeOptions, current);

                EditorGUILayout.BeginHorizontal();

                if (selectedIndex < 0)
                {
                    EditorGUILayout.LabelField($"크기 풀 {i + 1}", $"{GetDisplayName(current)} (사용하지 않는 크기)");
                }
                else
                {
                    int newIndex = EditorGUILayout.Popup(new GUIContent($"크기 풀 {i + 1}"), selectedIndex, EnemySizeContents);

                    if (newIndex != selectedIndex)
                    {
                        element.intValue = (int)EnemySizeOptions[newIndex];
                    }
                }

                if (GUILayout.Button("제거", GUILayout.Width(48f)))
                {
                    allowedSizes.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EnemySize nextSize = FindFirstUnusedEnemySize(allowedSizes);
            bool canAdd = nextSize != EnemySize.None;

            using (new EditorGUI.DisabledScope(!canAdd))
            {
                if (GUILayout.Button("크기 풀 추가"))
                {
                    int newIndex = allowedSizes.arraySize;
                    allowedSizes.arraySize++;
                    allowedSizes.GetArrayElementAtIndex(newIndex).intValue = (int)nextSize;
                }
            }

            if (HasDuplicateValues(allowedSizes))
            {
                EditorGUILayout.HelpBox("같은 몬스터 크기 풀이 중복 등록되어 있습니다.", MessageType.Warning);
            }

            if (allowedSizes.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "몬스터 크기 풀이 지정되지 않아 현재 어떤 몬스터도 이 패시브를 선택할 수 없습니다.",
                    MessageType.Warning);
            }
        }

        private static UnitClass FindFirstUnusedUnitClass(SerializedProperty property)
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

        private static EnemySize FindFirstUnusedEnemySize(SerializedProperty property)
        {
            for (int i = 0; i < EnemySizeOptions.Length; i++)
            {
                EnemySize candidate = EnemySizeOptions[i];

                if (!ContainsValue(property, (int)candidate))
                {
                    return candidate;
                }
            }

            return EnemySize.None;
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
                if (!usedValues.Add(property.GetArrayElementAtIndex(i).intValue))
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

            return inspectorName != null
                ? inspectorName.displayName
                : ObjectNames.NicifyVariableName(value.ToString());
        }
    }
}