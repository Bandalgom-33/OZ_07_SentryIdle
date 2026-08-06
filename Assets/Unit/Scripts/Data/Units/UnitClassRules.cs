using System.Collections.Generic;

namespace EndlessGuard.Unit.Data
{
    public static class UnitClassRules
    {
        private static readonly UnitSubclass[] NoneSubclasses =
        {
            UnitSubclass.None
        };

        private static readonly UnitSubclass[] VanguardSubclasses =
        {
            UnitSubclass.None,
            UnitSubclass.VanguardPioneer,
            UnitSubclass.VanguardCharger,
            UnitSubclass.VanguardStandardBearer,
            UnitSubclass.VanguardTactician,
            UnitSubclass.VanguardAgent,
            UnitSubclass.VanguardStrategist
        };

        private static readonly UnitSubclass[] GuardSubclasses =
        {
            UnitSubclass.None,
            UnitSubclass.GuardDreadnought,
            UnitSubclass.GuardFighter,
            UnitSubclass.GuardLord,
            UnitSubclass.GuardArtsFighter,
            UnitSubclass.GuardInstructor,
            UnitSubclass.GuardSoloBlade
        };

        private static readonly UnitSubclass[] DefenderSubclasses =
        {
            UnitSubclass.None,
            UnitSubclass.DefenderProtector,
            UnitSubclass.DefenderGuardian,
            UnitSubclass.DefenderJuggernaut,
            UnitSubclass.DefenderArtsProtector,
            UnitSubclass.DefenderDuelist,
            UnitSubclass.DefenderFortress
        };

        private static readonly UnitSubclass[] SupporterSubclasses =
        {
            UnitSubclass.None,
            UnitSubclass.SupporterSlower,
            UnitSubclass.SupporterShelterer,
            UnitSubclass.SupporterWeakener
        };

        private static readonly UnitSubclass[] SniperSubclasses =
        {
            UnitSubclass.None,
            UnitSubclass.SniperMarksman,
            UnitSubclass.SniperArtillery,
            UnitSubclass.SniperSharpshooter,
            UnitSubclass.SniperSiegeArcher
        };

        private static readonly UnitSubclass[] SpecialistSubclasses =
        {
            UnitSubclass.None,
            UnitSubclass.SpecialistMaster
        };

        public static IReadOnlyList<UnitSubclass> GetSubclasses(UnitClass unitClass)
        {
            switch (unitClass)
            {
                case UnitClass.Vanguard:
                    return VanguardSubclasses;

                case UnitClass.Guard:
                    return GuardSubclasses;

                case UnitClass.Defender:
                    return DefenderSubclasses;

                case UnitClass.Supporter:
                    return SupporterSubclasses;

                case UnitClass.Sniper:
                    return SniperSubclasses;

                case UnitClass.Specialist:
                    return SpecialistSubclasses;

                default:
                    return NoneSubclasses;
            }
        }

        public static bool IsSubclassAllowed(UnitClass unitClass, UnitSubclass subclass)
        {
            IReadOnlyList<UnitSubclass> subclasses = GetSubclasses(unitClass);

            for (int i = 0; i < subclasses.Count; i++)
            {
                if (subclasses[i] == subclass)
                {
                    return true;
                }
            }

            return false;
        }
    }
}