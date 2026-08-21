using System;

// 유닛 식별자(문자열 키)와 고유 정수 ID 간의 상호 변환 및 검증 유틸리티 클래스
public static class UnitIdHelper
{
    private const string UnitPrefix = "UNIT_";

    // 문자열 유닛 키에서 정수 ID 추출 연산
    public static int ParseUnitId(string unitKey)
    {
        if (string.IsNullOrEmpty(unitKey))
        {
            return -1;
        }

        // "UNIT_0001" 형식에서 숫자 부분 파싱
        if (unitKey.StartsWith(UnitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string numberPart = unitKey.Substring(UnitPrefix.Length);
            if (int.TryParse(numberPart, out int parsedId))
            {
                return parsedId;
            }
        }
        else if (int.TryParse(unitKey, out int directId))
        {
            return directId;
        }

        return -1;
    }

    // 정수 ID를 표준 유닛 키 문자열로 포맷팅 연산
    public static string ToUnitKey(int unitId)
    {
        if (unitId <= 0)
        {
            return string.Empty;
        }

        return $"{UnitPrefix}{unitId:D4}";
    }

    // 유효한 유닛 ID인지 판정 연산
    public static bool IsValidUnitId(int unitId)
    {
        return unitId > 0;
    }

    // 유효한 유닛 키인지 판정 연산
    public static bool IsValidUnitKey(string unitKey)
    {
        return ParseUnitId(unitKey) > 0;
    }
}
