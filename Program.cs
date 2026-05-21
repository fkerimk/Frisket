using System.Numerics;
using ImGuiNET;
using NativeFileDialogNET;
using Raylib_cs;
using rlImGui_cs;
using static Raylib_cs.Raylib;

internal static class Program {

    private static readonly int[] ShaderLocations = new int[AppState.ShaderUniformNames.Length];
    
    private static Shader _imageShader;
    private static Shader _resizeShader;
    private static Shader _seamlessShader;
    
    private static int _resizeTexelLocation;
    private static int _seamlessTexelLocation;
    private static int _seamlessXLocation;
    private static int _seamlessYLocation;
    private static int _seamWidthLocation;
    
    private static Texture2D _checkerTexture;
    private static RenderTexture2D _displayTarget;
    
    private static int _displayWidth;
    
    private static ImFontPtr _font;
    
    public static void Main(string[] args) {
        
        Initialize(args);

        while (!WindowShouldClose()) {
            
            HandleDroppedFiles();
            UpdateInput();
            RenderPreview();
            DrawFrame();
        }

        Shutdown();
    }

    private static unsafe void Initialize(string[] args) {
        
        var startupImagePath = GetStartupImagePath(args);
        if (startupImagePath != null) startupImagePath = Path.GetFullPath(startupImagePath);
        
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        if (!File.Exists("Image.frag")) Directory.SetCurrentDirectory(Path.Combine(AppContext.BaseDirectory, "../../../"));

        startupImagePath ??= GetStartupImagePath(args);
        
        SetTraceLogLevel(TraceLogLevel.Error);
        SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint);
        InitWindow(1400, 800, "Frisket");
        SetWindowMonitor(0);

        rlImGui.Setup();
        
        ImGui.GetIO().NativePtr->IniFilename = null;
        
        _font = ImGui.GetIO().Fonts.AddFontFromFileTTF("Montserrat-Regular.ttf", 18f);
        rlImGui.ReloadFonts();

        _imageShader = LoadShader(null, "Image.frag");
        
        for (var i = 0; i < AppState.ShaderUniformNames.Length; i++)
            ShaderLocations[i] = GetShaderLocation(_imageShader, AppState.ShaderUniformNames[i]);

        _resizeShader = LoadShader(null, "Resize.frag");
        _resizeTexelLocation = GetShaderLocation(_resizeShader, "uTexel");
        if (_resizeShader.Id == 0 || _resizeTexelLocation < 0) Console.Error.WriteLine("resize.frag FAILED TO LOAD");

        _seamlessShader = LoadShader(null, "Seamless.frag");
        _seamlessTexelLocation = GetShaderLocation(_seamlessShader, "uTexel");
        _seamlessXLocation = GetShaderLocation(_seamlessShader, "uSeamlessX");
        _seamlessYLocation = GetShaderLocation(_seamlessShader, "uSeamlessY");
        _seamWidthLocation = GetShaderLocation(_seamlessShader, "uSeamWidth");

        _checkerTexture = LoadTextureFromImage(GenImageChecked(512, 512, 32, 32, new Color(20, 20, 20, 255), new Color(40, 40, 40, 255)));
        SetTextureFilter(_checkerTexture, TextureFilter.Bilinear);

        if (!string.IsNullOrEmpty(startupImagePath)) LoadFile(startupImagePath);
    }

    private static string? GetStartupImagePath(string[] args) {
        
        if (args.Length > 0 && File.Exists(args[0])) return args[0];
        return File.Exists("SampleGrid.jpg") ? "SampleGrid.jpg" : null;
    }

    private static void Shutdown() {
        
        if (AppState.PreviewWidth > 0) UnloadRenderTexture(AppState.PreviewTarget);
        if (_displayWidth > 0) UnloadRenderTexture(_displayTarget);
        if (_checkerTexture.Id != 0) UnloadTexture(_checkerTexture);
        if (_imageShader.Id != 0) UnloadShader(_imageShader);
        if (_resizeShader.Id != 0) UnloadShader(_resizeShader);
        if (_seamlessShader.Id != 0) UnloadShader(_seamlessShader);
        if (AppState.SourceTexture.HasValue) UnloadTexture(AppState.SourceTexture.Value);
        
        rlImGui.Shutdown();
        
        CloseWindow();
    }

    private static void HandleDroppedFiles() {
        
        if (!IsFileDropped()) return;
        
        var files = LoadDroppedFiles();
        if (files.Count > 0) LoadFile(files[0]);
        
        UnloadDroppedFiles(files);
    }

    private static void UpdateInput() {
        
        AppState.MousePosition = GetMousePosition();
        AppState.ClickedThisFrame = IsMouseButtonPressed(MouseButton.Left);

        if (!AppState.SourceTexture.HasValue) return;

        var layout = GeometryUtils.GetLayout();
        var inputRect = GeometryUtils.GetInputRect(layout, AppState.SourceTexture.Value);
        
        HandleQuadInteraction(inputRect);
    }

    private static void RenderPreview() {
        
        var (outputWidth, outputHeight) = AppState.GetOutputSize();
        EnsurePreviewTarget(outputWidth, outputHeight);

        if (!AppState.SourceTexture.HasValue) return;
        if (!TryGetHomography(out var h0, out var h1, out var h2)) return;
        
        RenderToTexture(AppState.PreviewTarget, AppState.SourceTexture.Value, outputWidth, outputHeight, h0, h1, h2);
        RenderDisplayTexture(AppState.PreviewTarget, outputWidth, outputHeight);
    }

    private static void DrawFrame() {
        
        var layout = GeometryUtils.GetLayout();
        var (outputWidth, outputHeight) = AppState.GetOutputSize();

        BeginDrawing();
        ClearBackground(new Color(25, 25, 28, 255));
        DrawLine(layout.HalfWidth, 0, layout.HalfWidth, layout.Height, new Color(50, 50, 50, 255));
        DrawLine(layout.PanelX, 0, layout.PanelX, layout.Height, new Color(50, 50, 50, 255));

        if (AppState.SourceTexture.HasValue) DrawViews(layout, outputWidth, outputHeight, AppState.SourceTexture.Value);

        EndDrawing();
    }

    private static void DrawViews(AppLayout layout, int outputWidth, int outputHeight, Texture2D sourceTexture) {
        
        var inputRect = GeometryUtils.GetInputRect(layout, sourceTexture);
        var previewRect = GeometryUtils.GetPreviewRect(layout, outputWidth, outputHeight);
        var validQuad = GeometryUtils.IsConvex(AppState.Corners);

        DrawInputView(inputRect, sourceTexture);
        DrawPreviewView(previewRect, validQuad);

        rlImGui.Begin();
        SettingsPanel.Draw(_font, layout, validQuad, outputWidth, outputHeight, AppState.ResetAll, AppState.ResetCorners, AppState.ResetSettings, ExportCurrentImage);
        rlImGui.End();
    }

    private static void LoadFile(string filePath) {
        
        var image = LoadImage(filePath);
        
        if (image.Width > 0) {
            
            if (AppState.SourceTexture.HasValue) UnloadTexture(AppState.SourceTexture.Value);
            
            AppState.SourceTexture = LoadTextureFromImage(image);
            SetTextureFilter(AppState.SourceTexture.Value, TextureFilter.Bilinear);
            AppState.SourcePath = filePath;
            AppState.ResetCorners();
        }

        UnloadImage(image);
    }

    private static bool TryGetHomography(out Vector3 h0, out Vector3 h1, out Vector3 h2) {
        
        h0 = default;
        h1 = default;
        h2 = default;
        
        return GeometryUtils.IsConvex(AppState.Corners) && GeometryUtils.TryCalculateHomography(AppState.Corners, out h0, out h1, out h2);
    }

    private static void HandleQuadInteraction(Rectangle inputRect) {
        
        if (AppState.ClickedThisFrame) {
            
            AppState.DragCorner = FindHoveredCorner(inputRect);
            AppState.DragEdge = AppState.DragCorner < 0 ? FindHoveredEdge(inputRect) : -1;
            AppState.DragStartMouse = AppState.MousePosition;
            
            Array.Copy(AppState.Corners, AppState.DragStartCorners, AppState.Corners.Length);
            
            if (AppState.DragCorner < 0 && AppState.DragEdge < 0 && CheckCollisionPointRec(AppState.MousePosition, inputRect)) AppState.IsPanning = true;
        }

        if (!IsMouseButtonDown(MouseButton.Left)) {
            
            AppState.DragCorner = -1;
            AppState.DragEdge = -1;
            AppState.IsPanning = false;
            
            return;
        }

        if (AppState.DragCorner >= 0) {
            
            var corner = GeometryUtils.ClampUv(AppState.DragStartCorners[AppState.DragCorner] + GetDragDelta(inputRect));
            
            AppState.Corners[AppState.DragCorner] = IsAltDown() || AppState.SnapDivisions <= 1
                ? corner
                : GeometryUtils.SnapUv(corner, 1f / AppState.SnapDivisions);
            
            return;
        }

        if (AppState.DragEdge >= 0) {
            
            MoveCorners([AppState.DragEdge, (AppState.DragEdge + 1) % AppState.Corners.Length], GetDragDelta(inputRect));
            return;
        }

        if (!AppState.IsPanning) return;
        
        MoveCorners([0, 1, 2, 3], GetDragDelta(inputRect));
    }

    private static int FindHoveredCorner(Rectangle inputRect) {
        
        var hovered = -1;
        var bestDistance = AppState.HandleHoverDistance;

        for (var i = 0; i < AppState.Corners.Length; i++) {
            
            var distance = Vector2.Distance(AppState.MousePosition, GeometryUtils.ToScreenPoint(inputRect, AppState.Corners[i]));
            if (distance >= bestDistance) continue;
            
            bestDistance = distance;
            hovered = i;
        }

        return hovered;
    }

    private static int FindHoveredEdge(Rectangle inputRect) {
        
        var hovered = -1;
        var bestDistance = AppState.EdgeHoverDistance;

        for (var i = 0; i < AppState.Corners.Length; i++) {
            
            var a = GeometryUtils.ToScreenPoint(inputRect, AppState.Corners[i]);
            var b = GeometryUtils.ToScreenPoint(inputRect, AppState.Corners[(i + 1) % AppState.Corners.Length]);
            
            var distance = GeometryUtils.DistanceToSegment(AppState.MousePosition, a, b);
            if (distance >= bestDistance) continue;
            
            bestDistance = distance;
            hovered = i;
        }

        return hovered;
    }

    private static Vector2 GetDragDelta(Rectangle inputRect) {
        
        return new Vector2(
            
            (AppState.MousePosition.X - AppState.DragStartMouse.X) / inputRect.Width,
            (AppState.MousePosition.Y - AppState.DragStartMouse.Y) / inputRect.Height
        );
    }

    private static void MoveCorners(int[] indices, Vector2 delta) {
        
        delta = ConstrainDragDelta(indices, delta);
        
        if (!IsAltDown() && AppState.SnapDivisions > 1) {
            
            var step = 1f / AppState.SnapDivisions;
            
            delta = new Vector2(
                
                MathF.Round(delta.X / step) * step,
                MathF.Round(delta.Y / step) * step
            );
            
            delta = ConstrainDragDelta(indices, delta);
        }

        foreach (var index in indices)
            AppState.Corners[index] = GeometryUtils.ClampUv(AppState.DragStartCorners[index] + delta);
    }

    private static Vector2 ConstrainDragDelta(int[] indices, Vector2 delta) {
        
        var minAllowedX = float.NegativeInfinity;
        var maxAllowedX = float.PositiveInfinity;
        var minAllowedY = float.NegativeInfinity;
        var maxAllowedY = float.PositiveInfinity;

        foreach (var index in indices) {
            
            var start = AppState.DragStartCorners[index];
            
            minAllowedX = Math.Max(minAllowedX, -start.X);
            maxAllowedX = Math.Min(maxAllowedX, 1f - start.X);
            minAllowedY = Math.Max(minAllowedY, -start.Y);
            maxAllowedY = Math.Min(maxAllowedY, 1f - start.Y);
        }

        return new Vector2(
            
            Math.Clamp(delta.X, minAllowedX, maxAllowedX),
            Math.Clamp(delta.Y, minAllowedY, maxAllowedY)
        );
    }

    private static void EnsurePreviewTarget(int width, int height) {
        
        if (width == AppState.PreviewWidth && height == AppState.PreviewHeight) return;

        if (AppState.PreviewWidth > 0) UnloadRenderTexture(AppState.PreviewTarget);
        if (_displayWidth > 0) UnloadRenderTexture(_displayTarget);
        
        AppState.PreviewTarget = LoadRenderTexture(width, height);
        AppState.PreviewWidth = width;
        AppState.PreviewHeight = height;
        
        SetTextureFilter(AppState.PreviewTarget.Texture, AppState.PreviewFilter);
        
        _displayTarget = LoadRenderTexture(width, height);
        _displayWidth = width;
        
        SetTextureFilter(_displayTarget.Texture, AppState.PreviewFilter);
    }

    private static void RenderToTexture(RenderTexture2D target, Texture2D source, int width, int height, Vector3 h0, Vector3 h1, Vector3 h2) {
        
        SetTextureFilter(source, AppState.SourceFilter);
        SetTextureFilter(target.Texture, AppState.PreviewFilter);
        UploadShaderUniforms(source);
        BeginTextureMode(target);
        ClearBackground(new Color(0, 0, 0, 0));
        BeginShaderMode(_imageShader);
        SetHomographyUniforms(h0, h1, h2);
        DrawTexturePro(source, GeometryUtils.FullTextureRect(source), new Rectangle(0, 0, width, height), default, 0, Color.White);
        EndShaderMode();
        EndTextureMode();
        SetTextureFilter(source, TextureFilter.Bilinear);
    }

    private static void DrawInputView(Rectangle inputRect, Texture2D texture) {
        
        SetTextureFilter(texture, TextureFilter.Bilinear);
        DrawTexturePro(texture, GeometryUtils.FullTextureRect(texture), inputRect, default, 0, Color.White);

        var screenPoints = new Vector2[AppState.Corners.Length];
        
        for (var i = 0; i < AppState.Corners.Length; i++)
            screenPoints[i] = GeometryUtils.ToScreenPoint(inputRect, AppState.Corners[i]);

        for (var i = 0; i < screenPoints.Length; i++)
            DrawEdgeHandle(screenPoints[i], screenPoints[(i + 1) % screenPoints.Length], i);

        for (var i = 0; i < screenPoints.Length; i++)
            DrawCornerHandle(screenPoints[i], i);
    }

    private static void DrawCornerHandle(Vector2 position, int index) {
        
        var hovered = Vector2.Distance(AppState.MousePosition, position) < AppState.HandleHoverDistance;
        
        DrawCircleV(position, AppState.HandleRadius, hovered || AppState.DragCorner == index ? Color.White : Color.Orange);
        DrawCircleLines((int)position.X, (int)position.Y, AppState.HandleRadius, Color.White);
    }

    private static void DrawEdgeHandle(Vector2 start, Vector2 end, int index) {
        
        var hovered = GeometryUtils.DistanceToSegment(AppState.MousePosition, start, end) < AppState.EdgeHoverDistance;
        
        DrawLineEx(start, end, hovered || AppState.DragEdge == index ? 3f : 2f, hovered || AppState.DragEdge == index ? Color.White : Color.Orange);
    }

    private static void DrawPreviewView(Rectangle previewRect, bool validQuad) {
        
        DrawRectangleLinesEx(new Rectangle(previewRect.X - 1, previewRect.Y - 1, previewRect.Width + 2, previewRect.Height + 2), 1, Color.DarkGray);

        if (!validQuad) {
            
            DrawRectangleRec(previewRect, new Color(50, 20, 20, 255));
            return;
        }

        DrawTexturePro(_checkerTexture, GeometryUtils.FullTextureRect(_checkerTexture), previewRect, default, 0, Color.White);
        DrawPreviewTexture(previewRect);
        DrawGridOverlay(previewRect);
    }

    private static void DrawPreviewTexture(Rectangle previewRect) {
        
        if (AppState.ResizeIndex == 0 && AppState.FilterIndex != 0) {
            
            SetShaderValue(_resizeShader, _resizeTexelLocation, GetTexelSize(AppState.PreviewWidth, AppState.PreviewHeight), ShaderUniformDataType.Vec2);
            BeginShaderMode(_resizeShader);
            DrawTiledPreview(previewRect);
            EndShaderMode();
            
            return;
        }

        DrawTiledPreview(previewRect);
    }

    private static void DrawTiledPreview(Rectangle previewRect) {
        
        if (AppState.TilePreviewIndex == 0) {
            
            DrawPreviewTarget(previewRect);
            return;
        }

        var tileWidth = previewRect.Width / 3f;
        var tileHeight = previewRect.Height / 3f;

        for (var row = 0; row < 3; row++) {
            
            for (var col = 0; col < 3; col++) {
                
                var tileRect = new Rectangle(
                    
                    previewRect.X + col * tileWidth,
                    previewRect.Y + row * tileHeight,
                    tileWidth,
                    tileHeight
                );
                
                DrawPreviewTarget(tileRect);
            }
        }
    }

    private static void DrawPreviewTarget(Rectangle previewRect) =>
        DrawTexturePro(GetDisplayTexture(), new Rectangle(0, 0, AppState.PreviewWidth, -AppState.PreviewHeight), previewRect, default, 0, Color.White);

    private static void DrawGridOverlay(Rectangle previewRect) {
        
        if (AppState.GridOpacity <= 0.001f) return;

        var (cols, rows) = AppState.GetGridDimensions();
        var baseColor = AppState.GridColors[AppState.GridColorIndex];
        var gridColor = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)(AppState.GridOpacity * 255));

        for (var i = 0; i <= cols; i++) {
            
            var x = previewRect.X + i * (previewRect.Width / cols);
            DrawLineV(new Vector2(x, previewRect.Y), new Vector2(x, previewRect.Y + previewRect.Height), gridColor);
        }

        for (var i = 0; i <= rows; i++) {
            
            var y = previewRect.Y + i * (previewRect.Height / rows);
            DrawLineV(new Vector2(previewRect.X, y), new Vector2(previewRect.X + previewRect.Width, y), gridColor);
        }
    }

    private static void SetHomographyUniforms(Vector3 h0, Vector3 h1, Vector3 h2) {
        
        SetShaderValue(_imageShader, ShaderLocations[(int)ShaderUniform.H0], h0, ShaderUniformDataType.Vec3);
        SetShaderValue(_imageShader, ShaderLocations[(int)ShaderUniform.H1], h1, ShaderUniformDataType.Vec3);
        SetShaderValue(_imageShader, ShaderLocations[(int)ShaderUniform.H2], h2, ShaderUniformDataType.Vec3);
    }

    private static void UploadShaderUniforms(Texture2D source) {
        
        SetFloatUniform(ShaderUniform.Brightness, AppState.Brightness);
        SetFloatUniform(ShaderUniform.Contrast, AppState.Contrast);
        SetFloatUniform(ShaderUniform.Saturation, AppState.Saturation);
        SetFloatUniform(ShaderUniform.Sharpness, AppState.Sharpness);
        SetFloatUniform(ShaderUniform.Gamma, AppState.Gamma);
        SetShaderValue(_imageShader, ShaderLocations[(int)ShaderUniform.Texel], GetTexelSize(source.Width, source.Height), ShaderUniformDataType.Vec2);
        SetFloatUniform(ShaderUniform.ColorBits, AppState.ColorBitsValues[AppState.ColorBitsIndex]);
        SetFloatUniform(ShaderUniform.Noise, AppState.Noise);
        SetFloatUniform(ShaderUniform.Bleed, AppState.Bleed);
        SetFloatUniform(ShaderUniform.Dither, AppState.Dither);
        SetFloatUniform(ShaderUniform.Jitter, AppState.Jitter);
        SetFloatUniform(ShaderUniform.ChromaShift, AppState.ChromaShift);
        SetFloatUniform(ShaderUniform.SeamlessX, 0f);
        SetFloatUniform(ShaderUniform.SeamlessY, 0f);
        SetFloatUniform(ShaderUniform.SeamWidth, AppState.SeamWidth);
    }

    private static void SetFloatUniform(ShaderUniform uniform, float value) =>
        SetShaderValue(_imageShader, ShaderLocations[(int)uniform], value, ShaderUniformDataType.Float);

    private static Vector2 GetTexelSize(int width, int height) => new(1f / width, 1f / height);
    
    private static bool IsAltDown() => IsKeyDown(KeyboardKey.LeftAlt) || IsKeyDown(KeyboardKey.RightAlt);
    
    private static bool NeedsSeamlessProcessing() => AppState.SeamlessX > 0.001f || AppState.SeamlessY > 0.001f;

    private static Texture2D GetDisplayTexture() => NeedsSeamlessProcessing() ? _displayTarget.Texture : AppState.PreviewTarget.Texture;

    private static void RenderDisplayTexture(RenderTexture2D sourceTarget, int width, int height) {
        
        if (!NeedsSeamlessProcessing()) return;

        SetTextureFilter(_displayTarget.Texture, AppState.PreviewFilter);
        BeginTextureMode(_displayTarget);
        ClearBackground(new Color(0, 0, 0, 0));
        BeginShaderMode(_seamlessShader);
        SetShaderValue(_seamlessShader, _seamlessTexelLocation, GetTexelSize(width, height), ShaderUniformDataType.Vec2);
        SetShaderValue(_seamlessShader, _seamlessXLocation, AppState.SeamlessX, ShaderUniformDataType.Float);
        SetShaderValue(_seamlessShader, _seamlessYLocation, AppState.SeamlessY, ShaderUniformDataType.Float);
        SetShaderValue(_seamlessShader, _seamWidthLocation, AppState.SeamWidth, ShaderUniformDataType.Float);
        DrawTexturePro(sourceTarget.Texture, new Rectangle(0, 0, width, -height), new Rectangle(0, 0, width, height), default, 0, Color.White);
        EndShaderMode();
        EndTextureMode();
    }

    private static void ExportCurrentImage() {
        
        if (!AppState.SourceTexture.HasValue) return;
        if (!TryGetHomography(out var h0, out var h1, out var h2)) return;

        var exportPath = PromptExportPath();
        if (string.IsNullOrEmpty(exportPath)) return;

        var (exportWidth, exportHeight) = AppState.GetOutputSize();
        var exportTarget = LoadRenderTexture(exportWidth, exportHeight);
        
        RenderToTexture(exportTarget, AppState.SourceTexture.Value, exportWidth, exportHeight, h0, h1, h2);
        
        var finalTarget = exportTarget;
        
        RenderTexture2D seamlessTarget = default;
        
        if (NeedsSeamlessProcessing()) {
            
            seamlessTarget = LoadRenderTexture(exportWidth, exportHeight);
            SetTextureFilter(seamlessTarget.Texture, AppState.PreviewFilter);
            BeginTextureMode(seamlessTarget);
            ClearBackground(new Color(0, 0, 0, 0));
            BeginShaderMode(_seamlessShader);
            SetShaderValue(_seamlessShader, _seamlessTexelLocation, GetTexelSize(exportWidth, exportHeight), ShaderUniformDataType.Vec2);
            SetShaderValue(_seamlessShader, _seamlessXLocation, AppState.SeamlessX, ShaderUniformDataType.Float);
            SetShaderValue(_seamlessShader, _seamlessYLocation, AppState.SeamlessY, ShaderUniformDataType.Float);
            SetShaderValue(_seamlessShader, _seamWidthLocation, AppState.SeamWidth, ShaderUniformDataType.Float);
            DrawTexturePro(exportTarget.Texture, new Rectangle(0, 0, exportWidth, -exportHeight), new Rectangle(0, 0, exportWidth, exportHeight), default, 0, Color.White);
            EndShaderMode();
            EndTextureMode();
            finalTarget = seamlessTarget;
        }

        var image = LoadImageFromTexture(finalTarget.Texture);
        
        ImageFlipVertical(ref image);
        ExportImage(image, exportPath);
        UnloadImage(image);
        
        if (seamlessTarget.Id != 0) UnloadRenderTexture(seamlessTarget);
        
        UnloadRenderTexture(exportTarget);
    }

    private static string? PromptExportPath() {
        
        var defaultName = GetDefaultExportFileName();
        var defaultDirectory = GetDefaultExportDirectory();
        
        using var dialog = new NativeFileDialog()
            .SaveFile()
            .AddFilter("JPEG Image", "jpg,jpeg")
            .AddFilter("PNG Image", "png");

        var result = dialog.Open(out string? outputPath, defaultDirectory, defaultName);
        if (result != DialogResult.Okay || string.IsNullOrWhiteSpace(outputPath)) return null;
        
        return NormalizeExportPath(outputPath);
    }

    private static string GetDefaultExportFileName() {
        
        var baseName = string.IsNullOrEmpty(AppState.SourcePath) ? "out" : Path.GetFileNameWithoutExtension(AppState.SourcePath);
        return $"{baseName}_rect_{DateTime.Now:yyyyMMdd_HHmmss}.{AppState.GetExportExtension()}";
    }

    private static string GetDefaultExportDirectory() {
        
        if (string.IsNullOrEmpty(AppState.SourcePath)) return Directory.GetCurrentDirectory();
        
        var fullPath = Path.GetFullPath(AppState.SourcePath);
        return Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
    }

    private static string NormalizeExportPath(string outputPath) {
        
        var extension = Path.GetExtension(outputPath);
        
        if (string.IsNullOrEmpty(extension)) return $"{outputPath}.{AppState.GetExportExtension()}";

        if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)) {
            
            AppState.ExportFormatIndex = 1;
            return outputPath;
        }

        if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return $"{outputPath}.{AppState.GetExportExtension()}";
        
        AppState.ExportFormatIndex = 0;
        
        return outputPath;
    }
}
