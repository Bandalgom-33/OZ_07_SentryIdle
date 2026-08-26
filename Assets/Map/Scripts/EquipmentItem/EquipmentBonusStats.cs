public struct EquipmentBonusStats
{
    public int PhysicalAttack;
    public int MagicAttack;

    public int PhysicalDefense;
    public int MagicDefense;

    public float CriticalDamageBonus;
    public float Accuracy;

    //한번에 묶어서 쓰기
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