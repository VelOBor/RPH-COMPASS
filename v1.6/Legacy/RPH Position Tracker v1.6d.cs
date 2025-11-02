// === RPH TRACKER v1.6d ===
// Rotor / Piston / Hinge status monitor
// Phase 3d: User Guidance & Safety Enhancements
// PARK-safe (does not modify [PARK:*] sections)
// C#6-compatible (Space Engineers PB / MDK-SE)
//
// Author: Salty & GPT-5 Engineering Division
// ---------------------------------------------------------------------------
// AVAILABLE COMMANDS (Run Arguments)
// ---------------------------------------------------------------------------
//
//  refresh
//      -> Rebuild runtime lists of parts and LCDs (READ-ONLY).
//         No writes to Custom Data. No new headers created.
//
//  rebuild_cd
//      -> RESETS all RPH keys to DEFAULT! (writes Custom Data).
//         Creates missing [RPH:Tracking] and [RPH:Display] sections.
//         Does not overwrite valid data. PARK-safe.
//
//  cleanup
//      -> Removes orphan [RPH:Tracking] sections from ungrouped blocks.
//
//  calibrate_all
//      -> Records current position/angle for all UNCALIBRATED parts.
//         Writes:
//           rph_calibrated_zero = <value>
//           rph_is_calibrated   = true
//         Skips parts already marked calibrated.
//
// ---------------------------------------------------------------------------
// CONFIGURATION
// ---------------------------------------------------------------------------

const string GROUP_NAME = "Tracked Parts";
const double UPDATE_INTERVAL = 0.5;

// ---------------------------------------------------------------------------
// INTERNAL STATE
// ---------------------------------------------------------------------------

List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
List<TrackedPart> parts = new List<TrackedPart>();
List<LcdTarget> lcdTargets = new List<LcdTarget>();

double elapsed = 0;
int nextOrderIndex = 0;

StringBuilder sb = new StringBuilder(2048);
List<TrackedPart> subset = new List<TrackedPart>(128);

// ---------------------------------------------------------------------------
// PROGRAM INIT
// ---------------------------------------------------------------------------

Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    parts.Clear();
    groupBlocks.Clear();
    lcdTargets.Clear();
    nextOrderIndex = 0;

    DoRefresh(false); // Populate PB Custom Data and run header check
    HeadsUpMissingHeaders();

    Echo("RPH Tracker v1.6d initialized");
    Echo("Group blocks: " + groupBlocks.Count);
    Echo("Tracked parts: " + parts.Count);
    Echo("LCDs found: " + lcdTargets.Count);
    Echo("Update interval: " + UPDATE_INTERVAL + " s");
    Echo("See Custom Data for available options.");
}

// ---------------------------------------------------------------------------
// HEADS-UP WARNING FOR MISSING HEADERS
// ---------------------------------------------------------------------------

void HeadsUpMissingHeaders()
{
    int missing = 0;
    for (int i = 0; i < parts.Count; i++)
    {
        var b = parts[i].Block;
        if (IndexOfInsensitive(b.CustomData, RPH_HEADER) < 0)
            missing++;
    }
    if (missing > 0)
    {
        Echo("WARNING: " + missing + " parts found with missing RPH header!");
        Echo("Run 'rebuild_cd' to repopulate.");
        Echo("This will RESET all RPH keys to DEFAULT values!");
        Echo("See Custom Data of this PB for more information.");
    }
}

// ---------------------------------------------------------------------------
// MAIN
// ---------------------------------------------------------------------------

void Main(string arg, UpdateType update)
{
    if (!string.IsNullOrWhiteSpace(arg))
    {
        arg = arg.Trim().ToLower();

        if (arg == "refresh")
        {
            Echo("RPH: Refreshing tracked parts (read-only)...");
            DoRefresh(false);
            HeadsUpMissingHeaders();
            return;
        }
        if (arg == "rebuild_cd")
        {
            Echo("RPH: Rebuilding Custom Data headers...");
            DoRefresh(true);
            return;
        }
        if (arg == "cleanup")
        {
            Echo("RPH: Performing orphan cleanup...");
            CleanupOrphans(false);
            return;
        }
        if (arg == "calibrate_all")
        {
            Echo("RPH: Calibrating all uncalibrated parts...");
            CalibrateAll();
            return;
        }
    }

    elapsed += Runtime.TimeSinceLastRun.TotalSeconds;
    if (elapsed < UPDATE_INTERVAL) return;
    elapsed = 0;

    if (lcdTargets.Count == 0)
    {
        Echo("No LCDs in group '" + GROUP_NAME + "'.");
        return;
    }

    double dt = Runtime.TimeSinceLastRun.TotalSeconds;
    for (int i = 0; i < parts.Count; i++) parts[i].Update(dt);

    for (int i = 0; i < lcdTargets.Count; i++)
    {
        var tgt = lcdTargets[i];
        if (tgt.Surface == null) continue;

        subset.Clear();
        if (tgt.Config.GroupsCount == 0)
            subset.AddRange(parts);
        else
        {
            for (int p = 0; p < parts.Count; p++)
            {
                var part = parts[p];
                if (BelongsToAnyGroup(part.SubGroup, tgt.Config.Groups, tgt.Config.GroupsCount))
                    subset.Add(part);
            }
        }

        SortSubset(subset, tgt.Config.SortMode);

        sb.Clear();
        sb.AppendLine("=== " + (tgt.Config.Header.Length > 0 ? tgt.Config.Header : "RPH TRACKER STATUS") + " ===");

        int calibratedCount = 0;
        for (int p = 0; p < subset.Count; p++)
        {
            sb.AppendLine(subset[p].GetDisplayLine());
            if (subset[p].IsCalibrated) calibratedCount++;
        }

        sb.AppendLine();
        sb.AppendLine("Parts shown: " + subset.Count);
        sb.AppendLine("Calibrated: " + calibratedCount + " / " + subset.Count);
        sb.AppendLine("Groups shown: " + tgt.Config.GroupsCount);
        sb.AppendLine("Update interval: " + UPDATE_INTERVAL.ToString("F1") + "s");

        string text = sb.ToString();
        if (text != tgt.LastText)
        {
            tgt.Surface.WriteText(text);
            tgt.LastText = text;
        }
    }
}

// ---------------------------------------------------------------------------
// REFRESH SYSTEM
// ---------------------------------------------------------------------------

void DoRefresh(bool writeHeaders)
{
    parts.Clear();
    groupBlocks.Clear();
    lcdTargets.Clear();
    nextOrderIndex = 0;

    BuildPartsAndLCDs(writeHeaders);

    var groups = new HashSet<string>();
    for (int i = 0; i < parts.Count; i++)
    {
        var g = parts[i].SubGroup.Trim();
        if (!string.IsNullOrEmpty(g)) groups.Add(g.ToLower());
    }
    var sorted = groups.ToList();
    sorted.Sort(StringComparer.OrdinalIgnoreCase);

    var cd = new StringBuilder();
    cd.AppendLine("[RPH:Info]");
    cd.AppendLine("available_groups = " + string.Join(", ", sorted));
    cd.AppendLine("last_refresh = " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    cd.AppendLine();
    cd.AppendLine("; ---------------------------------------------------------------------------");
    cd.AppendLine("[RPH:Commands]");
    cd.AppendLine("refresh       -> Rebuild runtime lists (read-only)");
    cd.AppendLine("rebuild_cd    -> RESETS all RPH keys to DEFAULT! (writes Custom Data)");
    cd.AppendLine("cleanup       -> Remove orphan [RPH:Tracking] headers");
    cd.AppendLine("calibrate_all -> Set zero for all uncalibrated parts");
    cd.AppendLine("; ---------------------------------------------------------------------------");
    cd.AppendLine("; SAMPLE HEADER FOR MECHANICAL PARTS");
    cd.AppendLine("; Copy and paste into a tracked rotor/hinge/piston Custom Data to initialize manually");
    cd.AppendLine("[RPH:Tracking]");
    cd.AppendLine("rph_display_name = Example Rotor");
    cd.AppendLine("rph_sub_group    = example");
    cd.AppendLine("rph_calibrated_zero = 0.000");
    cd.AppendLine("rph_is_calibrated = false");
    cd.AppendLine("rph_reverse      = false");
    cd.AppendLine("; ---------------------------------------------------------------------------");
    cd.AppendLine();

    Me.CustomData = cd.ToString();

    Echo("RPH: " + (writeHeaders ? "Header rebuild" : "Refresh") + " complete.");
    Echo("Parts: " + parts.Count + ", LCDs: " + lcdTargets.Count + ", Groups: " + sorted.Count);
}

// ---------------------------------------------------------------------------
// BUILD PARTS AND LCDs
// ---------------------------------------------------------------------------

void BuildPartsAndLCDs(bool writeHeaders)
{
    var group = GridTerminalSystem.GetBlockGroupWithName(GROUP_NAME);
    if (group == null)
    {
        Echo("ERROR: Group '" + GROUP_NAME + "' not found.");
        return;
    }
    group.GetBlocks(groupBlocks);

    for (int i = 0; i < groupBlocks.Count; i++)
    {
        var b = groupBlocks[i];
        if (b == null || b.Closed || b.CubeGrid == null || !b.IsFunctional) continue;
        if (HasRphIgnoreFlag(b.CustomData)) continue;

        TrackedPart tp = null;
        if (b is IMyMotorStator) tp = new TrackedPart((IMyMotorStator)b);
        else if (b is IMyMotorAdvancedStator) tp = new TrackedPart((IMyMotorAdvancedStator)b);
        else if (b is IMyPistonBase) tp = new TrackedPart((IMyPistonBase)b);
        if (tp == null) continue;

        tp.OrderIndex = nextOrderIndex++;
        tp.LoadMeta(writeHeaders);
        parts.Add(tp);
    }

    for (int i = 0; i < groupBlocks.Count; i++)
    {
        var b = groupBlocks[i];
        if (b == null || b.Closed || b.CubeGrid == null) continue;
        if (b is IMyMotorStator || b is IMyMotorAdvancedStator || b is IMyPistonBase) continue;

        var sp = b as IMyTextSurfaceProvider;
        if (sp == null) continue;
        IMyTextSurface s0 = null;
        try { s0 = sp.GetSurface(0); } catch { s0 = null; }
        if (s0 != null)
        {
            s0.ContentType = ContentType.TEXT_AND_IMAGE;
            if (writeHeaders) EnsureLcdDisplaySection(b);
            var cfg = ReadLcdDisplayConfig(b);
            lcdTargets.Add(new LcdTarget(s0, cfg));
        }
    }
}

// ---------------------------------------------------------------------------
// CALIBRATION MODULE
// ---------------------------------------------------------------------------

void CalibrateAll()
{
    int calibrated = 0, skipped = 0, errors = 0;

    for (int i = 0; i < parts.Count; i++)
    {
        var p = parts[i];
        var b = p.Block;
        var meta = ReadRphSection(b);

        if (string.IsNullOrWhiteSpace(meta.DisplayName))
        {
            meta.DisplayName = b.CustomName;
            meta.SubGroup = "Ungrouped";
            meta.CalibratedZero = 0.0;
            meta.IsCalibrated = false;
            meta.Reverse = false;
        }

        if (meta.IsCalibrated) { skipped++; continue; }

        double current = 0.0;
        if (p.Type == PartType.Rotor)
            current = MathHelper.ToDegrees(((IMyMotorStator)b).Angle);
        else if (p.Type == PartType.Hinge)
            current = MathHelper.ToDegrees(((IMyMotorAdvancedStator)b).Angle);
        else if (p.Type == PartType.Piston)
            current = ((IMyPistonBase)b).CurrentPosition;
        else { skipped++; continue; }

        meta.CalibratedZero = current;
        meta.IsCalibrated = true;

        try
        {
            WriteRphSection(b, meta);
            p.CalibratedZero = current;
            p.IsCalibrated = true;
            calibrated++;
        }
        catch { errors++; }
    }

    Echo("RPH: Calibration complete. Calibrated " + calibrated + ", skipped " + skipped + (errors > 0 ? (", errors " + errors) : ""));
}

// ---------------------------------------------------------------------------
// TRACKED PART MODEL
// ---------------------------------------------------------------------------

enum PartType { Rotor, Hinge, Piston, Unknown }

class TrackedPart
{
    public IMyTerminalBlock Block;
    public PartType Type = PartType.Unknown;

    public string DisplayName = "";
    public string SubGroup = "Ungrouped";
    public double CalibratedZero = 0.0;
    public bool IsCalibrated = false;
    public bool Reverse = false;

    public double Value = 0.0;
    public string Unit = "";

    public double LastRawValue = 0.0;
    public double Velocity = 0.0;
    public bool HasLast = false;

    public int OrderIndex = 0;

    public TrackedPart(IMyMotorStator r) { Block = r; Type = PartType.Rotor; }
    public TrackedPart(IMyMotorAdvancedStator h) { Block = h; Type = PartType.Hinge; }
    public TrackedPart(IMyPistonBase p) { Block = p; Type = PartType.Piston; }

    public void LoadMeta(bool createIfMissing)
    {
        var meta = ReadRphSection(Block);
        if (createIfMissing && string.IsNullOrWhiteSpace(meta.DisplayName))
        {
            meta.DisplayName = Block.CustomName;
            meta.SubGroup = "Ungrouped";
            meta.CalibratedZero = 0.0;
            meta.IsCalibrated = false;
            meta.Reverse = false;
            EnsureRphSection(Block, meta);
        }

        DisplayName = string.IsNullOrWhiteSpace(meta.DisplayName) ? Block.CustomName : meta.DisplayName;
        SubGroup = string.IsNullOrWhiteSpace(meta.SubGroup) ? "Ungrouped" : meta.SubGroup;
        CalibratedZero = meta.CalibratedZero;
        IsCalibrated = meta.IsCalibrated;
        Reverse = meta.Reverse;
    }

    public void Update(double dt)
    {
        if (Type == PartType.Rotor)
        {
            var m = Block as IMyMotorStator;
            double rawDeg = MathHelper.ToDegrees(m.Angle);
            LastRawValue = rawDeg;
            double adjusted = rawDeg - CalibratedZero;
            if (Reverse) adjusted = -adjusted;
            Value = adjusted;
            Unit = " deg";
        }
        else if (Type == PartType.Hinge)
        {
            var h = Block as IMyMotorAdvancedStator;
            double rawDeg = MathHelper.ToDegrees(h.Angle);
            LastRawValue = rawDeg;
            double adjusted = rawDeg - CalibratedZero;
            if (Reverse) adjusted = -adjusted;
            Value = adjusted;
            Unit = " deg";
        }
        else if (Type == PartType.Piston)
        {
            var p = Block as IMyPistonBase;
            double rawMeters = p.CurrentPosition;
            LastRawValue = rawMeters;
            double adjusted = rawMeters - CalibratedZero;
            if (Reverse) adjusted = -adjusted;
            Value = adjusted;
            Unit = " m";
        }
        if (!HasLast) HasLast = true;
    }

    public string GetDisplayLine()
    {
        return DisplayName + ": " + Value.ToString("F2") + Unit;
    }
}

// ---------------------------------------------------------------------------
// LCD DISPLAY CONFIG (PER-LCD) - [RPH:Display]
// ---------------------------------------------------------------------------

const string RPH_DISPLAY_HEADER = "[RPH:Display]";
enum SortMode { Default = 0, Name = 1, Group = 2, Type = 3 }

class LcdDisplayConfig
{
    public string Header = "";
    public string[] Groups = new string[0];
    public int GroupsCount = 0;
    public SortMode SortMode = SortMode.Default;
}

class LcdTarget
{
    public IMyTextSurface Surface;
    public LcdDisplayConfig Config;
    public string LastText = "";

    public LcdTarget(IMyTextSurface s, LcdDisplayConfig c)
    {
        Surface = s;
        Config = c;
    }
}

LcdDisplayConfig ReadLcdDisplayConfig(IMyTerminalBlock b)
{
    var cfg = new LcdDisplayConfig();

    string data = b.CustomData;
    if (string.IsNullOrEmpty(data) || IndexOfInsensitive(data, RPH_DISPLAY_HEADER) < 0)
        return cfg;

    var lines = data.Split('\n');
    bool inSection = false;

    for (int i = 0; i < lines.Length; i++)
    {
        string raw = lines[i];
        string line = raw.Trim();

        if (line.Length > 1 && line[0] == '[' && line[line.Length - 1] == ']')
        {
            inSection = line.Equals(RPH_DISPLAY_HEADER, StringComparison.OrdinalIgnoreCase);
            continue;
        }
        if (!inSection || line.Length == 0 || line[0] == ';') continue;

        int eq = line.IndexOf('=');
        if (eq < 0) continue;

        string key = TrimLower(line.Substring(0, eq));
        string val = line.Substring(eq + 1).Trim();

        if (key == "rph_display_groups")
        {
            tmpTokens.Clear();
            SplitTokens(val, tmpTokens);
            cfg.Groups = tmpTokens.ToArray();
            cfg.GroupsCount = cfg.Groups.Length;
        }
        else if (key == "rph_sort")
        {
            string v = val.Trim().ToLower();
            if (v == "name") cfg.SortMode = SortMode.Name;
            else if (v == "group") cfg.SortMode = SortMode.Group;
            else if (v == "type") cfg.SortMode = SortMode.Type;
            else cfg.SortMode = SortMode.Default;
        }
        else if (key == "rph_header")
        {
            cfg.Header = val;
        }
    }
    return cfg;
}

// Auto-init LCD Custom Data section (only if missing)
void EnsureLcdDisplaySection(IMyTerminalBlock lcd)
{
    string data = lcd.CustomData ?? "";
    if (IndexOfInsensitive(data, RPH_DISPLAY_HEADER) >= 0) return;

    var sb = new StringBuilder();
    sb.AppendLine(RPH_DISPLAY_HEADER);
    sb.AppendLine("rph_display_groups = all");
    sb.AppendLine("rph_sort = default");
    sb.AppendLine("rph_header = RPH Tracker");
    sb.AppendLine();

    lcd.CustomData = sb.ToString() + data;
}

// ---------------------------------------------------------------------------
// RPH CUSTOM DATA MODULE (INI-SAFE) - [RPH:Tracking]
// ---------------------------------------------------------------------------

const string RPH_HEADER = "[RPH:Tracking]";

class RphMeta
{
    public string DisplayName = "";
    public string SubGroup = "";
    public double CalibratedZero = 0;
    public bool IsCalibrated = false;
    public bool Reverse = false;
}

// Read existing meta (non-destructive)
static RphMeta ReadRphSection(IMyTerminalBlock b)
{
    string data = b.CustomData;
    if (string.IsNullOrWhiteSpace(data) || IndexOfInsensitive(data, RPH_HEADER) < 0)
        return new RphMeta();

    var lines = data.Split('\n');
    bool inSection = false;
    var meta = new RphMeta();

    for (int i = 0; i < lines.Length; i++)
    {
        string raw = lines[i];
        string line = raw.Trim();
        if (line.Length > 1 && line[0] == '[' && line[line.Length - 1] == ']')
        {
            inSection = line.Equals(RPH_HEADER, StringComparison.OrdinalIgnoreCase);
            continue;
        }
        if (!inSection || line.Length == 0 || line[0] == ';') continue;

        int eq = line.IndexOf('=');
        if (eq < 0) continue;

        string key = TrimLower(line.Substring(0, eq));
        string val = line.Substring(eq + 1).Trim();

        if (key == "rph_display_name") meta.DisplayName = val;
        else if (key == "rph_sub_group") meta.SubGroup = val;
        else if (key == "rph_calibrated_zero")
        {
            double d; if (double.TryParse(val, out d)) meta.CalibratedZero = d;
        }
        else if (key == "rph_is_calibrated")
        {
            bool bval;
            if (bool.TryParse(val, out bval)) meta.IsCalibrated = bval;
            else meta.IsCalibrated = (val == "1" || val.ToLower() == "true" || val.ToLower() == "yes");
        }
        else if (key == "rph_reverse")
        {
            bool bval2;
            if (bool.TryParse(val, out bval2)) meta.Reverse = bval2;
            else meta.Reverse = (val == "1" || val.ToLower() == "true" || val.ToLower() == "yes");
        }
    }
    return meta;
}

// Ensure minimal section exists (non-destructive if present)
static void EnsureRphSection(IMyTerminalBlock b, RphMeta m)
{
    string data = b.CustomData ?? "";
    if (IndexOfInsensitive(data, RPH_HEADER) >= 0) return;

    var sb = new StringBuilder();
    sb.AppendLine(RPH_HEADER);
    sb.AppendLine("rph_display_name = " + m.DisplayName);
    sb.AppendLine("rph_sub_group    = " + m.SubGroup);
    sb.AppendLine("rph_calibrated_zero = " + m.CalibratedZero.ToString("F3"));
    sb.AppendLine("rph_is_calibrated = " + (m.IsCalibrated ? "true" : "false"));
    sb.AppendLine("rph_reverse      = " + (m.Reverse ? "true" : "false"));
    sb.AppendLine();

    b.CustomData = sb.ToString() + data;
}

// Write or replace our section (INI-safe, replaces only [RPH:Tracking])
static void WriteRphSection(IMyTerminalBlock b, RphMeta m)
{
    string data = b.CustomData ?? "";
    var sec = new StringBuilder();
    sec.AppendLine(RPH_HEADER);
    sec.AppendLine("rph_display_name = " + (m.DisplayName ?? ""));
    sec.AppendLine("rph_sub_group    = " + (m.SubGroup ?? ""));
    sec.AppendLine("rph_calibrated_zero = " + m.CalibratedZero.ToString("F3"));
    sec.AppendLine("rph_is_calibrated = " + (m.IsCalibrated ? "true" : "false"));
    sec.AppendLine("rph_reverse      = " + (m.Reverse ? "true" : "false"));
    sec.AppendLine();

    int start = IndexOfInsensitive(data, RPH_HEADER);
    if (start < 0)
    {
        b.CustomData = sec.ToString() + data;
        return;
    }
    int next = data.IndexOf('[', start + 1);
    if (next < 0) next = data.Length;

    string newData = data.Substring(0, start) + sec.ToString() + data.Substring(next);
    b.CustomData = newData.Trim();
}

// ---------------------------------------------------------------------------
// CLEANUP ORPHANS
// ---------------------------------------------------------------------------

void CleanupOrphans(bool silent)
{
    var all = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(all, b =>
        (b is IMyMotorStator || b is IMyMotorAdvancedStator || b is IMyPistonBase)
        && IndexOfInsensitive(b.CustomData, RPH_HEADER) >= 0);

    int total = all.Count;
    int removed = 0;

    for (int i = 0; i < all.Count; i++)
    {
        var b = all[i];
        if (b == null || b.Closed || b.CubeGrid == null) continue;
        if (HasRphIgnoreFlag(b.CustomData)) continue;

        bool stillInGroup = false;
        for (int g = 0; g < groupBlocks.Count; g++)
        {
            if (object.ReferenceEquals(groupBlocks[g], b))
            {
                stillInGroup = true;
                break;
            }
        }
        if (stillInGroup) continue;

        string data = b.CustomData ?? "";
        int start = IndexOfInsensitive(data, RPH_HEADER);
        if (start < 0) continue;
        int next = data.IndexOf('[', start + 1);
        if (next < 0) next = data.Length;

        b.CustomData = (data.Substring(0, start) + data.Substring(next)).Trim();
        removed++;
    }

    if (!silent)
        Echo("RPH: Cleanup complete. Checked " + total + " parts, removed " + removed + " orphans.");
}

// ---------------------------------------------------------------------------
// HELPERS
// ---------------------------------------------------------------------------

bool HasRphIgnoreFlag(string data)
{
    if (string.IsNullOrWhiteSpace(data)) return false;
    var lines = data.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        var raw = lines[i];
        var line = raw.Trim();
        if (line.Length == 0 || line[0] == ';' || line[0] == '[') continue;

        int eq = line.IndexOf('=');
        if (eq < 0) continue;

        string key = line.Substring(0, eq).Trim().ToLower();
        string val = line.Substring(eq + 1).Trim().ToLower();

        if (key != "rph_ignore") continue;
        return (val == "true" || val == "1" || val == "yes");
    }
    return false;
}

List<string> tmpTokens = new List<string>(16);

void SplitTokens(string csv, List<string> outList)
{
    outList.Clear();
    if (string.IsNullOrWhiteSpace(csv)) return;
    int n = csv.Length;
    int start = 0;
    for (int i = 0; i <= n; i++)
    {
        bool end = (i == n) || csv[i] == ',' || csv[i] == ';';
        if (end)
        {
            string token = csv.Substring(start, i - start).Trim().ToLower();
            if (token.Length > 0) outList.Add(token);
            start = i + 1;
        }
    }
}

bool BelongsToAnyGroup(string subgroup, string[] groups, int groupsCount)
{
    if (groupsCount == 0) return true;
    if (string.IsNullOrEmpty(subgroup)) return false;
    string sub = subgroup.Trim().ToLower();
    for (int i = 0; i < groupsCount; i++)
    {
        if (groups[i] == "all" || sub == groups[i]) return true;
    }
    return false;
}

// Sorting helpers
int TypeOrder(PartType t)
{
    if (t == PartType.Rotor) return 0;
    if (t == PartType.Hinge) return 1;
    if (t == PartType.Piston) return 2;
    return 3;
}

void SortSubset(List<TrackedPart> list, SortMode mode)
{
    for (int i = 1; i < list.Count; i++)
    {
        var x = list[i];
        int j = i - 1;
        while (j >= 0 && CompareParts(list[j], x, mode) > 0)
        {
            list[j + 1] = list[j];
            j--;
        }
        list[j + 1] = x;
    }
}

int CompareParts(TrackedPart a, TrackedPart b, SortMode mode)
{
    if (mode == SortMode.Name)
        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    else if (mode == SortMode.Group)
    {
        int g = string.Compare(a.SubGroup, b.SubGroup, StringComparison.OrdinalIgnoreCase);
        if (g != 0) return g;
        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
    else if (mode == SortMode.Type)
    {
        int t = TypeOrder(a.Type).CompareTo(TypeOrder(b.Type));
        if (t != 0) return t;
        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
    return a.OrderIndex.CompareTo(b.OrderIndex);
}

static string TrimLower(string s) { return s == null ? "" : s.Trim().ToLower(); }
static int IndexOfInsensitive(string h, string n) { return h == null ? -1 : h.IndexOf(n, StringComparison.OrdinalIgnoreCase); }

// ---------------------------------------------------------------------------
// END OF SCRIPT
// ---------------------------------------------------------------------------
//
// Version: 1.6d (User guidance, refresh vs rebuild_cd, calibration flag)
// - refresh: read-only rescan, warns when headers missing
// - rebuild_cd: write missing headers (resets keys to defaults)
// - calibrate_all: explicit rph_is_calibrated flag
// - ASCII-only strings; PARK-safe; backward compatible
// ---------------------------------------------------------------------------
