using System.Numerics;
using Raylib_cs;

internal static class AppState {
    
    public const int PanelWidth = 380;
    public const float HandleRadius = 8f;
    public const float HandleHoverDistance = 20f;
    public const float EdgeHoverDistance = 14f;
    public const float SettingsLabelWidth = 90f;

    public static readonly string[] ResolutionLabels = ["32", "64", "128", "256", "512", "1024", "2048", "4096"];
    public static readonly string[] OutputModeLabels = ["Square", "Landscape (2:1)", "Portrait (1:2)"];
    public static readonly string[] FilterModeLabels = ["Point", "Bilinear"];
    public static readonly string[] ResizeModeLabels = ["Mitchell", "Bilinear"];
    public static readonly string[] GridColorLabels = ["Yellow", "White", "Cyan", "Red"];
    public static readonly string[] ColorBitLabels = ["24-bit", "18-bit", "15-bit (PS1)", "12-bit", "9-bit", "6-bit", "3-bit"];
    public static readonly string[] TilePreviewLabels = ["Single", "3x3"];
    public static readonly string[] ExportFormatLabels = ["PNG", "JPG"];
    public static readonly string[] ShaderUniformNames = ["uH0", "uH1", "uH2", "uBright", "uContrast", "uSat", "uSharp", "uGamma", "uTexel", "uColorBits", "uNoise", "uBleed", "uDither", "uJitter", "uChromaShift", "uSeamlessX", "uSeamlessY", "uSeamWidth"];
    private static readonly int[] Resolutions = [32, 64, 128, 256, 512, 1024, 2048, 4096];
    public static readonly float[] ColorBitsValues = [8f, 6f, 5f, 4f, 3f, 2f, 1f];
    public static readonly Color[] GridColors = [new(255, 255, 0, 255), new(255, 255, 255, 255), new(0, 255, 255, 255), new(255, 0, 0, 255)];
    private static readonly Vector2[] DefaultCorners = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)];

    public static Texture2D? SourceTexture;
    public static string SourcePath = "";
    public static readonly Vector2[] Corners = (Vector2[])DefaultCorners.Clone();

    public static int DragCorner = -1;
    public static int DragEdge = -1;
    public static bool IsPanning;
    public static Vector2 DragStartMouse;
    public static readonly Vector2[] DragStartCorners = (Vector2[])DefaultCorners.Clone();
    public static bool ClickedThisFrame;
    public static Vector2 MousePosition;

    public static int ResolutionIndex = 4;
    public static int OutputModeIndex;
    public static int FilterIndex = 1;
    public static int ResizeIndex = 1;
    public static int ColorBitsIndex;
    public static int GridColorIndex;
    public static int TilePreviewIndex;
    public static int ExportFormatIndex = 1;
    public static int SnapDivisions = 40;

    public static float Brightness;
    public static float Contrast = 1f;
    public static float Saturation = 1f;
    public static float Sharpness;
    public static float Gamma = 1f;
    public static float GridOpacity;
    public static float Noise;
    public static float Bleed;
    public static float Dither;
    public static float Jitter;
    public static float ChromaShift;
    public static float SeamlessX;
    public static float SeamlessY;
    public static float SeamWidth = 0.18f;

    public static RenderTexture2D PreviewTarget;
    public static int PreviewWidth;
    public static int PreviewHeight;

    public static TextureFilter SourceFilter => FilterIndex == 0 ? TextureFilter.Point : TextureFilter.Bilinear;

    public static TextureFilter PreviewFilter => FilterIndex == 0 || ResizeIndex == 0 ? TextureFilter.Point : TextureFilter.Bilinear;

    public static void ResetCorners() => Array.Copy(DefaultCorners, Corners, Corners.Length);

    public static (int Width, int Height) GetOutputSize() {
        
        var width = Resolutions[ResolutionIndex];
        var height = OutputModeIndex == 1 ? width / 2 : width;
        
        if (OutputModeIndex == 2) width = height / 2;
        return (width, height);
    }

    public static (int Cols, int Rows) GetGridDimensions() =>
        
        OutputModeIndex switch {
            
            1 => (10, 5),
            2 => (5, 10),
            _ => (10, 10)
        };

    public static void ResetAll() {
        
        ResetCorners();

        ResolutionIndex = 4;
        OutputModeIndex = 0;
        FilterIndex = 1;
        ResizeIndex = 1;
        ColorBitsIndex = 0;
        GridColorIndex = 0;
        TilePreviewIndex = 0;
        ExportFormatIndex = 1;
        SnapDivisions = 40;

        Brightness = 0f;
        Contrast = 1f;
        Saturation = 1f;
        Sharpness = 0f;
        Gamma = 1f;
        GridOpacity = 0f;
        Noise = 0f;
        Bleed = 0f;
        Dither = 0f;
        Jitter = 0f;
        ChromaShift = 0f;
        SeamlessX = 0f;
        SeamlessY = 0f;
        SeamWidth = 0.18f;
    }

    public static string GetExportExtension() => ExportFormatIndex == 1 ? "jpg" : "png";

    public static string GetEstimatedExportSizeLabel() {
        
        var (width, height) = GetOutputSize();
        var pixels = width * height;
        var bytes = ExportFormatIndex == 1
            ? Math.Max(24_000d, pixels * 0.42d)
            : Math.Max(32_000d, pixels * 1.35d);

        return FormatBytes(bytes);
    }

    private static string FormatBytes(double bytes) {
        
        string[] units = ["B", "KB", "MB", "GB"];
        
        var unit = 0;
        
        while (bytes >= 1024d && unit < units.Length - 1) {
            
            bytes /= 1024d;
            unit++;
        }

        return unit == 0 ? $"{bytes:0} {units[unit]}" : $"{bytes:0.#} {units[unit]}";
    }
}
