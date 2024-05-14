using Silk.NET.Maths;
using Silk.NET.Windowing;
using FontStash.NET;
using FontStash.NET.GL;
using Silk.NET.OpenGL;

namespace SilkGL;

internal static class Program
{
    private static IWindow window;
    private static GL gl;
    private static int fontNormal;
    private static int fontItalic;
    private static int fontBold;
    private static FontManager fontManager;

    public static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "FontStash.NET - SilkGL";
        options.FramesPerSecond = 60;
        options.VSync = false;

        window = Window.Create(options);
        window.Load += OnLoad;
        window.Render += OnRender;
        window.Run();
    }
    
    private static void OnLoad()
    {
        gl = window.CreateOpenGL();
        fontNormal = FontManager.Invalid;
        fontItalic = FontManager.Invalid;
        fontBold = FontManager.Invalid;

        var glFont = new GlFont(gl);
        fontManager = glFont.Create(512, 512, FontFlags.ZeroTopLeft);

        var ft = fontManager.AddFont("icons", "./fonts/entypo.ttf", 0);

        fontNormal = fontManager.AddFont("sans", "./fonts/DroidSerif-Regular.ttf", 0);
        if (fontNormal == FontManager.Invalid)
        {
            Console.Error.WriteLine("Could not add font normal!");
            Environment.Exit(-1);
        }
        fontItalic = fontManager.AddFont("sans-italic", "./fonts/DroidSerif-Italic.ttf", 0);
        if (fontNormal == FontManager.Invalid)
        {
            Console.Error.WriteLine("Could not add font italic!");
            Environment.Exit(-1);
        }
        fontBold = fontManager.AddFont("sans-bold", "./fonts/DroidSerif-Bold.ttf", 0);
        if (fontNormal == FontManager.Invalid)
        {
            Console.Error.WriteLine("Could not add font bold!");
            Environment.Exit(-1);
        }
        
        gl.ClearColor(0.3f, 0.3f, 0.32f, 1.0f);
    }
    
    private static void OnRender(double deltaTime)
    {
        gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        fontManager.SetFont(fontNormal);
        fontManager.DrawText(0.0f, 0.0f, "HELLO WORLD!");
    }
}