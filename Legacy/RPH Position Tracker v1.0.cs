// === RPH TRACKER v1.1 ===
// Rotor / Piston / Hinge status monitor
// Phase 1.1: Maintenance Update + "refresh" command
// Fully compatible with PARK ([PARK:*] sections untouched)
// C#6-compatible (Space Engineers PB / MDK-SE)
//
// Author: Salty & GPT-5 Engineering Division
// ---------------------------------------------------------------------------
// CONFIGURATION
// ---------------------------------------------------------------------------

const string GROUP_NAME = "Tracked Parts";   // Block group containing parts + LCD(s)
const double UPDATE_INTERVAL = 0.5;          // Seconds between LCD refreshes

// ---------------------------------------------------------------------------
// INTERNAL STATE
// ---------------------------------------------------------------------------

List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
List<IMyTextSurface> lcds = new List<IMyTextSurface>();
List<TrackedPart> parts = new List<TrackedPart>();
double elapsed = 0;

// ---------------------------------------------------------------------------
// PROGRAM INIT
// ---------------------------------------------------------------------------

Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    // Always clear stale references (prevents memory persistence issues)
    parts.Clear();
    groupBlocks.Clear();
    lcds.Clear();

    BuildPartsAndLCDs();

    Echo("RPH Tracker v1.1 initialized");
    Echo("Group blocks: " + groupBlocks.Count);
    Echo("Tracked parts: " + parts.Count);
    Echo("LCDs found: " + lcds.Count);
    Echo("Update interval: " + UPDATE_INTERVAL + " s");
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
    }

    elapsed += Runtime.TimeSinceLastRun.TotalSeconds;
    if (elapsed < UPDATE_INTERVAL)
        return;
    elapsed = 0;

    if (lcds.Count == 0)
    {
        Echo("No LCDs in group '" + GROUP_NAME + "'.");
        return;
    }

    for (int i = 0; i < parts.Count; i++)
        parts[i].Update(Runtime.TimeSinceLastRun.TotalSeconds);

    var sb = new StringBuilder(1024);
    sb.AppendLine("=== RPH TRACKER STATUS ===");

    for (int i = 0; i < parts.Count; i++)
        sb.AppendLine(parts[i].GetDisplayLine());

    sb.AppendLine();
    sb.AppendLine("Parts tracked: " + parts.Count);
    sb.AppendLine("Update interval: " + UPDATE_INTERVAL.ToString("F1") + "s");

    string text = sb.ToString();
    for (int i = 0; i < lcds.Count; i++)
    {
        if (lcds[i] == null) continue;
        lcds[i].WriteText(text);
    }
}

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

    // --- Build tracked parts ---
    for (int i = 0; i < groupBlocks.Count; i++)
    {
        var b = groupBlocks[i];
        if (!b.IsFunctional) continue;

        if (b is IMyMotorStator) parts.Add(new TrackedPart(b as IMyMotorStator));
        else if (b is IMyMotorAdvancedStator) parts.Add(new TrackedPart(b as IMyMotorAdvancedStator));
        else if (b is IMyPistonBase) parts.Add(new TrackedPart(b as IMyPistonBase));
    }

    // --- Find LCD-capable blocks ---
    for (int i = 0; i < groupBlocks.Count; i++)
    {
        var b = groupBlocks[i];

        // Skip mechanicals (no text surfaces)
        if (b is IMyMotorStator || b is IMyMotorAdvancedStator || b is IMyPistonBase) continue;

        var sp = b as IMyTextSurfaceProvider;
        if (sp == null) continue;

        IMyTextSurface s0 = null;
        try { s0 = sp.GetSurface(0); } catch { s0 = null; }
        if (s0 != null)
        {
            s0.ContentType = ContentType.TEXT_AND_IMAGE;
            s0.WriteText("RPH Tracker linked.\n");
            lcds.Add(s0);
        }
    }

    // --- Ensure all tracked parts have valid metadata ---
    for (int i = 0; i < parts.Count; i++)
        parts[i].LoadOrCreateMeta();
}

void RefreshParts()
{
    parts.Clear();
    groupBlocks.Clear();
    lcds.Clear();
    BuildPartsAndLCDs();
    Echo("RPH: Scan complete. Parts: " + parts.Count + ", LCDs: " + lcds.Count);
}

// ---------------------------------------------------------------------------
// TRACKED PART MODEL
// ---------------------------------------------------------------------------

enum PartType { Rotor, Hinge, Piston, Unknown }

class TrackedPart
{
    public IMyTerminalBlock Block;
    public PartType Type = PartType.Unknown;

    // Meta
    public string DisplayName = "";
    public string SubGroup = "Ungrouped";
    public double CalibratedZero = 0.0;
    public bool Reverse = false;

    // Live readings
    public double Value = 0.0;
    public string Unit = "";

    // Future placeholders
    public double LastRawValue = 0.0;
    public double Velocity = 0.0;
    public bool HasLast = false;

    public TrackedPart(IMyMotorStator rotor) { Block = rotor; Type = PartType.Rotor; }
    public TrackedPart(IMyMotorAdvancedStator hinge) { Block = hinge; Type = PartType.Hinge; }
    public TrackedPart(IMyPistonBase piston) { Block = piston; Type = PartType.Piston; }

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
// RPH CUSTOM DATA MODULE (INI-SAFE)
// ---------------------------------------------------------------------------

const string RPH_HEADER = "[RPH:Tracking]";

class RphMeta
{
    public string DisplayName = "";
    public string SubGroup = "";
    public double CalibratedZero = 0;
    public bool Reverse = false;
    public RphMeta() { }
    public RphMeta(string n, string g, double z, bool r)
    { DisplayName = n; SubGroup = g; CalibratedZero = z; Reverse = r; }
}

static RphMeta ReadRphSection(IMyTerminalBlock block)
{
    string data = block.CustomData;
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
        string value = line.Substring(eq + 1).Trim();

        if (key == "rph_display_name") meta.DisplayName = value;
        else if (key == "rph_sub_group") meta.SubGroup = value;
        else if (key == "rph_calibrated_zero")
        {
            double d; if (double.TryParse(value, out d)) meta.CalibratedZero = d;
        }
        else if (key == "rph_reverse")
        {
            bool b;
            if (bool.TryParse(value, out b)) meta.Reverse = b;
            else meta.Reverse = (value == "1" || value.ToLower() == "true");
        }
    }
    return meta;
}

static void EnsureRphSection(IMyTerminalBlock block, RphMeta meta)
{
    string data = block.CustomData ?? "";
    if (IndexOfInsensitive(data, RPH_HEADER) >= 0) return;

    var sb = new StringBuilder();
    sb.AppendLine(RPH_HEADER);
    sb.AppendLine("rph_display_name = " + meta.DisplayName);
    sb.AppendLine("rph_sub_group    = " + meta.SubGroup);
    sb.AppendLine("rph_calibrated_zero = " + meta.CalibratedZero.ToString("F3"));
    sb.AppendLine("rph_reverse      = " + (meta.Reverse ? "true" : "false"));
    sb.AppendLine();

    block.CustomData = sb.ToString() + data;
}

// Helpers
static string TrimLower(string s) { return s == null ? "" : s.Trim().ToLower(); }
static int IndexOfInsensitive(string h, string n)
{ return h == null ? -1 : h.IndexOf(n, StringComparison.OrdinalIgnoreCase); }
