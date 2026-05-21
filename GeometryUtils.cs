using System.Numerics;
using OpenCvSharp;
using Raylib_cs;
using static Raylib_cs.Raylib;

internal readonly record struct AppLayout(int Height, int PanelX, int HalfWidth);

internal static class GeometryUtils {
    
    public static AppLayout GetLayout() {
        
        var width = GetScreenWidth();
        var height = GetScreenHeight();
        var panelX = width - AppState.PanelWidth;
        
        return new AppLayout(height, panelX, panelX / 2);
    }

    public static Rectangle GetInputRect(AppLayout layout, Texture2D texture) =>
        FitAspect(texture.Width, texture.Height, 10, 50, layout.HalfWidth - 20, layout.Height - 70);

    public static Rectangle GetPreviewRect(AppLayout layout, int outputWidth, int outputHeight) =>
        FitAspect(outputWidth, outputHeight, layout.HalfWidth + 10, 50, layout.PanelX - layout.HalfWidth - 20, layout.Height - 70);

    private static Rectangle FitAspect(int width, int height, int x, int y, int maxWidth, int maxHeight) {
        
        var scale = Math.Min((float)maxWidth / width, (float)maxHeight / height);
        
        return new Rectangle(
            
            x + maxWidth / 2f - width * scale / 2f,
            y + maxHeight / 2f - height * scale / 2f,
            width * scale,
            height * scale
        );
    }

    public static Vector2 ToScreenPoint(Rectangle rect, Vector2 uv) => new(rect.X + uv.X * rect.Width, rect.Y + uv.Y * rect.Height);

    public static Rectangle FullTextureRect(Texture2D texture) => new(0, 0, texture.Width, texture.Height);

    public static Vector2 ClampUv(Vector2 point) => new(Math.Clamp(point.X, 0f, 1f), Math.Clamp(point.Y, 0f, 1f));

    public static Vector2 SnapUv(Vector2 point, float step) =>
        
        ClampUv(new Vector2(
            
            MathF.Round(point.X / step) * step,
            MathF.Round(point.Y / step) * step
        ));

    public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b) {
        
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        
        if (lengthSquared <= float.Epsilon) return Vector2.Distance(point, a);

        var t = Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        var projection = a + ab * t;
        
        return Vector2.Distance(point, projection);
    }

    public static bool IsConvex(Vector2[] points) => Cv2.IsContourConvex(ToCvPoints(points));

    public static bool TryCalculateHomography(Vector2[] points, out Vector3 c0, out Vector3 c1, out Vector3 c2) {
        
        try {
            
            using var matrix = Cv2.GetPerspectiveTransform(UnitSquarePoints, ToCvPoints(points));
            
            c0 = new Vector3((float)matrix.At<double>(0, 0), (float)matrix.At<double>(1, 0), (float)matrix.At<double>(2, 0));
            c1 = new Vector3((float)matrix.At<double>(0, 1), (float)matrix.At<double>(1, 1), (float)matrix.At<double>(2, 1));
            c2 = new Vector3((float)matrix.At<double>(0, 2), (float)matrix.At<double>(1, 2), (float)matrix.At<double>(2, 2));
            
            return true;
            
        } catch (OpenCVException) {
            
            c0 = default;
            c1 = default;
            c2 = default;
            
            return false;
        }
    }

    private static readonly Point2f[] UnitSquarePoints = [
        
        new(0f, 0f),
        new(1f, 0f),
        new(1f, 1f),
        new(0f, 1f)
    ];

    private static Point2f[] ToCvPoints(Vector2[] points) => [.. points.Select(point => new Point2f(point.X, point.Y))];
}
