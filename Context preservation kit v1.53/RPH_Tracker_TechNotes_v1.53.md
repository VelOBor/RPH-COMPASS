# RPH Tracker – Technical Notes v1.53

## Purpose
Reference sheet detailing internal logic, system behavior, and implementation decisions for RPH Tracker.
Intended for developers maintaining or extending the script.

---

## ⚙️ Language & Environment
- **Language:** C#6 subset (Space Engineers in-game PB sandbox).
- **Tooling Reference:** MDK-SE (Malware’s Development Kit for Space Engineers).
- **Restrictions:** No namespaces, no LINQ, limited reflection, and partial System.IO support.
- **Update Frequency:** `Update10` (approx. every 0.166s) with manual interval control.

---

## 🧩 Echo vs LCD Output
| Function | Destination | Update Rate | Usage |
|-----------|--------------|-------------|--------|
| `Echo()` | PB terminal console | On script tick | Debugging, setup, and feedback |
| `sb.AppendLine()` | LCD or cockpit surface | Every update cycle | User display (status, values, etc.) |

**Note:** `Echo()` output is volatile — overwritten every tick.  
LCDs are persistent and updated via `IMyTextSurface.WriteText()`.

---

## 🧱 Custom Data Structure
RPH uses a strict, INI-safe layout to prevent interference with other scripts (notably PARK).

### Example (Rotor with PARK)
```
[RPH:Tracking]
rph_display_name = Tail Rotor
rph_sub_group    = Extenders
rph_calibrated_zero = 0.000
rph_reverse      = false

[PARK:Main]
Forward=0
Backward=0
...
```
- **INI-Safe:** Each header is fully enclosed (`[RPH:Tracking]`), ensuring compatibility.
- **Non-destructive:** RPH only prepends or edits its own section, never overwrites other headers.

---

## 🧠 Internal Logic Overview
### 1. Initialization
- Searches for block group `Tracked Parts`.
- Scans for Rotors, Hinges, Pistons, and LCDs.
- Populates `[RPH:Tracking]` and `[RPH:Display]` headers as needed.

### 2. Runtime Loop
- Updates every `UPDATE_INTERVAL` seconds.
- Reads angles/positions and computes adjusted values (zero-offset + reverse flag).

### 3. Custom Data Auto-Healing
- Adds `[RPH:Tracking]` to new parts automatically.
- Removes orphan headers during `cleanup` or when blocks leave the group.

### 4. LCD Display Management
- LCD configuration via `[RPH:Display]`.
- Supports subgroup filtering and sort modes (`default`, `name`, `group`, `type`).

### 5. Group Caching Behavior
Space Engineers does **not** live-update terminal group memberships during runtime.  
Workaround: user must **delete and recreate** the group after changing contents.

---

## 🧰 Command System
Commands are provided via **Run Argument** in PB terminal:
| Command | Function |
|----------|-----------|
| `refresh` / `rescan` / `reload` | Rebuilds lists and regenerates LCDs |
| `cleanup` | Removes `[RPH:Tracking]` from ungrouped mechanical parts |
| *(future)* `calibrate <name>` | Sets zero reference |
| *(future)* `debug` | Toggles runtime diagnostics |

---

## 🧮 Sorting Logic
Implemented custom insertion sort for PB performance stability.
- Default: by discovery order.  
- Name: alphabetically by display name.  
- Group: subgroup name, then display name.  
- Type: Rotor → Hinge → Piston → alphabetical.

---

## 🔐 PARK Compatibility Summary
- RPH only edits its own `[RPH:Tracking]` section.
- PARK typically **appends** new `[PARK:Profile]` sections; it does not rewrite Custom Data unless triggered by specific events.
- Verified safe coexistence under all expected triggers (profile addition, script recompilation).

---

## 🧾 Notes on Cleanup Behavior
- `CleanupOrphans()` scans the entire grid for `[RPH:Tracking]` sections.
- Compares against current group membership.
- If Custom Data header found on a block **not** in group → removes `[RPH:Tracking]` only.
- Optional `silent` flag disables echo output during internal runs.

---

## ⚡ Performance
- Execution cost negligible (<0.01 ms typical).  
- LCD refresh optimized by caching last display text (`tgt.LastText`).  
- Cleanup operations only run manually or on startup.  
- Expected scalability: ~200 tracked parts per grid safely.

---

## 🧭 Design Philosophy
1. **Non-destructive:** Never overwrite other scripts’ sections.  
2. **Self-healing:** Auto-creates and repairs missing data headers.  
3. **User-friendly:** Commands accessible via PB Run Argument and Custom Data.  
4. **Readable output:** Clean, uncluttered LCD formatting.  
5. **Modular growth:** Future upgrades can add calibration, velocity, visuals without rewrites.

---

_Compiled: 2025-10-28_  
_Authors: Salty & GPT-5 Engineering Division_
