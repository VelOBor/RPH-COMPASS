# RPH Tracker – Test Status v1.53

## Purpose
Comprehensive record of in-game test sessions, known results, and pending retests for the RPH Tracker project.

---

## ⚙️ Test Environment Summary
- **Game:** Space Engineers (stable branch, 2025 build)  
- **Testing Tools:** MDK-SE, Notepad++, In-game programmable block console  
- **Rig Configuration:**  
  - Cockpit + PB + multiple rotors, hinges, and pistons  
  - 1–3 LCDs in test group  
  - Additional PARK-controlled parts for coexistence testing  
- **Update Frequency:** 0.5 seconds (default)  

---

## ✅ Phase 1 – Core Functionality Tests

| Test | Description | Result | Notes |
|------|--------------|---------|-------|
| Initialization | PB echoes system summary correctly | ✅ | Works as intended |
| Group detection | Finds blocks in “Tracked Parts” group | ✅ | Requires existing group |
| LCD output | Displays all tracked parts | ✅ | LCD text clear and stable |
| Custom Data injection | Adds `[RPH:Tracking]` header | ✅ | Verified non-destructive |
| PARK compatibility | Coexists with `[PARK:Main]` and new profiles | ✅ | PARK appends only |
| Manual recompile | Restores function with no corruption | ✅ | Safe |
| Group membership change | Removing parts requires group recreation | ⚠️ | SE cache quirk |

**Status:** All core systems validated and stable.

---

## ✅ Phase 2 – LCD Filtering & Sorting

| Test | Description | Result | Notes |
|------|--------------|---------|-------|
| `[RPH:Display]` creation | Automatically populated on first run | ✅ | Verified |
| Group filtering | LCD displays selected sub-groups | ✅ | Works with comma-separated entries |
| Sorting | Default / name / type / group | ✅ | All functional |
| Multi-LCD support | Each LCD maintains independent config | ✅ | Verified with 3 LCDs |
| Refresh command | Rebuilds parts and LCD lists | ✅ | Works |
| Cleanup command | Removes `[RPH:Tracking]` from orphans | ✅ | Works when group deleted/recreated |
| PARK recompile | No data loss or interference | ✅ | Safe |
| rph_ignore flag | Works with `rph_ignore=true` and `rph_ignore=1` | ⚠️ | Strict syntax required |
| PB diagnostics | Group registry + timestamp in PB CD | ✅ | Stable |

**Status:** Fully operational, minor non-critical syntax sensitivity.

---

## 🧩 Known Issues (as of v1.53a)

1. **SE Group Cache Behavior**
   - Removing blocks from a group does not update membership live.
   - Workaround: delete and recreate group with same name.

2. **Custom Data Formatting Sensitivity**
   - Whitespaces in key names tolerated, but `rph_ignore` flag must match strict format (`rph_ignore=true` or `rph_ignore=1`).

3. **Echo Repetition (fixed in v1.53a)**
   - Echo spam issue resolved by limiting Echo output to init and command calls.

4. **Multi-LCD Loop Overwrites (resolved)**
   - Resolved by moving Echo() outside LCD loop.

---

## 🧠 Stress Testing

| Test | Configuration | Result |
|------|----------------|---------|
| 12 rotors + 12 pistons | Stable at 0.5s interval | ✅ |
| 32 pistons across 8 groups | Stable output | ✅ |
| 3 LCDs, mixed sorting | All independent displays OK | ✅ |
| PARK active control during tracking | Stable, no CD interference | ✅ |
| Manual CD edits | Script restores missing sections | ✅ |

---

## 🚀 Pending Tests (Phase 3)
- Calibration zero command (`calibrate <name>`).  
- Velocity calculation and display (`deg/s`, `m/s`).  
- Cross-group LCD consistency.  
- Extended command argument parser (multi-word support).  
- Verify hard reset logic once implemented.

---

_Compiled: 2025-10-28_  
_Authors: Salty & GPT-5 Engineering Division_
