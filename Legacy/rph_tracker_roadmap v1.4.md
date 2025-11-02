# 🛰️ RPH Tracker — Development Roadmap
### Rotor / Piston / Hinge Mechanical Status System for Space Engineers

---

## 📘 Overview
A modular mechanical tracking system for Space Engineers’ Programmable Block environment (C#6 compatible).  
Designed to monitor **rotors**, **pistons**, and **hinges** with custom naming, grouping, calibration, and per-LCD filtering — all via **Custom Data**.  
Fully compatible with other in-game scripts (notably **PARK**, Steam ID `1933151026`).

---

## [v] Phase 1 — Modular Core and Data Safety (Completed)
### Objective  
Establish a stable, modular foundation for tracking mechanical parts (Rotors, Pistons, Hinges) with safe Custom Data handling, full PARK compatibility, and basic LCD reporting.

- [v] Create `TrackedPart` class  
   • Encapsulates all per-part information and behavior (meta + runtime).  
- [v] Refactor main loop to use TrackedPart objects  
   • Replaces raw block iteration with clean, maintainable structure.  
- [v] Safe RPH Custom Data module ([RPH:Tracking])  
   • Fully INI-compatible and PARK-safe.  
- [v] Automatic creation of missing [RPH:Tracking] headers  
   • Blocks self-initialize with defaults on first scan.  
- [v] Automatic and manual refresh commands  
   • `refresh` / `rescan` / `reload` implemented.  
- [v] Add `rph_ignore = true` flag  
   • Skips blocks marked for exclusion even if in group.  
- [v] Automatic orphan cleanup on compile and refresh  
   • Removes [RPH:Tracking] from blocks no longer in group.  
- [v] Full-grid orphan detection (v1.4 Enhanced Cleanup)  
   • Scans entire grid for mechanical parts with stale RPH headers.  
- [v] Manual `cleanup` command  
   • Triggers grid-wide orphan purge on demand.  
- [v] Group cache behavior documented  
   • Requires delete/recreate group to force fresh membership.  
- [x] Optional “hard cleanup” (`cleanup hard`) planned for later.  
- [v] Code stable, no regressions, PARK compatibility confirmed.

---

## [x] Phase 2 — Sub-Group Display, Multi-LCD Support and Sorting (Next)
### Objective  
Enable multiple LCDs to display different subsets of tracked parts based on each LCD’s Custom Data configuration, with optional sorting controls.

- [x] Design [RPH:Display] section for LCDs  
   • Keys: `rph_display_groups = tail, wing, engine`  
   • Optional: `rph_sort = name|group|type|order`  
- [x] Implement per-LCD filtering logic  
   • Only show parts whose `rph_sub_group` matches the LCD’s assigned groups.  
- [x] Multi-LCD update loop  
   • Each LCD in the main group renders its own filtered subset.  
- [x] Sorting options  
   • Default (group order), Alphabetical, Sub-Group, Part Type, or `rph_display_order`.  
- [x] LCD update optimization  
   • Skip rebuilding LCD text if data hasn’t changed.  
- [x] User documentation and examples for [RPH:Display].  
- [x] Final Phase 2 validation with multi-LCD test rig.

---

## [ ] Phase 3 — Calibration and Velocity (Readout Enhancements)
### Objective  
Add zero-position calibration, velocity readouts, and direction flag support.

- [x] `calibrate` command to set zero positions.  
- [x] Persist calibration values to [RPH:Tracking].  
- [x] Compute angular / linear velocity per update.  
- [x] Display current velocity alongside position.  
- [x] Respect `rph_reverse` for directional sign in both values.  
- [x] Optional smoothing filter for velocity.

---

## [ ] Phase 4 — Quality-of-Life and Graphical Options
### Objective  
Polish the user experience and introduce optional visual features.

- [x] ASCII progress bars for piston lengths and angles.  
- [x] Multi-page LCD views or rotating status screens.  
- [x] `export` / `import` commands for settings transfer between grids.  
- [x] Debug toggle and performance profiling readout.  
- [x] Optional graphical display mode (using text graphics).  
- [x] Hard cleanup (`cleanup hard`) for full wipe and re-init.  
- [x] Final code audit and public release build.

---

### Overall Status Summary
- ✅ Phase 1: Complete (stable foundation + maintenance systems)  
- 🔄 Phase 2: In progress – design stage under way  
- ⏸️ Phase 3: Pending post-Phase 2 validation  
- ⏸️ Phase 4: Future QOL and visual enhancements

---

**Project Codename:** `RPH Tracker`  
**Author:** Salty & GPT-5 Engineering Division  
**Status:** 🟢 Foundation complete — entering Phase 2 design and implementation