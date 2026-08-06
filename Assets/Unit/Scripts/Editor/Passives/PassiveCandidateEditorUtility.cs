using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class PassiveCandidateEditorUtility
    {
        private const string PassiveDataFolder = "Assets/Unit/Data/Passives";

        public static void LoadAllPassives(List<PassiveDataSO> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();

            if (!AssetDatabase.IsValidFolder(PassiveDataFolder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { PassiveDataFolder });

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                PassiveDataSO passive = AssetDatabase.LoadAssetAtPath<PassiveDataSO>(assetPath);

                if (passive != null)
                {
                    destination.Add(passive);
                }
            }

            destination.Sort(CompareByAssetPath);
        }

        public static void BuildUnitCandidates(IReadOnlyList<PassiveDataSO> allPassives, UnitClass unitClass, UnitSubclass subclass, List<PassiveDataSO> destination)
        {
            destination.Clear();

            if (unitClass == UnitClass.None || subclass == UnitSubclass.None)
            {
                return;
            }

            for (int i = 0; i < allPassives.Count; i++)
            {
                PassiveDataSO passive = allPassives[i];

                if (passive != null && passive.CanBeUsedByUnit(unitClass, subclass))
                {
                    destination.Add(passive);
                }
            }
        }

        public static void BuildEnemyCandidates(IReadOnlyList<PassiveDataSO> allPassives, EnemyCategory category, EnemyMovementType movementType, EnemySize size, EnemyRole role, List<PassiveDataSO> destination)
        {
            destination.Clear();

            if (category == EnemyCategory.None || movementType == EnemyMovementType.None || size == EnemySize.None || role == EnemyRole.None)
            {
                return;
            }

            for (int i = 0; i < allPassives.Count; i++)
            {
                PassiveDataSO passive = allPassives[i];

                if (passive != null && passive.CanBeUsedByEnemy(category, movementType, size, role))
                {
                    destination.Add(passive);
                }
            }
        }

        public static GUIContent[] CreateOptionContents(IReadOnlyList<PassiveDataSO> candidates)
        {
            GUIContent[] options = new GUIContent[candidates.Count + 1];
            options[0] = new GUIContent("미설정", "이 패시브 슬롯을 비워 둡니다.");

            for (int i = 0; i < candidates.Count; i++)
            {
                PassiveDataSO passive = candidates[i];
                string displayName = string.IsNullOrWhiteSpace(passive.DisplayName) ? passive.name : passive.DisplayName;
                string label = string.Equals(displayName, passive.name, StringComparison.Ordinal) ? displayName : $"{displayName} ({passive.name})";
                string description = string.IsNullOrWhiteSpace(passive.Description) ? "설명이 입력되지 않았습니다." : passive.Description;
                string assetPath = AssetDatabase.GetAssetPath(passive);

                options[i + 1] = new GUIContent(label, $"{description}\n경로: {assetPath}");
            }

            return options;
        }

        public static int FindCandidateIndex(IReadOnlyList<PassiveDataSO> candidates, PassiveDataSO target)
        {
            if (target == null)
            {
                return 0;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == target)
                {
                    return i + 1;
                }
            }

            return -1;
        }

        public static bool IsAlreadyAssigned(SerializedProperty passiveList, PassiveDataSO candidate, int ignoredIndex)
        {
            if (candidate == null)
            {
                return false;
            }

            for (int i = 0; i < passiveList.arraySize; i++)
            {
                if (i == ignoredIndex)
                {
                    continue;
                }

                PassiveDataSO assigned = passiveList.GetArrayElementAtIndex(i).objectReferenceValue as PassiveDataSO;

                if (assigned == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareByAssetPath(PassiveDataSO left, PassiveDataSO right)
        {
            string leftPath = AssetDatabase.GetAssetPath(left);
            string rightPath = AssetDatabase.GetAssetPath(right);
            return string.CompareOrdinal(leftPath, rightPath);
        }
    }
}