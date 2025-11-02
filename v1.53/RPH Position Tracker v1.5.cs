// === RPH TRACKER v1.53a ===
// Rotor / Piston / Hinge status monitor
// Phase 2.3a: Echo Fix + Refinement Pass
// Fully compatible with PARK ([PARK:*] sections untouched)
// C#6-compatible (Space Engineers PB / MDK-SE)
//
// Author: Salty & GPT-5 Engineering Division
// ---------------------------------------------------------------------------
// AVAILABLE COMMANDS (Run Arguments)
// ---------------------------------------------------------------------------
//
//  refresh / rescan / reload
//      → Rebuilds the tracked part list and LCD list from the group
//        "Tracked Parts". Revalidates Custom Data + runs cleanup.
//
//  cleanup
//      → Full-grid orphan cleanup: removes [RPH:Tracking] from any
//        mechanical block not in the current tracked group.
//
// (future) calibrate <part>   → set current position as zero
// (future) debug              → toggle diagnostic mode
// (future) set <param>=<val>  → update config parameters
//
// NOTE (SE quirk):
//   After removing blocks from a group, delete and recreate the group with the
//   same name to force a fresh membership list for the PB runtime.
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

    BuildPartsAndLCDs();
    CleanupOrphans(true); // auto full-grid cleanup on compile

    Echo("RPH Tracker v1.53a initialized");
    Echo("Group blocks: " + groupBlocks.Count);
    Echo("Tracked parts: " + parts.Count);
    Echo("LCDs found: " + lcdTargets.Count);
    Echo("Update interval: " + UPDATE_INTERVAL + " s");
    Echo("See Custom Data for available options.");
}

// ---------------------------------------------------------------------------
// MAIN LOOP
// ---------------------------------------------------------------------------

void Main(string arg, UpdateType update)
{
    // --- Command handling ---
    if (!string.IsNullOrWhiteSpace(arg))
    {
        arg = arg.Trim().ToLower();

        if (arg == "refresh" || arg == "rescan" || arg == "reload")
        {
            Echo("RPH: Refreshing tracked parts...");
            RefreshParts();
            return;
        }
        if (arg == "cleanup")
        {
            Echo("RPH: Performing full-grid cleanup...");
            CleanupOrphans(false);
            Echo("See Custom Data for available options.");
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
    for (int i = 0; i < parts.Count; i++)
        parts[i].Update(dt);

    for (int i = 0; i < lcdTargets.Count; i++)
    {
        var tgt = lcdTargets[i];
        if (tgt.Surface == null) continue;

        subset.Clear();
        if (tgt.Config.GroupsCount == 0)
        {
            for (int p = 0; p < parts.Count; p++) subset.Add(parts[p]);
        }
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
        sb.AppendLine("=== " + (string.IsNullOrEmpty(tgt.Config.Header)
            ? "RPH TRACKER STATUS" : tgt.Config.Header) + " ===");

        for (int p = 0; p < subset.Count; p++)
            sb.AppendLine(subset[p].GetDisplayLine());

        sb.AppendLine();
        sb.AppendLine("Parts shown: " + subset.Count);
        sb.AppendLine("Groups shown: " + tgt.Config.GroupsCount);
        sb.AppendLine("Update interval: " + UPDATE_INTERVAL.ToString("F1") + "s");

        var text = sb.ToString();
        if (text != tgt.LastText)
        {
            tgt.Surface.WriteText(text);
            tgt.LastText = text;
        }
    }
}

StringBuilder sb = new StringBuilder(2048);
List<TrackedPart> subset = new List<TrackedPart>(128);

// ---------------------------------------------------------------------------
// REFRESH & INITIALIZATION HELPERS
// ---------------------------------------------------------------------------

void BuildPartsAndLCDs()
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

        var rotor = b as IMyMotorStator;
        var hinge = b as IMyMotorAdvancedStator;
        var piston = b as IMyPistonBase;

        if (rotor != null)
        {
            var tp = new TrackedPart(rotor);
            tp.OrderIndex = nextOrderIndex++;
            tp.LoadOrCreateMeta();
            parts.Add(tp);
            continue;
        }
        if (hinge != null)
        {
            var tp = new TrackedPart(hinge);
            tp.OrderIndex = nextOrderIndex++;
            tp.LoadOrCreateMeta();
            parts.Add(tp);
            continue;
        }
        if (piston != null)
        {
            var tp = new TrackedPart(piston);
            tp.OrderIndex = nextOrderIndex++;
            tp.LoadOrCreateMeta();
            parts.Add(tp);
            continue;
        }
    }

    // --- LCD detection + config auto-init ---
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
            EnsureLcdDisplaySection(b);
            var cfg = ReadLcdDisplayConfig(b);
            lcdTargets.Add(new LcdTarget(s0, cfg));
        }
    }
}
// ---------------------------------------------------------------------------
// REFRESH (rebuild lists, write PB CD diagnostics)
// ---------------------------------------------------------------------------

void RefreshParts()
{
    parts.Clear();
    groupBlocks.Clear();
    lcdTargets.Clear();
    nextOrderIndex = 0;

    BuildPartsAndLCDs();
    CleanupOrphans(false);

    // --- Generate group registry + echo summary ---
    var groups = new HashSet<string>();
    for (int i = 0; i < parts.Count; i++)
    {
        var g = parts[i].SubGroup.Trim();
        if (!string.IsNullOrEmpty(g))
            groups.Add(g.ToLower());
    }
    var sorted = groups.ToList();
    sorted.Sort(StringComparer.OrdinalIgnoreCase);

    var cd = new StringBuilder();
    cd.AppendLine("[RPH:Info]");
    cd.AppendLine("available_groups = " + string.Join(", ", sorted));
    cd.AppendLine("last_refresh = " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    cd.AppendLine();
    cd.AppendLine("[RPH:Commands]");
    cd.AppendLine("refresh / rescan / reload  → rebuild part and LCD lists");
    cd.AppendLine("cleanup                    → remove orphan [RPH:Tracking] headers");
    cd.AppendLine("(future) calibrate <part>  → set current position as zero");
    cd.AppendLine("(future) debug             → toggle diagnostic mode");
    cd.AppendLine("(future) set <p>=<val>     → update config parameters");
    cd.AppendLine();

    Me.CustomData = cd.ToString();

    Echo("RPH: Scan complete.");
    Echo("Parts: " + parts.Count + ", LCDs: " + lcdTargets.Count + ", Groups: " + sorted.Count);
    Echo("See Custom Data for available options.");
}

// ---------------------------------------------------------------------------
// ENHANCED ORPHAN CLEANUP MODULE (full-grid)
// ---------------------------------------------------------------------------

void CleanupOrphans(bool silent)
{
    var all = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(all, b =>
        (b is IMyMotorStator || b is IMyMotorAdvancedStator || b is IMyPistonBase)
        && b.CustomData.IndexOf(RPH_HEADER, StringComparison.OrdinalIgnoreCase) >= 0);

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

        string data = b.CustomData;
        int start = data.IndexOf(RPH_HEADER, StringComparison.OrdinalIgnoreCase);
        if (start < 0) continue;
        int next = data.IndexOf('[', start + 1);
        if (next < 0) next = data.Length;

        b.CustomData = data.Remove(start, next - start).Trim();
        removed++;
    }

    if (!silent)
        Echo("RPH: Cleanup complete. Checked " + total + " parts, removed " + removed + " orphans.");
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
    public bool Reverse = false;

    public double Value = 0.0;
    public string Unit = "";

    public double LastRawValue = 0.0;
    public double Velocity = 0.0;
    public bool HasLast = false;

    public int OrderIndex = 0; // preserves group order for default sorting

    public TrackedPart(IMyMotorStator r) { Block = r; Type = PartType.Rotor; }
    public TrackedPart(IMyMotorAdvancedStator h) { Block = h; Type = PartType.Hinge; }
    public TrackedPart(IMyPistonBase p) { Block = p; Type = PartType.Piston; }

    public void LoadOrCreateMeta()
    {
        var meta = ReadRphSection(Block);
        if (IsEmpty(meta.DisplayName))
        {
            meta.DisplayName = Block.CustomName;
            meta.SubGroup = "Ungrouped";
            meta.CalibratedZero = 0.0;
            meta.Reverse = false;
            EnsureRphSection(Block, meta);
        }

        DisplayName = meta.DisplayName;
        SubGroup = meta.SubGroup;
        CalibratedZero = meta.CalibratedZero;
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
            Unit = "°";
        }
        else if (Type == PartType.Hinge)
        {
            var h = Block as IMyMotorAdvancedStator;
            double rawDeg = MathHelper.ToDegrees(h.Angle);
            LastRawValue = rawDeg;
            double adjusted = rawDeg - CalibratedZero;
            if (Reverse) adjusted = -adjusted;
            Value = adjusted;
            Unit = "°";
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

    static bool IsEmpty(string s) { return string.IsNullOrEmpty(s) || s.Trim().Length == 0; }
}

// ---------------------------------------------------------------------------
// LCD DISPLAY CONFIG (PER-LCD) — [RPH:Display]
// ---------------------------------------------------------------------------

const string RPH_DISPLAY_HEADER = "[RPH:Display]";
enum SortMode { Default = 0, Name = 1, Group = 2, Type = 3 }

class LcdDisplayConfig
{
    public string Header = "";
    public string[] Groups = new string[0]; // lowercase tokens
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
        return cfg; // defaults: show all, default sort, no custom header

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

// Auto-init LCD Custom Data section (non-destructive; only if missing)
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
// RPH CUSTOM DATA MODULE (INI-SAFE) — [RPH:Tracking]
// ---------------------------------------------------------------------------

const string RPH_HEADER = "[RPH:Tracking]";

class RphMeta
{
    public string DisplayName = "";
    public string SubGroup = "";
    public double CalibratedZero = 0;
    public bool Reverse = false;
}

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
        else if (key == "rph_reverse")
        {
            bool bval;
            if (bool.TryParse(val, out bval)) meta.Reverse = bval;
            else meta.Reverse = (val == "1" || val.ToLower() == "true");
        }
    }
    return meta;
}

static void EnsureRphSection(IMyTerminalBlock b, RphMeta m)
{
    string data = b.CustomData ?? "";
    if (IndexOfInsensitive(data, RPH_HEADER) >= 0) return;

    var sb = new StringBuilder();
    sb.AppendLine(RPH_HEADER);
    sb.AppendLine("rph_display_name = " + m.DisplayName);
    sb.AppendLine("rph_sub_group    = " + m.SubGroup);
    sb.AppendLine("rph_calibrated_zero = " + m.CalibratedZero.ToString("F3"));
    sb.AppendLine("rph_reverse      = " + (m.Reverse ? "true" : "false"));
    sb.AppendLine();

    b.CustomData = sb.ToString() + data;
}

// ---------------------------------------------------------------------------
// HELPERS
// ---------------------------------------------------------------------------

bool HasRphIgnoreFlag(string data)
{
    if (string.IsNullOrWhiteSpace(data)) return false;
    var lines = data.Split('\n');
    foreach (var raw in lines)
    {
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
// Version: 1.53a (Echo Fix + Refinements)
// • Robust rph_ignore parser (INI-safe, PARK-safe)
// • Alphabetical group registry with timestamp
// • PB Custom Data includes [RPH:Info] + [RPH:Commands]
// • Echo only on init/commands (no LCD-loop spam)
// • LCD footer shows group count
// • 100% backward compatible
// ---------------------------------------------------------------------------
