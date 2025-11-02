# RPH Tracker – Version History v1.53

## Purpose
Chronological changelog of all RPH Tracker versions up to v1.53a.  
Includes major milestones, bug fixes, and compatibility notes.

---

## 🧭 Version 1.0 – Project Genesis
**Date:** Initial prototype  
**Highlights:**
- Basic tracking of Rotors, Hinges, and Pistons.
- Single LCD output using fixed names.
- Displays angles in degrees and piston position in meters.
- Hardcoded update interval (0.5s).
- No subgrouping, no commands, no PARK safeguards.

**Status:** Proof of concept.

---

## ⚙️ Version 1.1 – Custom Data Integration
**Highlights:**
- Added `[RPH:Tracking]` section to Custom Data.
- Each mechanical part stores display name, subgroup, and flags.
- Implemented INI-safe writing method.
- First compatibility test with PARK (successful).

**Notes:** Established safe coexistence pattern for Custom Data management.

---

## 🧩 Version 1.2 – Group & LCD System
**Highlights:**
- Introduced block group “Tracked Parts” for easy management.
- LCDs auto-detected within same group.
- Added display name truncation and formatted LCD output.
- Added angle normalization and display units.

**Notes:** First multi-block scalable version.

---

## 🧮 Version 1.3 – Cleanup & Refresh Framework
**Highlights:**
- Added `refresh` and `cleanup` commands (via Run Argument).
- Implemented orphan cleanup detection and removal.
- Added silent vs verbose cleanup modes.
- Improved Custom Data writing performance.
- PARK recompile test confirmed RPH header persistence.

**Notes:** Identified SE group caching limitation (requires group recreation).

---

## 🧭 Version 1.4 – Phase 1 Completion
**Highlights:**
- Fully stable tracking pipeline.
- Verified PARK coexistence, INI safety, and non-destructive writing.
- Standardized variable naming and config structure.
- Introduced versioned header in script comments.

**Notes:** Marked as baseline for future phases.

---

## 🖥️ Version 1.5 – LCD Filtering & Sorting (Phase 2)
**Highlights:**
- Implemented `[RPH:Display]` section for LCDs.
- Added `rph_display_groups`, `rph_sort`, and `rph_header` parameters.
- Supported multiple LCDs with independent configurations.
- Added group-based filtering and multiple sorting modes.
- Added PB Custom Data summary `[RPH:Info]` with group list and timestamp.

**Notes:** Large milestone for usability and scalability.

---

## 🧭 Version 1.52 – PARK Audit & Echo Optimization
**Highlights:**
- Conducted full review of PARK script’s Custom Data behavior.
- Verified non-destructive coexistence.
- Removed redundant echo calls.
- Added PB diagnostics for clarity.
- Internal refactor for better grouping logic.

---

## ⚡ Version 1.53 – Refinement Pass
**Highlights:**
- Improved `rph_ignore` parser (INI-safe, PARK-safe).
- Alphabetical group registry and timestamp in PB Custom Data.
- LCD footer with group count and interval info.
- `[RPH:Commands]` list added to PB Custom Data.
- Consolidated all initialization and runtime echo handling.
- Added context preservation utilities and documentation package.

**Notes:** Final version of Phase 2. Project state: stable and production-ready.

---

## 🔧 Version 1.53a – Echo Fix + Stability
**Highlights:**
- Fixed echo flooding from LCD loop.
- Echo limited to init and command runs.
- Verified stable runtime and Custom Data integrity.

**Notes:** Marked as stable release and handoff point for next development phase (v1.6).

---

## 🧭 Planned v1.6 (Phase 3)
**Goals:**
- Implement `calibrate` command and persistent zero storage.
- Add velocity tracking (deg/s, m/s).
- Add PB configuration for update interval and group name.
- Introduce optional debug mode with toggleable overlay.

---

_Compiled: 2025-10-28_  
_Authors: Salty & GPT-5 Engineering Division_
