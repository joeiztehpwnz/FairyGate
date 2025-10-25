# Scene Setup Quick Start

## 🚀 Get Started in 30 Seconds

1. **In Unity**: `Combat → Complete Scene Setup → Testing Sandbox`
2. **Click "Create"**
3. **Press Play ▶️**
4. **Done!** You're ready to test combat

---

## 🎮 Essential Controls

### Combat
- **WASD** - Move
- **1-6** - Skills (Attack/Defense/Counter/Smash/Windmill/Ranged)
- **Space** - Cancel Skill
- **X** - Rest (stamina regen)

### Testing
- **F1-F6** - Force Enemy Skill
- **`[` `]`** or **PgUp/PgDn** - Change Equipment Set
- **`\`** or **Home** - Remove All Equipment
- **Esc** - Exit Combat

---

## 📦 What You Get

✅ Complete combat environment (ground, camera, lighting)
✅ Player (blue capsule) at (-3, 0, 0)
✅ Enemy (red capsule) at (3, 0, 0)
✅ Health/Stamina UI bars
✅ Equipment system with 4 preset builds
✅ Debug visualizers showing combat state
✅ All 6 skills ready to test
✅ TestRepeaterAI (cycles through skills automatically)

---

## 🔧 Quick Testing Workflows

### Test a Specific Interaction
1. Press Play
2. Use your skill (e.g., press 3 for Counter)
3. Press F1 to force enemy Attack
4. Watch the interaction resolve

### Try Different Equipment
1. Press Play
2. Press Esc (exit combat)
3. Press `]` (or PgDn) to change equipment
4. Press Tab to re-engage combat
5. Notice stat changes

### Learn AI Patterns
1. Use `Combat → Complete Scene Setup → Quick 1v1 Setup` instead
2. Press Play
3. Watch KnightAI's 8-second pattern
4. Find vulnerable windows
5. Practice counterattacking

---

## 📊 Equipment Sets (`[` `]` Brackets)

| Set | Strategy | Key Stats |
|-----|----------|-----------|
| **Fortress** | Tank | +30 HP, +15 Def, -1 Spd |
| **Windrunner** | Speed | +2 Spd, +5 Dex |
| **Wanderer** | Balanced | Moderate all |
| **Berserker** | Glass Cannon | +10 Str, -5 Def |

---

## 🤖 AI Types

### TestRepeaterAI (Testing Sandbox default)
- Cycles through all skills
- Perfect for systematic testing
- Override with F1-F6

### KnightAI (Quick 1v1 default)
- 8-second pattern cycle
- Teaches pattern recognition
- Vulnerable windows for counterattack

---

## ⚠️ Common Issues

**Bars not updating?**
→ Check Console for errors, bars are attached to character GameObjects

**Hotkeys not working?**
→ Ensure TestSkillSelector exists (TestingUI_Manager GameObject)

**Character falls through ground?**
→ Ensure Ground has Mesh Collider enabled

**Equipment won't change?**
→ Press Esc to exit combat first (equipment locked in combat)

---

## 🎯 Next Steps

1. ✅ Create scene with Testing Sandbox
2. ✅ Test all 6 skills (1-6 keys)
3. ✅ Force enemy skills (F1-F6) to test interactions
4. ✅ Try all equipment sets (`[` `]` brackets or PgUp/PgDn)
5. ✅ Review debug visualizer for combat info
6. ✅ Check Console for detailed logs

---

## 📖 Full Documentation

For detailed information, see:
- `SCENE_SETUP_GUIDE.md` - Complete setup guide
- `SKILL_TEST_ENVIRONMENT_USAGE.md` - Skill testing details
- `EQUIPMENT_SYSTEM_DESIGN.md` - Equipment system info

---

**Need to start fresh?**
`Combat → Complete Scene Setup → Clear All Combat Objects`

**Want simple 1v1?**
`Combat → Complete Scene Setup → Quick 1v1 Setup`
