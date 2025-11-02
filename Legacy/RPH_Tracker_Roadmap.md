# 🛰️ RPH Tracker — Development Roadmap
### Rotor / Piston / Hinge Mechanical Status System for Space Engineers

---

## 📘 Overview
A modular mechanical tracking system for Space Engineers’ Programmable Block environment (C#6 compatible).  
Designed to monitor **rotors**, **pistons**, and **hinges** with custom naming, grouping, calibration, and per-LCD filtering — all via **Custom Data**.  
Fully compatible with other in-game scripts (notably **PARK**, Steam ID `1933151026`).

---

## [+] Phase 0 — Baseline (existing)
- [+] Single LCD output of all parts in a predefined group.  
- [+] Display angles (°) or positions (m).  
- [+] Fixed group & LCD name constants.  
- [+] Stable runtime loop, 0.5 s updates.  

---

## [x] Phase 1 — Modular Data Structure (Foundation)
- [x] Introduce `TrackedPart` struct/class containing:  
  - Block reference  
  - Display name (from Custom Data)  
  - Sub-group name  
  - Calibrated zero offset  
  - Reverse flag  
  - Part type (rotor / piston / hinge)
- [x] Implement parsing of Custom Data (`key: value` per line).  
- [x] Graceful fallback to defaults (safe if tags missing).  
- [x] Maintain legacy behavior when no Custom Data present.  

**Example Custom Data for a part:**
```
display name: Tail Piston Top
sub-group: Tail Extenders
calibrated zero: 0
reverse: false
```

---
## [x] Phase 2 — Sub-group Filtering, Multi-LCD Display, and Sorting Options
- [x] **Implement sub-group filtering**  
  Each LCD will read its assigned sub-groups from Custom Data and display only matching parts.
- [x] **Support multiple LCDs**  
  Each LCD in the group shows different subsets based on its Custom Data entries.
- [x] **Add sorting options for displayed parts**  
  Allow sorting by:
    - Group order (default)
    - Alphabetical (display name)
    - By sub-group
    - By part type (rotor/hinge/piston)
    - Optional `rph_display_order` numeric key in Custom Data.

---

## [x] Phase 3 — Calibration & Direction Control
- [x] Implement per-part `calibrated zero` offsets.  
- [x] Apply `reverse` flag to position and velocity calculations.  
- [x] Ensure output values reflect intuitive physical directions.  

---

## [x] Phase 4 — Velocity Tracking
- [x] Track previous readings (dictionary keyed by EntityId).  
- [x] Calculate velocity = delta / time.  
- [x] Apply calibration and reverse modifiers.  
- [x] Display both values on LCD:
  ```
  Tail Rotor A: 45.0° (1.25°/s)
  Tail Piston B: 1.75 m (0.05 m/s)
  ```

---

## [x] Phase 5 — Data Refresh & Optimization
- [x] Maintain accurate per-block elapsed time.  
- [x] Add **script update speed setting** to Custom Data (commented with valid values).  
  - Acceptable: `0`, `1`, `10`, `100`  
  - Example:
    ```
    update speed: 10   // Runs every 1/6 second (Update10)
    ```
- [x] Skip LCD refresh if output text unchanged.  
- [x] Gracefully degrade under heavy loads.  

---

## [v] Phase 6 — Compatibility & Safety (High Priority)
- [v] Confirmed through live testing that PARK preserves unknown INI sections.
- [v] RPH headers persist after PARK recompilation and new profile creation.
- [v] Auto-healing logic deemed optional; PARK updates are non-destructive.
- [v] Compatibility validation complete — RPH and PARK fully coexist.


---

## [x] Phase 7 — User Experience & Polish
- [x] Display startup summary:
  ```
  === RPH Tracker ===
  Tracking 12 parts across 3 LCDs
  ```
- [x] Report missing/misconfigured parts in PB Echo.  
- [x] Comprehensive inline comments & setup instructions.  
- [x] Final naming:
  ```
  RPH Tracker v1.0
  Rotor / Piston / Hinge Status System
  ```

---

## [x] Bonus / Experimental
- [x] ASCII progress bars or minimal graphics for pistons.  
- [x] Aggregate data views (min / max / avg per group).  
- [x] Custom formatting templates via Custom Data.  
- [x] Auto-discover LCDs tagged `[RPH]`.  

> ⚠️ **Performance Note:**  
> Visual text graphics will be tested only after core optimization is finalized.  
> They may impact runtime frequency and frame time, so will remain optional.

---

### 🧭 Development Priorities
1. Phase 6 — Compatibility & Custom Data safety (critical)
2. Phase 1 — Foundation & parsing structure
3. Phase 2 — Multi-LCD filtering
4. Phase 3 → 4 — Calibration & velocity
5. Phase 5 → 7 — Optimization, UX, final polish

---

**Project Codename:** `RPH Tracker`  
**Author:** Salty & GPT-5 Engineering Division  
**Status:** 🟢 Planning Complete — Ready for Phase 6 Compatibility Analysis
