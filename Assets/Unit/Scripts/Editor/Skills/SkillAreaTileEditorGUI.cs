using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class SkillAreaTileEditorGUI
    {
        private const float CellSize = 24f;

        private static readonly Color SelectedColor = new Color(0.15f, 0.45f, 1f, 0.95f);
        private static readonly Color EmptyColor = new Color(0.55f, 0.55f, 0.55f, 0.3f);
        private static readonly Color CenterColor = new Color(0.3f, 0.8f, 0.45f, 0.9f);

        internal static void Draw(SerializedProperty property)
        {
            EditorGUILayout.Space(4f);

            if (property == null)
            {
                EditorGUILayout.HelpBox("범위 스킬 타일 데이터를 찾지 못했습니다. UnitSkillSettings의 areaTileRange 필드를 확인하세요.", MessageType.Error);
                return;
            }

            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "범위 공격 타일 패턴", true);
            if (!property.isExpanded)
            {
                return;
            }

            SerializedProperty horizontalRadius = property.FindPropertyRelative("horizontalRadius");
            SerializedProperty forwardDistance = property.FindPropertyRelative("forwardDistance");
            SerializedProperty backwardDistance = property.FindPropertyRelative("backwardDistance");
            SerializedProperty affectedTiles = property.FindPropertyRelative("affectedTiles");

            if (horizontalRadius == null || forwardDistance == null || backwardDistance == null || affectedTiles == null)
            {
                EditorGUILayout.HelpBox("범위 스킬 타일 데이터의 직렬화 필드를 찾지 못했습니다.", MessageType.Error);
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.IntSlider(horizontalRadius, 0, 6, new GUIContent("좌우 표시 범위", "중심 대상의 왼쪽과 오른쪽에 표시할 타일 수입니다."));
            EditorGUILayout.IntSlider(forwardDistance, 0, 10, new GUIContent("위쪽 표시 범위", "중심 대상 위쪽에 표시할 타일 수입니다."));
            EditorGUILayout.IntSlider(backwardDistance, 0, 10, new GUIContent("아래쪽 표시 범위", "중심 대상 아래쪽에 표시할 타일 수입니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("초록색 '중'이 대표 대상의 타일 (0,0)이며 항상 피해를 받습니다. 주변 칸을 클릭해 같이 맞을 타일을 직접 선택하세요. 숫자 반경 계산은 사용하지 않습니다.", MessageType.Info);

            DrawGrid(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, affectedTiles);

            EditorGUILayout.Space(4f);
            int invalidCount = CountInvalidTiles(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, affectedTiles);
            EditorGUILayout.LabelField($"추가 영향 타일: {affectedTiles.arraySize}개 / 중심 포함 최대 타일: {affectedTiles.arraySize + 1}개", EditorStyles.miniLabel);

            if (invalidCount > 0)
            {
                EditorGUILayout.HelpBox($"중복되었거나 현재 표시 격자 밖에 있는 영향 타일이 {invalidCount}개 있습니다.", MessageType.Warning);
                if (GUILayout.Button("잘못된 영향 타일 정리"))
                {
                    RemoveInvalidTiles(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, affectedTiles);
                }
            }

            if (GUILayout.Button("표시 격자 전체 선택"))
            {
                SelectAll(horizontalRadius.intValue, forwardDistance.intValue, backwardDistance.intValue, affectedTiles);
            }

            if (affectedTiles.arraySize > 0 && GUILayout.Button("추가 영향 타일 전체 해제"))
            {
                affectedTiles.ClearArray();
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawGrid(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty affectedTiles)
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
                    bool isCenter = coordinate == Vector2Int.zero;
                    int selectedIndex = FindTileIndex(affectedTiles, coordinate);
                    bool isSelected = selectedIndex >= 0;
                    Color previous = GUI.backgroundColor;

                    GUI.backgroundColor = isCenter ? CenterColor : isSelected ? SelectedColor : EmptyColor;
                    GUIContent content = isCenter
                        ? new GUIContent("중", "대표 대상 타일 (0,0) - 항상 피해 적용")
                        : new GUIContent(isSelected ? "■" : "□", $"중심 대상 기준 상대 타일 ({coordinate.x}, {coordinate.y})");

                    using (new EditorGUI.DisabledScope(isCenter))
                    {
                        if (GUI.Button(cellRect, content, EditorStyles.miniButton))
                        {
                            if (isSelected)
                            {
                                affectedTiles.DeleteArrayElementAtIndex(selectedIndex);
                            }
                            else
                            {
                                int newIndex = affectedTiles.arraySize;
                                affectedTiles.InsertArrayElementAtIndex(newIndex);
                                affectedTiles.GetArrayElementAtIndex(newIndex).vector2IntValue = coordinate;
                            }
                        }
                    }

                    GUI.backgroundColor = previous;
                }
            }
        }

        private static int FindTileIndex(SerializedProperty tiles, Vector2Int coordinate)
        {
            for (int i = 0; i < tiles.arraySize; i++)
            {
                if (tiles.GetArrayElementAtIndex(i).vector2IntValue == coordinate)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CountInvalidTiles(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty tiles)
        {
            int invalid = 0;
            for (int i = 0; i < tiles.arraySize; i++)
            {
                Vector2Int coordinate = tiles.GetArrayElementAtIndex(i).vector2IntValue;
                if (coordinate == Vector2Int.zero || !IsInsideDisplay(coordinate, horizontalRadius, forwardDistance, backwardDistance) || HasEarlierDuplicate(tiles, i, coordinate))
                {
                    invalid++;
                }
            }

            return invalid;
        }

        private static bool HasEarlierDuplicate(SerializedProperty tiles, int index, Vector2Int coordinate)
        {
            for (int i = 0; i < index; i++)
            {
                if (tiles.GetArrayElementAtIndex(i).vector2IntValue == coordinate)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideDisplay(Vector2Int coordinate, int horizontalRadius, int forwardDistance, int backwardDistance)
        {
            return coordinate.x >= -horizontalRadius && coordinate.x <= horizontalRadius &&
                   coordinate.y >= -backwardDistance && coordinate.y <= forwardDistance;
        }

        private static void RemoveInvalidTiles(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty tiles)
        {
            for (int i = tiles.arraySize - 1; i >= 0; i--)
            {
                Vector2Int coordinate = tiles.GetArrayElementAtIndex(i).vector2IntValue;
                if (coordinate == Vector2Int.zero || !IsInsideDisplay(coordinate, horizontalRadius, forwardDistance, backwardDistance) || HasEarlierDuplicate(tiles, i, coordinate))
                {
                    tiles.DeleteArrayElementAtIndex(i);
                }
            }
        }

        private static void SelectAll(int horizontalRadius, int forwardDistance, int backwardDistance, SerializedProperty tiles)
        {
            tiles.ClearArray();
            for (int y = forwardDistance; y >= -backwardDistance; y--)
            {
                for (int x = -horizontalRadius; x <= horizontalRadius; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    if (coordinate == Vector2Int.zero)
                    {
                        continue;
                    }

                    int index = tiles.arraySize;
                    tiles.InsertArrayElementAtIndex(index);
                    tiles.GetArrayElementAtIndex(index).vector2IntValue = coordinate;
                }
            }
        }
    }
}
