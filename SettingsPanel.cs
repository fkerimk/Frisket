using System.Numerics;
using ImGuiNET;

internal static class SettingsPanel {
    
    private readonly record struct ComboSetting(string Label, string Id, Func<int> Get, Action<int> Set, string[] Items, int DefaultValue);
    private readonly record struct SliderSetting(string Label, string Id, Func<float> Get, Action<float> Set, float Min, float Max, float DefaultValue, float Step);
    private readonly record struct IntSliderSetting(string Label, string Id, Func<int> Get, Action<int> Set, int Min, int Max, int DefaultValue, int Step);

    private static readonly ComboSetting[] ComboSettings = [
        
        new("Resolution", "##res", () => AppState.ResolutionIndex, value => AppState.ResolutionIndex = value, AppState.ResolutionLabels, 4),
        new("Filter", "##filter", () => AppState.FilterIndex, value => AppState.FilterIndex = value, AppState.FilterModeLabels, 1),
        new("Resize", "##rsz", () => AppState.ResizeIndex, value => AppState.ResizeIndex = value, AppState.ResizeModeLabels, 1),
        new("Output", "##out", () => AppState.OutputModeIndex, value => AppState.OutputModeIndex = value, AppState.OutputModeLabels, 0),
        new("Tile Preview", "##tile_preview", () => AppState.TilePreviewIndex, value => AppState.TilePreviewIndex = value, AppState.TilePreviewLabels, 0),
        new("Grid Color", "##col", () => AppState.GridColorIndex, value => AppState.GridColorIndex = value, AppState.GridColorLabels, 0),
        new("Color Bits", "##bits", () => AppState.ColorBitsIndex, value => AppState.ColorBitsIndex = value, AppState.ColorBitLabels, 0)
    ];

    private static readonly SliderSetting[] SliderSettings = [
        
        new("Brightness", "##bri", () => AppState.Brightness, value => AppState.Brightness = value, -1f, 1f, 0f, 0.05f),
        new("Contrast", "##con", () => AppState.Contrast, value => AppState.Contrast = value, 0.1f, 3f, 1f, 0.1f),
        new("Saturation", "##sat", () => AppState.Saturation, value => AppState.Saturation = value, 0f, 3f, 1f, 0.1f),
        new("Sharpness", "##shp", () => AppState.Sharpness, value => AppState.Sharpness = value, 0f, 1f, 0f, 0.05f),
        new("Gamma", "##gam", () => AppState.Gamma, value => AppState.Gamma = value, 0.2f, 3f, 1f, 0.05f),
        new("Grid Opacity", "##gop", () => AppState.GridOpacity, value => AppState.GridOpacity = value, 0f, 1f, 0f, 0.05f),
        new("Seam X", "##seam_x", () => AppState.SeamlessX, value => AppState.SeamlessX = value, 0f, 1f, 0f, 0.05f),
        new("Seam Y", "##seam_y", () => AppState.SeamlessY, value => AppState.SeamlessY = value, 0f, 1f, 0f, 0.05f),
        new("Seam Width", "##seam_width", () => AppState.SeamWidth, value => AppState.SeamWidth = value, 0.02f, 0.45f, 0.18f, 0.01f),
        new("Noise", "##noise", () => AppState.Noise, value => AppState.Noise = value, 0f, 0.2f, 0f, 0.01f),
        new("Bleed", "##bleed", () => AppState.Bleed, value => AppState.Bleed = value, 0f, 1f, 0f, 0.05f),
        new("Dither", "##dither", () => AppState.Dither, value => AppState.Dither = value, 0f, 1f, 0f, 0.05f),
        new("Jitter", "##jitter", () => AppState.Jitter, value => AppState.Jitter = value, 0f, 1f, 0f, 0.05f),
        new("Chroma", "##chroma", () => AppState.ChromaShift, value => AppState.ChromaShift = value, 0f, 3f, 0f, 0.1f)
    ];

    private static readonly IntSliderSetting[] IntSliderSettings = [
        
        new("Snap", "##snap", () => AppState.SnapDivisions, value => AppState.SnapDivisions = value, 2, 80, 40, 1)
    ];

    public static void Draw(ImFontPtr font, AppLayout layout, bool validQuad, int outputWidth, int outputHeight, Action resetEverything, Action resetCorners, Action resetSettings, Action exportImage) {
        
        ImGui.PushFont(font);
        PushPanelColors();

        ImGui.SetNextWindowPos(new Vector2(layout.PanelX, 0));
        ImGui.SetNextWindowSize(new Vector2(AppState.PanelWidth, layout.Height));
        
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;

        if (ImGui.Begin("##panel", windowFlags)) {
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 16));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 10));

            DrawExportButton(validQuad, outputWidth, outputHeight, exportImage);
            DrawResetButtons(resetEverything, resetCorners, resetSettings);
            DrawSectionTitle("PREVIEW");
            DrawPreviewSettingsTable();
            ImGui.Spacing();
            DrawSectionTitle("OUTPUT");
            DrawOutputSettingsTable();

            ImGui.PopStyleVar(3);
            ImGui.End();
        }

        ImGui.PopStyleColor(10);
        ImGui.PopFont();
    }

    private static void PushPanelColors() {
        
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.098f, 0.098f, 0.109f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.196f, 0.196f, 0.216f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.156f, 0.156f, 0.176f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.274f, 0.274f, 0.294f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.235f, 0.235f, 0.254f, 1f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.949f, 0.627f, 0.188f, 1f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(1f, 0.725f, 0.286f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.156f, 0.156f, 0.176f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.345f, 0.247f, 0.156f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.467f, 0.306f, 0.165f, 1f));
    }

    private static void DrawSectionTitle(string title) {
        
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), title);
        ImGui.Separator();
    }

    private static void DrawResetButtons(Action resetEverything, Action resetCorners, Action resetSettings) {
        
        DrawSectionTitle("RESET");
        
        if (ImGui.BeginTable("##reset_actions", 3, ImGuiTableFlags.SizingStretchSame)) {
            
            ImGui.TableNextColumn();
            if (ImGui.Button("Corners", new Vector2(-1, 40))) resetCorners();
            ImGui.TableNextColumn();
            if (ImGui.Button("Effects", new Vector2(-1, 40))) resetSettings();
            ImGui.TableNextColumn();
            if (ImGui.Button("Everything", new Vector2(-1, 40))) resetEverything();
            ImGui.EndTable();
        }
        
        ImGui.Spacing();
    }

    private static void DrawExportButton(bool validQuad, int outputWidth, int outputHeight, Action exportImage) {
        
        ImGui.BeginDisabled(!validQuad);
        if (ImGui.Button($"Export {AppState.ExportFormatLabels[AppState.ExportFormatIndex]}", new Vector2(-1, 40))) exportImage();
        ImGui.EndDisabled();
        ImGui.TextColored(new Vector4(0.4f, 0.4f, 0.4f, 1f), $"{outputWidth} × {outputHeight} px    ~{AppState.GetEstimatedExportSizeLabel()}");
        ImGui.Spacing();
    }

    private static void DrawPreviewSettingsTable() {
        
        if (!ImGui.BeginTable("##preview_set", 2)) return;

        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, AppState.SettingsLabelWidth);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

        DrawComboRow(ComboSettings[4]);
        DrawComboRow(ComboSettings[5]);
        DrawIntSliderRow(IntSliderSettings[0]);
        DrawSliderRow(SliderSettings[5]);

        ImGui.EndTable();
    }

    private static void DrawOutputSettingsTable() {
        
        if (!ImGui.BeginTable("##set", 2)) return;

        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, AppState.SettingsLabelWidth);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

        DrawComboRow(ComboSettings[0]);
        DrawComboRow(new ComboSetting("Format", "##export_fmt", () => AppState.ExportFormatIndex, value => AppState.ExportFormatIndex = value, AppState.ExportFormatLabels, 1));
        foreach (var setting in ComboSettings[1..4]) DrawComboRow(setting);
        DrawComboRow(ComboSettings[^1]);
        foreach (var setting in SliderSettings[..5]) DrawSliderRow(setting);
        foreach (var setting in SliderSettings[6..]) DrawSliderRow(setting);

        ImGui.EndTable();
    }

    private static void DrawComboRow(ComboSetting setting) {
        
        var value = setting.Get();
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(setting.Label);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo(setting.Id, ref value, setting.Items, setting.Items.Length);
        
        setting.Set(value);

        if (!ImGui.IsItemHovered()) return;
        ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
        
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) value = setting.DefaultValue;
        var wheel = (int)ImGui.GetIO().MouseWheel;
        
        if (wheel != 0) {
            ImGui.SetNextFrameWantCaptureMouse(true);
            value = Math.Clamp(value - wheel, 0, setting.Items.Length - 1);
        }
        
        setting.Set(value);
    }

    private static void DrawSliderRow(SliderSetting setting) {
        
        var value = setting.Get();
        
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(setting.Label);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat(setting.Id, ref value, setting.Min, setting.Max);
        
        setting.Set(value);

        if (!ImGui.IsItemHovered()) return;
        ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
        
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) value = setting.DefaultValue;
        var wheel = ImGui.GetIO().MouseWheel;
        
        if (wheel != 0) {
            ImGui.SetNextFrameWantCaptureMouse(true);
            value = Math.Clamp(value + wheel * setting.Step, setting.Min, setting.Max);
        }
        
        setting.Set(value);
    }

    private static void DrawIntSliderRow(IntSliderSetting setting) {
        
        var value = setting.Get();
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(setting.Label);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderInt(setting.Id, ref value, setting.Min, setting.Max);
        
        setting.Set(value);

        if (!ImGui.IsItemHovered()) return;
        ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
        
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) value = setting.DefaultValue;
        var wheel = (int)ImGui.GetIO().MouseWheel;
        
        if (wheel != 0) {
            
            ImGui.SetNextFrameWantCaptureMouse(true);
            value = Math.Clamp(value + wheel * setting.Step, setting.Min, setting.Max);
        }
        
        setting.Set(value);
    }
}
