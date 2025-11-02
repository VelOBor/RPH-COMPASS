# RPH Tracker Roadmap v1.6e (Full Consolidated Edition)
Author: Salty & GPT-5 Engineering Division

---

## [✅] Phase 1 — Core Architecture and Detection Logic
### Objective
Establish the fundamental detection and management system for the RPH (Rotor–Piston–Hinge) tracking suite.  
Lay down the foundation for modular development and clean INI-style Custom Data structures.

- [✅] Block group detection by name (`Tracked Parts` group).  
- [✅] Rotor, hinge, and piston detection through common interfaces.  
- [✅] Custom Data header insertion (`[RPH:Tracking]`) for mechanical parts.  
- [✅] Implement robust INI-like key-value parsing.  
- [✅] Add safe Custom Data write operation (does not overwrite other sections).  
- [✅] Add PB Custom Data `[RPH:Info]` summary and `[RPH:Commands]` list.  
- [✅] Multi-block group support stub (future expansion placeholder).  
- [✅] Basic LCD detection via `IMyTextSurfaceProvider`.  
- [✅] Safe handling for non-functional or closed blocks.  
- [✅] Verified core grid-safe logic.

---

## [✅] Phase 2 — Display & Data Output System
### Objective
Create user-facing data presentation via LCD panels and prepare for configurable output.

- [✅] LCD grouping and assignment through `[RPH:Display]` section.  
- [✅] Filtering by subgroup name using `rph_display_groups` key.  
- [✅] Sorting modes: `name`, `group`, `type`, `default`.  
- [✅] Display formatting for part type and current position.  
- [✅] Footer info showing totals and calibration state.  
- [✅] Header and layout consistent across multiple LCDs.  
- [✅] Compatibility with PB internal display (runtime echo).  
- [✅] ASCII-only text rendering for maximum SE compatibility.  
- [✅] Phase completed and verified in full game environment.

---

## [✅] Phase 3 — Calibration, Safety & User Guidance
### Objective
Finalize calibration logic, introduce Custom Data persistence, and implement clear user feedback for maintenance and safety operations.

- [✅] Add `calibrate_all` command – records zero positions for all uncalibrated parts.  
- [✅] Add `rph_is_calibrated` explicit flag – ensures deterministic calibration state.  
- [✅] Split `refresh` (read-only) vs. `rebuild_cd` (write/reset) commands.  
- [✅] Preserve PARK data – script never overwrites non-RPH sections.  
- [✅] Heads-up warnings for missing headers – visible after `refresh`.  
- [✅] PB Custom Data auto-population with `[RPH:Commands]` + sample header.  
- [✅] Full ASCII cleanup for all user-facing strings.  
- [✅] Confirmed persistence through recompilation; no data loss.  
- [✅] Manual CD clear recognized as deliberate reset (safe re-init).  
- [✅] Phase 3d validated – stable and ready for interface upgrades.

---

## [⚙️] Phase 3e — Interface Enhancements (Approved / In Progress)
### Objective
Improve readability and in-game usability through display customization and developer-friendly diagnostics.

- [⚙️] **Compact LCD Mode**  
  • New key: `rph_mode = compact` (per-LCD toggle).  
  • Displays condensed one-line summaries:  
    `RotorA +15.2° | HingeB –3.8° | PistonC 1.02 m`  
  • Suppresses headers/footers for minimal footprint.

- [⚙️] **Debug Mode & Diagnostics**  
  • Commands: `debug_on` / `debug_off`.  
  • Optional `[RPH:Display]` key `rph_debug = true` for per-LCD override.  
  • Dedicated **tagged LCD output** showing runtime stats, timing, orphan count, calibration state, and part details.  
  • `debug_mode` persisted under `[RPH:Info]`.

- [⚙️] Maintain strict INI-compatibility and PARK-safety.  
- [⚙️] Implement incremental testing for compact render and debug output.  
- [⚙️] Document new configuration keys and usage in future phase notes.

---

## [🧭] Phase 4 — QoL and Graphical Enhancements (Planned)
### Objective
Introduce optional quality-of-life improvements, more flexible formatting, and possible visual elements for advanced LCDs.

- [🧭] Multi-surface display splitting (per-LCD page selection).  
- [🧭] User-selectable units (deg/deg·s/m).  
- [🧭] Color-coded text for calibration/alert states.  
- [🧭] Adjustable update frequency via Custom Data key.  
- [🧭] Export/import configuration presets.  
- [🧭] Optional summary line toggle for single-screen dashboards.  
- [🧭] Investigate graphical LCD modes (progress bars, angle arcs, etc.).  
- [🧭] Continue performance profiling under high part counts.

---

### Overall Status Summary
- ✅ Phase 1 – Foundation complete  
- ✅ Phase 2 – LCD filtering & sorting complete  
- ✅ Phase 3 – Calibration & Safety complete (v1.6d)  
- 🔄 Phase 3e – Interface Enhancements in progress  
- ⏸️ Phase 4 – QoL and graphical features upcoming

---

**Project Codename:** RPH Tracker  
**Version:** v1.6e (Full Roadmap Consolidation)  
**Status:** 🟢 Stable Core + UI/Debug Enhancements Approved
