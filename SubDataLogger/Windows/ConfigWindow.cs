using System;
using System.Numerics;
using System.Threading;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SubDataLogger.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Config;
    private Plugin plugin;

    public ConfigWindow(Plugin plugin) : base(
        "Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(375, 200);
        this.SizeCondition = ImGuiCond.Always;

        this.Config = plugin.Configuration;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var enabled = this.Config.enabled;
        if (ImGui.Checkbox("Data Capture Enabled", ref enabled))
        {
            this.Config.enabled = enabled;
            this.Config.Save();
        }
        ImGui.Separator();
        var name = this.Config.name ?? "";
        if (ImGui.InputText("Username", ref name, 64))
        {
            this.Config.name = name;
        }

        var sheetID = this.Config.sheetID ?? "";
        if (ImGui.InputText("Sheet ID", ref sheetID, 64))
        {
            this.Config.sheetID = sheetID;

        }

        var sheetName = this.Config.sheetName ?? "";
        if (ImGui.InputText("Sheet Name", ref sheetName, 64))
        {
            this.Config.sheetName = sheetName;

        }

        var range = this.Config.range ?? "";
        if (ImGui.InputText("Range", ref range, 64))
        {
            this.Config.range = range;

        }

        ImGui.Separator();
        if (ImGui.Button("Save"))
        {
            this.Config.Save();
        }

    }
}
