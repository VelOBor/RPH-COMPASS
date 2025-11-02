# RPH Tracker – Corkboard v1.53

## Purpose
A living list of deferred ideas, experimental features, and future enhancements for the RPH Tracker project.
Items are not prioritized unless marked as `[HIGH]`.

---

## Deferred / Planned Features

### 🧩 1. Multi-Group Membership [MED]
Allow a single tracked part to belong to multiple sub-groups via a comma-separated list in Custom Data, e.g.:
```
rph_sub_group = tail, extenders
```
**Reason deferred:** Requires more complex LCD filtering logic and potentially new UI for managing overlapping groups.

---

### 🧮 2. Alphabetical Subgroup Display [LOW]
Currently, LCDs display parts in the order discovered within the group.  
Implement an optional alphabetically sorted output to improve readability in large setups.

**Dependencies:** Sorting mode expansion (Phase 2+).

---

### 🎨 3. LCD Visual Enhancements [LOW]
- Add configurable visual styles: borders, headers, ASCII dividers.
- Bold titles and section spacing for better readability.
- Optional monochrome “diagram” using text graphics.

**Reason deferred:** May increase script runtime cost, reserved for v2.0 optimization.

---

### 🧭 4. Debug Overlay Mode [MED]
A toggleable debug mode showing raw angles, update time, and internal data (PID-friendly).  
Would be activated via command: `debug on/off` or via PB Custom Data flag.

---

### ⚙️ 5. Configurable Update Speed [HIGH]
Move `UPDATE_INTERVAL` from code into PB Custom Data for user configuration.
Provide allowed values and safety limits (e.g., 0.1, 0.5, 1.0).

---

### 🧰 6. Hard Reset Command [MED]
Command: `hardreset`  
- Clears all `[RPH:Tracking]` sections from the grid.
- Forces full rebuild and LCD reinitialization.

---

### 📐 7. Persistent Calibration [HIGH]
Add ability to calibrate a zero position per part and persist it across script restarts.  
Command: `calibrate <display name>` or context-based calibration.

---

### ⚡ 8. Dynamic Velocity Display [HIGH]
Show current motion speed (deg/s or m/s).  
Useful for diagnosing mechanical desync or rotor/piston speed mismatches.

---

### 📊 9. Performance Monitor [LOW]
Add optional runtime statistics: instruction count, runtime time, refresh cycles/sec.

---

### 🔒 10. Safety & Data Integrity [ONGOING]
Ensure continued non-destructive coexistence with PARK and similar scripts.  
Add verification before writing to Custom Data.

---

_Compiled: 2025-10-28_  
_Authors: Salty & GPT-5 Engineering Division_
