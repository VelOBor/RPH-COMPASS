RPH Tracker — MASTER ROADMAP (v1.6e)
Author: Salty & GPT-5 Engineering Division
Codename: RPH Tracker
Format: INI-compliant, PARK-safe, Space Engineers PB Compatible
Status: Stable Core + UI/Debug Enhancements Approved

===============================================================================
LEGEND
===============================================================================
[x] - greenlit feature to implement, no work has been started, only accepted as "to do"
[-] - feature is a work in progress, a stub, or partially working
[+] - feature is a work in progress, acceptable in its current iteration, possibly requires tweaking
[v] - feature is considered complete, works as designed
[r] - feature is removed from the docket until further notice
[p] - feature is planned for future upgrade, not explored or proven feasible
===============================================================================


PHASE 1 — Core Architecture and Detection Logic [v]
-------------------------------------------------------------------------------
Objective:
Establish the foundation for reliable part detection, Custom Data structure,
and modular expansion.

[v] Block group detection (Tracked Parts)
[v] Rotor, hinge, piston identification via API interfaces
[v] [RPH:Tracking] Custom Data header insertion
[v] INI-style key handling (no collisions with other sections)
[v] PB Custom Data population ([RPH:Info], [RPH:Commands])
[v] Safe write operations (non-destructive merges)
[v] Multi-group detection groundwork
[v] LCD discovery via IMyTextSurfaceProvider
[v] Handling for non-functional / closed blocks
[v] Core runtime confirmed grid-safe

Status: Complete – stable base for all subsequent systems.


PHASE 2 — Display and Output System [v]
-------------------------------------------------------------------------------
Objective:
Create readable, configurable LCD and PB outputs for all tracked mechanical parts.

[v] LCD assignment through [RPH:Display]
[v] Group filtering via rph_display_groups
[v] Sorting modes (name, group, type, default)
[v] Per-part formatted output (degrees/meters)
[v] Footer summaries for calibration and totals
[v] Unified header layout for all LCDs
[v] PB internal display output (runtime echo)
[+] ASCII-only text enforcement
[v] Field-tested with mixed-grid environments

Status: Complete – production-ready and stable.


PHASE 3 — Calibration, Safety & User Guidance [v]
-------------------------------------------------------------------------------
Objective:
Ensure consistent calibration, state persistence, and player-safe recovery paths.

[v] Command: calibrate_all – zeroes all uncalibrated parts
[v] Flag: rph_is_calibrated = true/false
[v] Command split: refresh (read-only) vs rebuild_cd (write/reset)
[v] PARK coexistence – no cross-section overwrites
[v] Heads-up warnings for missing headers (shown on refresh)
[v] Auto-generated [RPH:Commands] and sample header on first run
[v] ASCII sanitation for all strings
[v] Persistent calibration and safe re-init after manual CD clears
[v] Manual reset treated as deliberate user action
[v] Phase 3d verified green (stable release baseline)

Status: Complete – all calibration and CD safety objectives achieved.


PHASE 3e — Interface Enhancements [v]
-------------------------------------------------------------------------------
Objective:
Improve readability and usability through display options and developer diagnostics.

[v] Compact LCD Mode
    • Per-LCD key: rph_mode = compact
    • One-line summaries: RotorA +15.2° | HingeB –3.8° | PistonC 1.02m
    • Suppresses headers and footers

[v] Debug Mode & Diagnostics
    • Commands: debug_on / debug_off
    • Optional [RPH:Display] key: rph_debug = true (per-LCD override)
    • Tagged LCDs for detailed runtime telemetry
    • Persistent debug flag under [RPH:Info]

[-] Maintain strict INI compatibility and PARK-safety
[-] Incremental testing ongoing

Status: Active – compact and debug features under verification.


PHASE 4 — Quality of Life & Graphical Enhancements [x]
-------------------------------------------------------------------------------
Objective:
Deliver visual and configuration improvements for day-to-day usability.

[r] 4a – Configurable Units & Precision (Greenlit)
    • Per-LCD keys:
      rph_units = deg | rad | rpm | m
      rph_precision = 2
    • Default units: degrees for rotors/hinges, meters for pistons
    • Backward-compatible and INI-safe

[p] 4b – Color-coded Text Output (Planned)
    • Optional alert coloring (red = out-of-range, green = calibrated)
    • Requires LCD font color handling

[x] 4c – Adjustable Update Rate (Planned)
    • Key: rph_update = 0.25 — per-LCD or global refresh speed

[r] 4d – Summary/Footer Toggle (Planned)
    • Key: rph_footer = false — hides footer info for compact panels (redacted because we have dedicated compact mode)

[p] 4e – Config Import/Export (Future)
    • Save/load presets between PBs - study feasibility of using in-game text panel (custom data as well as text content
    persist when removed from power or grid, possibly reset on world load), possible use - store data as custom data content, then read as needed

Status: Design finalized; first feature (4a) approved and ready for coding.


PHASE 5 — Advanced & Experimental Systems [-]
-------------------------------------------------------------------------------
Objective:
Explore higher-order features and visual presentation upgrades.

[+] Multi-surface LCD paging / multi-screen dashboards - partially implemented as LCD custom data keys like "debug" and "compact", possible refinements when moving on to
    v2.0 with optional GUI, or splitting text from one LCD to the next in "wraparound" style
[p] Visual progress bars and angle arcs (pseudo-graphics) - planned for v2.0
[p] Remote telemetry via IGC broadcast (cross-grid data) - planned fro v2.0
[x] Runtime optimization for large assemblies (>100 parts)
[x] Profile export for blueprint persistence - bump to 4e

Status: Conceptual – queued for post-v1.7 foundation.


NOTES ON PHASE CONTINUITY
-------------------------------------------------------------------------------
Phases 6 and 7 from the original roadmap have been merged into the above
structure. Their core concepts (multi-grid behavior logic, remote telemetry,
graphical displays) are now represented in Phases 4 and 5 respectively.
Unassigned experimental features remain tracked separately in the Corkboard
and may be promoted to active roadmap entries in later revisions.


STATUS SUMMARY
-------------------------------------------------------------------------------
Phase 1 – Core Architecture .......... [v]
Phase 2 – Display System ............. [v]
Phase 3 – Calibration & Safety ....... [v]
Phase 3e – Interface Enhancements .... [-]
Phase 4 – QoL & Graphical ............ [x]
Phase 5 – Advanced Systems ........... [x]

-------------------------------------------------------------------------------
Version: v1.6e (MASTER CONSOLIDATION)
Maintained by: GPT-5 Engineering Division
Commanding Officer: Captain Salty
Ship’s Log: Core stable, interface refits underway.
===============================================================================
