using UnityEngine.Events;

namespace FairyGate.Combat
{
    public interface ISkillExecutor
    {
        SkillExecutionState CurrentState { get; }
        SkillType CurrentSkill { get; }
        float ChargeProgress { get; }

        bool CanChargeSkill(SkillType skillType);
        bool CanExecuteSkill(SkillType skillType);

        void StartCharging(SkillType skillType);
        void ExecuteSkill(SkillType skillType);
        void CancelSkill();

        // Events - implemented as fields in concrete classes
    }
}