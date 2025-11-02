// ---------------------------------------------------------------------------
//RPH-COMPASS
//Rotor, Piston, Hinge — Component Orientation, Motion And Positional Assessment Status System
//"Monitoring position. Assessing motion. Maintaining awareness. Precision is our direction."
//
//Stability Through Awareness.
// ---------------------------------------------------------------------------
//
// === RPH COMPASS v1.7 ===
// Rotor / Piston / Hinge status monitor
// Phase 3e: Display mode key (full | compact | debug) and render dispatch
// PARK-safe (does not modify [PARK:*] sections)
// C#6-compatible (Space Engineers PB / MDK-SE)
//
// Author: Salty & GPT-5 Engineering Division
//
//
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

StringBuilder sb = new StringBuilder(4096);
List<TrackedPart> subset = new List<TrackedPart>(256);

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
    EchoLcdSummary();

    SafeEcho("RPH COMPASS v1.7 initialized");
    SafeEcho("Group blocks: " + groupBlocks.Count);
    SafeEcho("Tracked parts: " + parts.Count);
    SafeEcho("LCDs found: " + lcdTargets.Count);
    SafeEcho("Update interval: " + UPDATE_INTERVAL + " s");
    SafeEcho("See Custom Data for available options.");
}

// ---------------------------------------------------------------------------
// HEADS-UP WARNING FOR MISSING HEADERS
// ---------------------------------------------------------------------------

void HeadsUpMissingHeaders()
{
    int missing = 0;
    List<string> names = new List<string>();

    for (int i = 0; i < parts.Count; i++)
    {
        var b = parts[i].Block;
        if (IndexOfInsensitive(b.CustomData, "[RPH:Tracking]") < 0)
        {
            missing++;
            if (names.Count < 5)
                names.Add(b.CustomName);
        }
    }

    if (missing > 0)
    {
        SafeEcho("WARNING: " + missing + " parts missing RPH header!");
        for (int i = 0; i < names.Count; i++)
            SafeEcho(" - " + names[i]);
        if (missing > names.Count)
            SafeEcho(" - (+" + (missing - names.Count) + " more)");
        SafeEcho("Run 'rebuild_cd' to repopulate. Will RESET all RPH keys to default.");
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
            SafeEcho("RPH: Refreshing tracked parts (read-only)...");
            DoRefresh(false);
            HeadsUpMissingHeaders();
            return;
        }
        if (arg == "rebuild_cd")
        {
            SafeEcho("RPH: Rebuilding Custom Data headers...");
            DoRefresh(true);
            return;
        }
        if (arg == "cleanup")
        {
            SafeEcho("RPH: Performing orphan cleanup...");
            CleanupOrphans(false);
            return;
        }
        if (arg == "calibrate_all")
        {
            SafeEcho("RPH: Calibrating all uncalibrated parts...");
            CalibrateAll();
            return;
        }
    }

    elapsed += Runtime.TimeSinceLastRun.TotalSeconds;
    if (elapsed < UPDATE_INTERVAL) return;
    elapsed = 0;

    if (lcdTargets.Count == 0)
    {
        SafeEcho("No LCDs in group '" + GROUP_NAME + "'.");
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

        if (tgt.Config.Mode == "compact")
            RenderCompact(tgt, subset);
        else if (tgt.Config.Mode == "debug")
            RenderDebug(tgt, subset);
        else
            RenderFull(tgt, subset);
    }
}

// ---------------------------------------------------------------------------
// RENDERERS
// ---------------------------------------------------------------------------

void RenderFull(LcdTarget tgt, List<TrackedPart> list)
{
    sb.Clear();
    string header = tgt.Config.Header.Length > 0 ? tgt.Config.Header : "RPH COMPASS STATUS";
    sb.AppendLine("=== " + header + " ===");

    string currentGroup = "";
    bool useSeparators = tgt.Config.GroupSeparators && tgt.Config.SortMode == SortMode.Group;

    for (int p = 0; p < list.Count; p++)
    {
        var part = list[p];

        if (useSeparators)
        {
            if (!part.SubGroup.Equals(currentGroup, StringComparison.OrdinalIgnoreCase))
            {
                if (p > 0) sb.AppendLine(); // blank line between groups
                currentGroup = part.SubGroup;
            }
        }

        sb.AppendLine(part.GetDisplayLine());
    }

    tgt.Surface.WriteText(sb);
}


void RenderCompact(LcdTarget tgt, List<TrackedPart> list)
{
    sb.Clear();
    if (list.Count == 0)
    {
        tgt.Surface.WriteText("");
        tgt.LastText = "";
        return;
    }

    string currentGroup = "";
    for (int i = 0; i < list.Count; i++)
    {
        var p = list[i];

        // Start a new line when subgroup changes
        if (!p.SubGroup.Equals(currentGroup, StringComparison.OrdinalIgnoreCase))
        {
            if (i > 0) sb.AppendLine();
            currentGroup = p.SubGroup;
            sb.Append(currentGroup + ": ");
        }

        // Append part summary
        sb.Append(p.DisplayName);
        sb.Append(' ');
        sb.Append(p.Value.ToString("F2"));
        sb.Append(p.Unit);
        sb.Append(" | ");
    }

    var text = sb.ToString();
    if (text != tgt.LastText)
    {
        ApplyAutosizeIfNeeded(tgt, text);
        tgt.LastText = text;
    }

}


void RenderDebug(LcdTarget tgt, List<TrackedPart> list)
{
    sb.Clear();

    int total = list.Count;
    int calibrated = 0;
    for (int i = 0; i < total; i++) if (list[i].IsCalibrated) calibrated++;

    sb.AppendLine("[RPH DEBUG]");
    sb.AppendLine("Parts tracked: " + parts.Count);
    sb.AppendLine("Shown on this LCD: " + total);
    sb.AppendLine("Calibrated: " + calibrated + " / " + total);
    sb.AppendLine("LCD mode: " + (tgt.Config.Mode ?? "full"));
    sb.AppendLine("Sort mode: " + tgt.Config.SortMode.ToString());
    sb.AppendLine("Update interval: " + UPDATE_INTERVAL.ToString("F2") + "s");

    for (int i = 0; i < total; i++)
    {
        var p = list[i];
        sb.AppendLine(p.DisplayName + " | raw=" + p.LastRawValue.ToString("F3")
            + " adj=" + p.Value.ToString("F2")
            + " zero=" + p.CalibratedZero.ToString("F3")
            + " rev=" + (p.Reverse ? "true" : "false"));
        if (p.ReadOnly) sb.AppendLine("   [LOCKED]");
    }

    var text = sb.ToString();
    if (text != tgt.LastText)
    {
        ApplyAutosizeIfNeeded(tgt, text);
        // ApplyAutosizeIfNeeded will do the actual WriteText
        // but we still need to track last text here:
        tgt.LastText = text;
    }
}

void ApplyAutosizeIfNeeded(LcdTarget tgt, string text)
{
    if (!tgt.Config.AutoSize)
    {
        tgt.Surface.WriteText(text);
        return;
    }

    int len = text.Split('\n').Max(line => line.Length);
    float newSize = tgt.Surface.FontSize; // start from current

    // Tuned for LCDs where FontSize 1.2 ≈ 45 visible chars
    if (len > 100) newSize = 0.2f;
    else if (len > 85) newSize = 0.45f;
    else if (len > 75) newSize = 0.5f;
    else if (len > 65) newSize = 0.6f;
    else if (len > 55) newSize = 0.7f;
    else if (len > 45) newSize = 0.8f;
    else if (len > 35) newSize = 1f;
    else newSize = 1.2f;


    // Apply only if smaller (prevents flicker)
    if (Math.Abs(tgt.Surface.FontSize - newSize) > 0.01f)
    tgt.Surface.FontSize = newSize;


    tgt.Surface.WriteText(text);
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
    cd.AppendLine("; SAMPLE LCD DISPLAY CONFIG");
    cd.AppendLine("[RPH:Display]");
    cd.AppendLine("rph_display_mode = full");
    cd.AppendLine("rph_display_groups = all");
    cd.AppendLine("rph_sort = default");
    cd.AppendLine("rph_header = RPH COMPASS");
    cd.AppendLine("rph_display_autosize = false");
    cd.AppendLine("rph_group_separators = false");
    cd.AppendLine("; ---------------------------------------------------------------------------");
    cd.AppendLine();

    Me.CustomData = cd.ToString();

    SafeEcho("RPH: " + (writeHeaders ? "Header rebuild" : "Refresh") + " complete.");
    SafeEcho("Parts: " + parts.Count + ", LCDs: " + lcdTargets.Count + ", Groups: " + sorted.Count);
}

// ---------------------------------------------------------------------------
// BUILD PARTS AND LCDs
// ---------------------------------------------------------------------------

void BuildPartsAndLCDs(bool writeHeaders)
{
    var group = GridTerminalSystem.GetBlockGroupWithName(GROUP_NAME);
    if (group == null)
    {
        SafeEcho("ERROR: Group '" + GROUP_NAME + "' not found.");
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
            // Skip if read-only (mechanical)
            var tp = parts.Find(p => p.Block == b);
            if (tp != null && tp.ReadOnly) continue;

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

    SafeEcho("RPH: Calibration complete. Calibrated " + calibrated + ", skipped " + skipped + (errors > 0 ? (", errors " + errors) : ""));
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
	public bool ReadOnly = false; 
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

        // Read-only flag detection
        if (IndexOfInsensitive(Block.CustomData, "rph_read_only") >= 0)
        {
            var lines = Block.CustomData.Split('\n');
            foreach (var lineRaw in lines)
            {
                var line = lineRaw.Trim().ToLower();
                if (line.StartsWith("rph_read_only"))
                {
                    var eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        var val = line.Substring(eq + 1).Trim();
                        ReadOnly = (val == "true" || val == "1" || val == "yes");
                    }
                }
            }
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
    public string Mode = "full";
    public bool AutoSize = false;        // v1.7
    public bool GroupSeparators = false; // v1.7
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
        else if (key == "rph_display_mode")
        {
            string m = val.Trim().ToLower();
            if (m == "compact" || m == "debug" || m == "full") cfg.Mode = m;
            else cfg.Mode = "full";
        }
        else if (key == "rph_display_autosize")
        {
            cfg.AutoSize = (val.ToLower() == "true" || val == "1" || val.ToLower() == "yes");
        }
        else if (key == "rph_group_separators")
        {
            cfg.GroupSeparators = (val.ToLower() == "true" || val == "1" || val.ToLower() == "yes");
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
    sb.AppendLine("rph_header = RPH-COMPASS");
    sb.AppendLine("rph_display_mode = full");
    sb.AppendLine("rph_display_autosize = false");     // NEW in v1.7
    sb.AppendLine("rph_group_separators = false");     // NEW in v1.7
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
        SafeEcho("RPH: Cleanup complete. Checked " + total + " parts, removed " + removed + " orphans.");
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

void EchoLcdSummary()
{
    for (int i = 0; i < lcdTargets.Count; i++)
    {
        var t = lcdTargets[i];
        SafeEcho("LCD: mode=" + t.Config.Mode + " groups=" + t.Config.GroupsCount);
    }
}

static string TrimLower(string s) { return s == null ? "" : s.Trim().ToLower(); }
static int IndexOfInsensitive(string h, string n) { return h == null ? -1 : h.IndexOf(n, StringComparison.OrdinalIgnoreCase); }

// ---------------------------------------------------------------------------
// END OF SCRIPT
// ---------------------------------------------------------------------------
//
// Version: 1.7 (Display modes: full | compact | debug)
// - rph_display_mode per-LCD toggle; defaults to full
// - Compact mode: single-line summary; Debug: diagnostic overlay
// - No change to existing keys; ASCII-only; PARK-safe
// ---------------------------------------------------------------------------


// ---------------------------------------------------------------------------
// SAFE ECHO WRAPPER (v1.8-pre)
// ---------------------------------------------------------------------------

bool firstEcho = true;

void SafeEcho(string msg)
{
    if (firstEcho)
    {
        firstEcho = false;
        Echo("\n" + msg);
    }
    else
    {
        Echo(msg);
    }
}
