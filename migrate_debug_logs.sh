#!/bin/bash

# Migration script for Debug.Log to CombatLogger
# Maps files to their appropriate CombatLogger categories

declare -A CATEGORY_MAP

# Core files - Combat category
CATEGORY_MAP["Assets/Scripts/Combat/Core/CombatInteractionManager.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/CombatController.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/CombatStateValidator.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/SkillInteractionResolver.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/SkillExecutionTracker.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/SkillExecution.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/CombatObjectPoolManager.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Core/SpeedConflictResolver.cs"]="Combat"

# Core files - General category
CATEGORY_MAP["Assets/Scripts/Combat/Core/CombatUpdateManager.cs"]="General"
CATEGORY_MAP["Assets/Scripts/Combat/Core/GameManager.cs"]="General"

# Core files - Movement category
CATEGORY_MAP["Assets/Scripts/Combat/Core/MovementController.cs"]="Movement"
CATEGORY_MAP["Assets/Scripts/Combat/Core/MovementArbitrator.cs"]="Movement"

# AI files
CATEGORY_MAP["Assets/Scripts/Combat/AI/AICoordinator.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Coordination/AttackCoordinator.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Coordination/FormationManager.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternCombatHandler.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternCondition.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternNode.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternMovementController.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternWeaponManager.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternDefinition.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/PatternExecutor.cs"]="AI"
CATEGORY_MAP["Assets/Scripts/Combat/AI/Patterns/Editor/PatternGenerator.cs"]="AI"

# Skill files
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/ApproachingState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/ChargedState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/AimingState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/RecoveryState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/ActiveState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/StartupState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/ChargingState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/WaitingState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/SkillStateMachine.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/SkillStateBase.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/States/UnchargedState.cs"]="Skills"
CATEGORY_MAP["Assets/Scripts/Combat/Skills/Base/SkillSystem.cs"]="Skills"

# Weapon files
CATEGORY_MAP["Assets/Scripts/Combat/Weapons/WeaponController.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Weapons/WeaponTrailController.cs"]="Combat"

# Systems files
CATEGORY_MAP["Assets/Scripts/Combat/Systems/HealthSystem.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Systems/StaminaSystem.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Systems/KnockdownMeterTracker.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Systems/AccuracySystem.cs"]="Combat"
CATEGORY_MAP["Assets/Scripts/Combat/Systems/CameraController.cs"]="General"

# StatusEffects files
CATEGORY_MAP["Assets/Scripts/Combat/StatusEffects/StatusEffectManager.cs"]="StatusEffects"

# UI files
CATEGORY_MAP["Assets/Scripts/Combat/UI/SkillIconDisplay.cs"]="UI"
CATEGORY_MAP["Assets/Scripts/Combat/UI/OutlineEffect.cs"]="UI"
CATEGORY_MAP["Assets/Scripts/Combat/UI/CharacterInfoDisplay.cs"]="UI"

# Equipment files
CATEGORY_MAP["Assets/Scripts/Combat/Equipment/EquipmentManager.cs"]="Combat"

# Function to migrate a single file
migrate_file() {
    local file="$1"
    local category="$2"

    if [ ! -f "$file" ]; then
        return
    fi

    # Skip if already migrated (check for CombatLogger usage)
    if grep -q "CombatLogger\.Log" "$file" 2>/dev/null && ! grep -q "Debug\.Log" "$file" 2>/dev/null; then
        echo "Skipping $file - already migrated"
        return
    fi

    # Create backup
    cp "$file" "$file.bak"

    # Perform replacements using sed
    # Note: These patterns preserve the exact message content

    # Replace Debug.LogError
    sed -i "s/Debug\.LogError(/CombatLogger.LogError(CombatLogger.Category.$category, /g" "$file"

    # Replace Debug.LogWarning
    sed -i "s/Debug\.LogWarning(/CombatLogger.LogWarning(CombatLogger.Category.$category, /g" "$file"

    # Replace Debug.Log (but not Debug.LogError or Debug.LogWarning)
    sed -i "s/\([^a-zA-Z]\)Debug\.Log(/\1CombatLogger.Log(CombatLogger.Category.$category, /g" "$file"

    # Handle line-beginning Debug.Log
    sed -i "s/^Debug\.Log(/CombatLogger.Log(CombatLogger.Category.$category, /g" "$file"

    echo "Migrated: $file -> Category.$category"
}

# Migrate all files
for file in "${!CATEGORY_MAP[@]}"; do
    migrate_file "$file" "${CATEGORY_MAP[$file]}"
done

echo "Migration complete!"
echo "Backup files created with .bak extension"
