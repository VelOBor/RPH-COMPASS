// === RPH TRACKER v0.91 ===
// Rotor / Piston / Hinge status monitor
// Fully compatible with PARK script (does not modify [PARK:*] sections)
// C#6-compatible (Space Engineers PB / MDK-SE)
//
// Author: Salty & GPT-5 Engineering Division
// ---------------------------------------------------------------------------
// CONFIGURATION
// ---------------------------------------------------------------------------

const string GROUP_NAME = "Tracked Parts";   // Block group containing parts + LCD(s)
const double UPDATE_INTERVAL = 0.5;          // Seconds between updates

// ---------------------------------------------------------------------------
// INTERNAL STATE
// ---------------------------------------------------------------------------

List<IMyTerminalBlock> tracked = new List<IMyTerminalBlock>();
List<IMyTextSurface> lcds = new List<IMyTextSurface>();
double elapsed = 0;

// ---------------------------------------------------------------------------
// INITIALIZATION
// ---------------------------------------------------------------------------

Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    var group = GridTerminalSystem.GetBlockGroupWithName(GROUP_NAME);
    if (group == null)
    {
        Echo("ERROR: Group '" + GROUP_NAME + "' not found.");
        return;
    }

    group.GetBlocks(tracked);

    // Find valid LCDs in the group (text panels, cockpits, programmable blocks)
    foreach (var b in tracked)
    {
        var surfProv = b as IMyTextSurfaceProvider;
        if (surfProv == null)
            continue;

        // Exclude mechanical parts that falsely implement the interface (rare)
        if (b is IMyMotorStator || b is IMyPistonBase || b is IMyMotorAdvancedStator)
            continue;

        // Try to get first surface
        IMyTextSurface surf = null;
        try { surf = surfProv.GetSurface(0); } catch { surf = null; }
        if (surf != null)
        {
            surf.ContentType = ContentType.TEXT_AND_IMAGE;
            surf.WriteText("RPH Tracker LCD linked.\n");
            lcds.Add(surf);
        }
    }

    Echo("RPH Tracker initialized.\n" +
         "Tracked blocks: " + tracked.Count + "\n" +
         "LCDs found: " + lcds.Count + "\n" +
         "Update interval: " + UPDATE_INTERVAL + " s");
}

// ---------------------------------------------------------------------------
// MAIN LOOP
// ---------------------------------------------------------------------------

void Main(string arg, UpdateType update)
{
    elapsed += Runtime.TimeSinceLastRun.TotalSeconds;
    if (elapsed < UPDATE_INTERVAL)
        return;
    elapsed = 0;

    if (lcds.Count == 0)
    {
        Echo("No LCDs detected in group '" + GROUP_NAME + "'.");
        return;
    }

    var sb = new StringBuilder();
    sb.AppendLine("=== RPH TRACKER STATUS ===");

    int partCount = 0;

    foreach (var block in tracked)
    {
        if (block == null || !block.IsFunctional)
            continue;

        var rotor = block as IMyMotorStator;
        var hinge = block as IMyMotorAdvancedStator;
        var piston = block as IMyPistonBase;

        if (rotor == null && hinge == null && piston == null)
            continue;

        var meta = ReadRphSection(block);
        if (string.IsNullOrEmpty(meta.DisplayName))
        {
            meta.DisplayName = block.CustomName;
            meta.SubGroup = "Ungrouped";
            meta.CalibratedZero = 0;
            meta.Reverse = false;
            EnsureRphSection(block, meta);
        }

        double value = 0;
        string unit = "";

        if (rotor != null)
        {
            value = MathHelper.ToDegrees(rotor.Angle) - meta.CalibratedZero;
            if (meta.Reverse) value = -value;
            unit = "°";
        }
        else if (hinge != null)
        {
            value = MathHelper.ToDegrees(hinge.Angle) - meta.CalibratedZero;
            if (meta.Reverse) value = -value;
            unit = "°";
        }
        else if (piston != null)
        {
            value = piston.CurrentPosition - meta.CalibratedZero;
            if (meta.Reverse) value = -value;
            unit = " m";
        }

        sb.AppendLine(meta.DisplayName + ": " + value.ToString("F2") + unit);
        partCount++;
    }

    sb.AppendLine();
    sb.AppendLine("Parts tracked: " + partCount);
    sb.AppendLine("Update interval: " + UPDATE_INTERVAL.ToString("F1") + "s");

    foreach (var lcd in lcds)
    {
        if (lcd == null) continue;
        lcd.WriteText(sb.ToString());
    }
}

// ---------------------------------------------------------------------------
// === RPH TRACKER — CUSTOM DATA SAFETY MODULE ===
// ---------------------------------------------------------------------------

const string RPH_HEADER = "[RPH:Tracking]";

public class RphMeta
{
    public string DisplayName = "";
    public string SubGroup = "";
    public double CalibratedZero = 0;
    public bool Reverse = false;

    public RphMeta() { }
    public RphMeta(string name, string group, double zero, bool rev)
    {
        DisplayName = name;
        SubGroup = group;
        CalibratedZero = zero;
        Reverse = rev;
    }
}

RphMeta ReadRphSection(IMyTerminalBlock block)
{
    string data = block.CustomData;
    if (string.IsNullOrWhiteSpace(data) || !data.Contains(RPH_HEADER))
        return new RphMeta();

    var lines = data.Split('\n');
    bool inSection = false;
    var meta = new RphMeta();

    foreach (string raw in lines)
    {
        string line = raw.Trim();
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            inSection = line.Equals(RPH_HEADER, StringComparison.OrdinalIgnoreCase);
            continue;
        }

        if (!inSection || line.StartsWith(";") || line.Length == 0)
            continue;

        int eq = line.IndexOf('=');
        if (eq < 0) continue;
        string key = line.Substring(0, eq).Trim().ToLower();
        string value = line.Substring(eq + 1).Trim();

        switch (key)
        {
            case "rph_display_name":
                meta.DisplayName = value;
                break;
            case "rph_sub_group":
                meta.SubGroup = value;
                break;
            case "rph_calibrated_zero":
                double.TryParse(value, out meta.CalibratedZero);
                break;
            case "rph_reverse":
                bool rev = false;
                if (bool.TryParse(value, out rev)) meta.Reverse = rev;
                else meta.Reverse = (value == "1" || value.ToLower() == "true");
                break;
        }
    }
    return meta;
}

void EnsureRphSection(IMyTerminalBlock block, RphMeta meta)
{
    string data = block.CustomData ?? "";
    if (data.Contains(RPH_HEADER))
        return; // already exists

    var sb = new StringBuilder();
    sb.AppendLine(RPH_HEADER);
    sb.AppendLine("rph_display_name = " + meta.DisplayName);
    sb.AppendLine("rph_sub_group    = " + meta.SubGroup);
    sb.AppendLine("rph_calibrated_zero = " + meta.CalibratedZero.ToString("F3"));
    sb.AppendLine("rph_reverse      = " + (meta.Reverse ? "true" : "false"));
    sb.AppendLine();

    block.CustomData = sb.ToString() + data;
}
