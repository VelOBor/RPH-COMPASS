# 🛰️ RPH Tracker — Development Roadmap v1.6
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
- [v] Full-grid orphan detection (Enhanced Cleanup)  
   • Scans entire grid for mechanical parts with stale RPH headers.  
- [v] Manual `cleanup` command  
   • Triggers grid-wide orphan purge on demand.  
- [v] Group cache behavior documented  
   • Requires delete/recreate group to force fresh membership.  
- [v] Code stable, no regressions, PARK compatibility confirmed.

---

## [v] Phase 2 — Sub-Group Display, Multi-LCD Support and Sorting (Completed)
### Objective  
Enable multiple LCDs to display different subsets of tracked parts based on each LCD’s Custom Data configuration, with optional sorting controls.

- [v] Design [RPH:Display] section for LCDs  
   • Keys: `rph_display_groups = tail, wing, engine`  
   • Optional: `rph_sort = name|group|type|order`  
- [v] Implement per-LCD filtering logic  
   • Only show parts whose `rph_sub_group` matches the LCD’s assigned groups.  
- [v] Multi-LCD update loop  
   • Each LCD in the main group renders its own filtered subset.  
- [v] Sorting options  
   • Default (group order), Alphabetical, Sub-Group, Part Type, or `rph_display_order`.  
- [v] LCD update optimization  
   • Skip rebuilding LCD text if data hasn’t changed.  
- [v] User documentation and examples for [RPH:Display].  
- [v] Final Phase 2 validation with echo fix and full stability tests (v1.53a).

---

## [⚙️] Phase 3 — Calibration and Velocity (In Progress)
### Objective  
Add zero-position calibration, velocity readouts, and direction flag support.

- [⚙️] `calibrate <part>` command to set zero positions.  
- [⚙️] Persist calibration values to [RPH:Tracking] across refresh and recompilation.  
- [⚙️] Compute angular / linear velocity per update.  
- [⚙️] Display current velocity alongside position (deg/s or m/s).  
- [⚙️] Respect `rph_reverse` for directional sign in both values.  
- [⚙️] Optional smoothing filter for velocity.

---

## [x] Phase 4 — Quality-of-Life and Graphical Options (Planned)
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
- ✅ Phase 1: Complete (foundation & maintenance systems)  
- ✅ Phase 2: Complete (LCD filtering, sorting, echo optimization)  
- 🔄 Phase 3: **In progress** – calibration + velocity implementation  
- ⏸️ Phase 4: Planned post-Phase 3 polish & graphical options

---

**Project Codename:** `RPH Tracker`  
**Author:** Salty & GPT-5 Engineering Division  
**Version:** v1.6  
**Status:** 🟢 Phase 3 active — calibration and velocity features in development

