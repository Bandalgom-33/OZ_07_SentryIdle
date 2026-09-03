// 장비 아이템 종합 보너스 스탯 데이터 구조체
public struct EquipmentBonusStats
{
    public int PhysicalAttack;
    public int MagicAttack;

    public int PhysicalDefense;
    public int MagicDefense;

    public float CriticalDamageBonus;
    public float Accuracy;

    public bool HasAnyBonus => PhysicalAttack > 0 || MagicAttack > 0 ||
                               PhysicalDefense > 0 || MagicDefense > 0 ||
                               CriticalDamageBonus > 0f || Accuracy > 0f;

    // 장비 보너스 스탯 생성자
    public EquipmentBonusStats(
        int physicalAttack,
        int magicAttack,
        int physicalDefense,
        int magicDefense,
        float criticalDamageBonus,
        float accuracy)
    {
        PhysicalAttack = physicalAttack;
        MagicAttack = magicAttack;

        PhysicalDefense = physicalDefense;
        MagicDefense = magicDefense;

        CriticalDamageBonus = criticalDamageBonus;
        Accuracy = accuracy;
    }
}