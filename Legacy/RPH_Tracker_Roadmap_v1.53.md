# RPH Tracker – Roadmap v1.53

## Version
**Current:** v1.53a (Echo Fix + Refinements)  
**Next Planned Increment:** v1.6 (Phase 3 – Calibration & Velocity Display)  
**Major Milestone:** v2.0 (Advanced Diagnostics & Visual Output)

---

## Phase Overview

| Phase | Description | Status | Notes |
|-------|--------------|--------|-------|
| 1 | **Base Functionality & Compatibility Layer**<br>• Detects and tracks all Rotors, Hinges, Pistons.<br>• Outputs to a designated LCD.<br>• Generates `[RPH:Tracking]` headers in Custom Data.<br>• Confirmed PARK compatibility and non-destructive CD parsing. | [v] | Stable and fully validated. |
| 2 | **LCD Subgroup Filtering + Sorting**<br>• LCDs can filter by subgroup via `[RPH:Display]`.<br>• Sorting by name, group, or type.<br>• Group registry output to PB Custom Data.<br>• Command system (`refresh`, `cleanup`). | [v] | Fully functional. Minor non-critical issues with SE group caching. |
| 3 | **Calibration + Velocity Tracking**<br>• Implement `calibrate` command to set zero reference.<br>• Display current movement velocity.<br>• Add direction-reversal flags. | [x] | Planned. |
| 4 | **Configurable Parameters**<br>• Move update interval and group name to PB Custom Data.<br>• Add validation and on-the-fly reload. | [x] | Planned. |
| 5 | **Diagnostics and Debug Tools**<br>• Optional debug overlay on LCD.<br>• PB echo summary toggle. | [x] | Planned. |
| 6 | **PARK Compatibility Audit**<br>• Confirm CD integrity and section coexistence. | [v] | Completed successfully. |
| 7 | **Polish & Optimization Pass**<br>• Add visual styling options (headers, dividers).<br>• Improve text alignment and layout. | [x] | Deferred to post-v1.6. |

---

## Versioning Scheme
- Major revisions (e.g., **v2.0**) introduce new systems or data formats.  
- Minor revisions (**v1.x**) introduce new features or functional phases.  
- Patch revisions (**v1.xa**) are for bug fixes or minor QoL improvements.

---

## Current Summary (v1.53a)
- Core functionality: ✅ Stable  
- PARK safety: ✅ Confirmed  
- Orphan cleanup: ✅ Works (requires group recreation if SE cache stalls)  
- Subgroup filtering: ✅ Stable  
- LCD auto-init: ✅ Stable  
- Echo optimization: ✅ Fixed (no more loop spam)  
- Custom Data diagnostics: ✅ Included  

---

## Next Objectives
- Begin Phase 3: `calibrate` and `velocity` implementation.  
- Add PB Custom Data for adjustable update interval.  
- Start documentation polish and in-game setup guide.

---

_Compiled: 2025-10-28_  
_Authors: Salty & GPT-5 Engineering Division_
