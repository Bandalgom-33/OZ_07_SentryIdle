using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class BasicAttackRangeEditorGUI
    {
        private const float CellSize = 24f;
        private const float PreviewTileWorldSize = 1f;
        private const int CircleSegmentCount = 64;

        private static readonly Vector3[] CirclePoints = new Vector3[CircleSegmentCount + 1];
        private static readonly Color InRangeColor = new Color(0.35f, 0.65f, 1f, 0.45f);
        private static readonly Color SelectedInRangeColor = new Color(0.15f, 0.45f, 1f, 0.95f);
        private static readonly Color SelectedOutOfRangeColor = new Color(1f, 0.25f, 0.25f, 0.95f);
        private static readonly Color OutsideRangeColor = new Color(0.45f, 0.45f, 0.45f, 0.35f);
        private static readonly Color BodyColor = new Color(0.8f, 0.8f, 0.8f, 0.75f);
        private static readonly Color GuideColor = new Color(0.15f, 0.6f, 1f, 1f);

        public static void Draw(SerializedProperty property, SerializedProperty attackRange)
        {
            EditorGUILayout.Space(5f);

            if (property == null)
            {
                EditorGUILayout.HelpBox("기본 공격 타일 범위 데이터를 찾지 못했습니다. AttackSettings의 basicAttackRange 필드를 확인하세요.", MessageType.Error);
                return;
            }

            if (attackRange == null)
            {
                EditorGUILayout.HelpBox("공격 사거리 데이터를 찾지 못했습니다. AttackSettings의 attackRange 필드를 확인하세요.", MessageType.Error);
                return;
            }

            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "기본 공격 타일 범위", true);

            if (!property.isExpanded)
            {
                return;
            }

            SerializedProperty horizontalRadius = property.FindPropertyRelative("horizontalRadius");
            SerializedProperty forwardDistance = property.FindPropertyRelative("forwardDistance");
            SerializedProperty backwardDistance = property.FindPropertyRelative("backwardDistance");
            SerializedProperty attackTiles = property.FindPropertyRelative("attackTiles");

            if (horizontalRadius == null || forwardDistance == null || backwardDistance == null || attackTiles == null)
            {
                EditorGUILayout.HelpBox("기본 공격 타일 범위의 직렬화 필드를 찾지 못했습니다. BasicAttackRangeData의 필드 이름을 확인하세요.", MessageType.Error);
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.IntSlider(horizontalRadius, 0, 6, new GUIContent("좌우 표시 범위", "공격 주체의 왼쪽과 오른쪽에 표시할 타일 수입니다."));
            EditorGUILayout.IntSlider(forwardDistance, 0, 10, new GUIContent("정면 표시 범위", "기준 방향의 정면에 표시할 타일 수입니다. 양의 Y 좌표가 정면입니다."));
            EditorGUILayout.IntSlider(backwardDistance, 0, 10, new GUIContent("후방 표시 범위", "기준 방향의 후방에 표시할 타일 수입니다. 음의 Y 좌표가 후방입니다."));

            bool canEvaluateRange = !attackRange.hasMultipleDifferentValues;
            float attackRangeValue = canEvaluateRange ? Mathf.Max(0f, attackRange.floatValue) : 0f;

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("위쪽이 기준 정면(+Y)입니다. 파란색 원은 공격 사거리이며 현재 미리보기는 1타일 = 1월드 유닛을 기준으로 합니다.", MessageType.Info);
            EditorGUILayout.HelpBox("옅은 파란색은 사거리 안, 진한 파란색은 선택된 타일, 빨간색은 선택됐지만 사거리 밖인 타일입니다.", MessageType.None);

            if (!canEvaluateRange)
            {
                EditorGUILayout.HelpBox("여러 에셋의 공격 사거리가 서로 달라 사거리 가이드를 표시할 수 없습니다.", MessageType.Info);
            }
            else if (IsGuideClipped(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, attackRangeValue))
            {
                EditorGUILayout.HelpBox("현재 공격 사거리 원이 표시 격자보다 큽니다. 좌우·정면·후방 표시 범위를 늘리면 전체 사거리 원을 확인할 수 있습니다.", MessageType.Warning);
            }

            DrawGrid(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, attackTiles, attackRangeValue, canEvaluateRange);

            EditorGUILayout.Space(4f);

            int outOfRangeCount = canEvaluateRange ? CountOutOfAttackRangeTiles(attackTiles, attackRangeValue) : 0;
            EditorGUILayout.LabelField($"선택된 공격 타일: {attackTiles.arraySize}개", EditorStyles.miniLabel);

            int invalidTileCount = CountInvalidTiles(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, attackTiles);

            if (invalidTileCount > 0)
            {
                EditorGUILayout.HelpBox($"중복되었거나 현재 표시 격자 밖에 있는 공격 타일이 {invalidTileCount}개 있습니다. 자동으로 삭제하지 않으며 아래 버튼으로 직접 정리할 수 있습니다.", MessageType.Warning);

                if (GUILayout.Button("잘못된 공격 타일 정리"))
                {
                    RemoveInvalidTiles(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, attackTiles);
                }
            }

            if (outOfRangeCount > 0)
            {
                EditorGUILayout.HelpBox($"선택된 공격 타일 중 {outOfRangeCount}개가 현재 공격 사거리 밖에 있습니다. 빨간색 타일을 직접 해제하거나 아래 버튼으로 제거할 수 있습니다.", MessageType.Warning);

                if (GUILayout.Button("사거리 밖 공격 타일 제거"))
                {
                    RemoveOutOfAttackRangeTiles(attackTiles, attackRangeValue);
                }
            }

            using (new EditorGUI.DisabledScope(!canEvaluateRange || attackRangeValue <= 0f))
            {
                if (GUILayout.Button("사거리 안 타일 전체 선택"))
                {
                    SelectAllInsideAttackRange(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, attackTiles, attackRangeValue);
                }
            }

            if (attackTiles.arraySize > 0 && GUILayout.Button("공격 타일 전체 해제"))
            {
                attackTiles.ClearArray();
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawGrid(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty attackTiles, float attackRange, bool canEvaluateRange)
        {
            int columnCount = horizontalRadius * 2 + 1;
            int rowCount = forwardDistance + backwardDistance + 1;
            float gridWidth = columnCount * CellSize;
            float gridHeight = rowCount * CellSize;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight, GUILayout.Width(gridWidth), GUILayout.Height(gridHeight));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            for (int y = forwardDistance; y >= -backwardDistance; y--)
            {
                for (int x = -horizontalRadius; x <= horizontalRadius; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    int column = x + horizontalRadius;
                    int row = forwardDistance - y;
                    Rect cellRect = new Rect(gridRect.x + column * CellSize, gridRect.y + row * CellSize, CellSize, CellSize);
                    bool isBody = coordinate == Vector2Int.zero;
                    int selectedIndex = FindTileIndex(attackTiles, coordinate);
                    bool isSelected = selectedIndex >= 0;
                    bool isInsideRange = canEvaluateRange && IsInsideAttackRange(coordinate, attackRange);
                    bool canClick = !isBody && (!canEvaluateRange || isInsideRange || isSelected);
                    Color previousBackgroundColor = GUI.backgroundColor;

                    GUI.backgroundColor = GetCellColor(isBody, isSelected, isInsideRange, canEvaluateRange);

                    string symbol = isBody ? "본" : isSelected ? "■" : "□";
                    float worldDistance = GetWorldDistance(coordinate);
                    string rangeDescription = !canEvaluateRange ? "여러 공격 사거리 값" : isInsideRange ? "공격 사거리 안" : "공격 사거리 밖";
                    GUIContent content = new GUIContent(symbol, $"상대 타일 좌표 ({coordinate.x}, {coordinate.y}) / 중심 거리 {worldDistance:0.###} / {rangeDescription}");

                    using (new EditorGUI.DisabledScope(!canClick))
                    {
                        if (GUI.Button(cellRect, content, EditorStyles.miniButton))
                        {
                            if (isSelected)
                            {
                                attackTiles.DeleteArrayElementAtIndex(selectedIndex);
                            }
                            else
                            {
                                int newIndex = attackTiles.arraySize;
                                attackTiles.InsertArrayElementAtIndex(newIndex);
                                attackTiles.GetArrayElementAtIndex(newIndex).vector2IntValue = coordinate;
                            }
                        }
                    }

                    GUI.backgroundColor = previousBackgroundColor;
                }
            }

            if (canEvaluateRange && attackRange > 0f && Event.current.type == EventType.Repaint)
            {
                DrawAttackRangeGuide(gridRect, horizontalRadius, forwardDistance, attackRange);
            }
        }

        private static void DrawAttackRangeGuide(Rect gridRect, int horizontalRadius, int forwardDistance, float attackRange)
        {
            float centerX = gridRect.x + (horizontalRadius + 0.5f) * CellSize;
            float centerY = gridRect.y + (forwardDistance + 0.5f) * CellSize;
            float radiusPixels = attackRange / PreviewTileWorldSize * CellSize;

            for (int i = 0; i <= CircleSegmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / CircleSegmentCount;
                float x = centerX + Mathf.Cos(angle) * radiusPixels;
                float y = centerY + Mathf.Sin(angle) * radiusPixels;
                CirclePoints[i] = new Vector3(x, y, 0f);
            }

            Handles.BeginGUI();
            Color previousColor = Handles.color;
            Handles.color = GuideColor;
            Handles.DrawAAPolyLine(2f, CirclePoints);
            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private static Color GetCellColor(bool isBody, bool isSelected, bool isInsideRange, bool canEvaluateRange)
        {
            if (isBody)
            {
                return BodyColor;
            }

            if (!canEvaluateRange)
            {
                return isSelected ? SelectedInRangeColor : OutsideRangeColor;
            }

            if (isSelected && !isInsideRange)
            {
                return SelectedOutOfRangeColor;
            }

            if (isSelected)
            {
                return SelectedInRangeColor;
            }

            return isInsideRange ? InRangeColor : OutsideRangeColor;
        }

        private static bool IsGuideClipped(int horizontalRadius, int forwardDistance, int backwardDistance, float attackRange)
        {
            float horizontalWorldSize = (horizontalRadius + 0.5f) * PreviewTileWorldSize;
            float forwardWorldSize = (forwardDistance + 0.5f) * PreviewTileWorldSize;
            float backwardWorldSize = (backwardDistance + 0.5f) * PreviewTileWorldSize;
            return attackRange > horizontalWorldSize || attackRange > forwardWorldSize || attackRange > backwardWorldSize;
        }

        private static bool IsInsideAttackRange(Vector2Int coordinate, float attackRange)
        {
            return GetWorldDistance(coordinate) <= attackRange + 0.0001f;
        }

        private static float GetWorldDistance(Vector2Int coordinate)
        {
            return Mathf.Sqrt(coordinate.x * coordinate.x + coordinate.y * coordinate.y) * PreviewTileWorldSize;
        }

        private static int FindTileIndex(SerializedProperty attackTiles, Vector2Int coordinate)
        {
            for (int i = 0; i < attackTiles.arraySize; i++)
            {
                if (attackTiles.GetArrayElementAtIndex(i).vector2IntValue == coordinate)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CountInvalidTiles(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty attackTiles)
        {
            HashSet<Vector2Int> uniqueTiles = new HashSet<Vector2Int>();
            int invalidCount = 0;

            for (int i = 0; i < attackTiles.arraySize; i++)
            {
                Vector2Int coordinate = attackTiles.GetArrayElementAtIndex(i).vector2IntValue;

                if (coordinate == Vector2Int.zero || !IsInsideBounds(coordinate, horizontalRadius, forwardDistance, backwardDistance) || !uniqueTiles.Add(coordinate))
                {
                    invalidCount++;
                }
            }

            return invalidCount;
        }

        private static int CountOutOfAttackRangeTiles(SerializedProperty attackTiles, float attackRange)
        {
            int outOfRangeCount = 0;

            for (int i = 0; i < attackTiles.arraySize; i++)
            {
                Vector2Int coordinate = attackTiles.GetArrayElementAtIndex(i).vector2IntValue;

                if (!IsInsideAttackRange(coordinate, attackRange))
                {
                    outOfRangeCount++;
                }
            }

            return outOfRangeCount;
        }

        private static void RemoveInvalidTiles(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty attackTiles)
        {
            HashSet<Vector2Int> uniqueTiles = new HashSet<Vector2Int>();

            for (int i = attackTiles.arraySize - 1; i >= 0; i--)
            {
                Vector2Int coordinate = attackTiles.GetArrayElementAtIndex(i).vector2IntValue;

                if (coordinate == Vector2Int.zero || !IsInsideBounds(coordinate, horizontalRadius, forwardDistance, backwardDistance) || !uniqueTiles.Add(coordinate))
                {
                    attackTiles.DeleteArrayElementAtIndex(i);
                }
            }
        }

        private static void RemoveOutOfAttackRangeTiles(SerializedProperty attackTiles, float attackRange)
        {
            for (int i = attackTiles.arraySize - 1; i >= 0; i--)
            {
                Vector2Int coordinate = attackTiles.GetArrayElementAtIndex(i).vector2IntValue;

                if (!IsInsideAttackRange(coordinate, attackRange))
                {
                    attackTiles.DeleteArrayElementAtIndex(i);
                }
            }
        }

        private static void SelectAllInsideAttackRange(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty attackTiles, float attackRange)
        {
            for (int y = forwardDistance; y >= -backwardDistance; y--)
            {
                for (int x = -horizontalRadius; x <= horizontalRadius; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);

                    if (coordinate == Vector2Int.zero || !IsInsideAttackRange(coordinate, attackRange) || FindTileIndex(attackTiles, coordinate) >= 0)
                    {
                        continue;
                    }

                    int newIndex = attackTiles.arraySize;
                    attackTiles.InsertArrayElementAtIndex(newIndex);
                    attackTiles.GetArrayElementAtIndex(newIndex).vector2IntValue = coordinate;
                }
            }
        }

        private static bool IsInsideBounds(Vector2Int coordinate, int horizontalRadius, int forwardDistance, int backwardDistance)
        {
            bool isInsideHorizontal = coordinate.x >= -horizontalRadius && coordinate.x <= horizontalRadius;
            bool isInsideVertical = coordinate.y >= -backwardDistance && coordinate.y <= forwardDistance;
            return isInsideHorizontal && isInsideVertical;
        }
    }
}