# FontStashPlus
A modernised version of [FontStash.NET](https://github.com/MatijaBrown/FontStash.NET), that conforms to standard .NET code style
practices, provides API cleanups, small optimisation and refactors.

## Usage / Examples
> Examples can be found in the 'samples' directory.

### Creating a new context
Information about the desired font atlas and the callbacks used for rendering
are stored in the ```FontParams``` struct.
```cs
var fontParams = new FontParams()
{
	// The initial dimensions of the font atlas
	Width = 512,
	Height = 512,
	// Where point (0, 0) is located on the atlas
	Flags = FontFlags.ZeroTopLeft,
	// Callbacks for creating, resizing, updating, drawing and deleting the atlas
	RenderCreate = OnCreate,
	RenderUpdate = OnUpdate,
	RenderDraw = OnDraw,
	RenderDelete = OnDelete
};
```
Examples for implementations of these methods can be found in either the
``src/FontStash.NET.GL.Legacy`` (for people not used to OpenGL) or
``src/FontStash.NET.GL`` (for how to render in modern OpenGL) projects.

To finally create the FontStash context and instance, do the following:
```cs
var fontManager = new FontManager(fontParams);
```

Now, one can use the FontStash similarly to [memononen/fontstash](https://github.com/memononen/fontstash),
apart from not having to pass a context with every call, as the context information is stored within the FontManager instance in an OOP fashion.
```cs
// Create a new font
// The last parameter should usually be set to zero when using StbTrueType font indices.
int testFontId = fontManager.CreateFont("testFont", "./fonts/verdana.ttf", 0);

// Rendering method here
// Colours are stored as 4 bytes (rgba) next to each other in a uint.
uint fontColourRed = Utils.RgbaToUint(255, 0, 0, 255);

// Render "I am the walrus!"
fontManager.SetFont(testFontId);
fontManager.SetSize(72.0f);
fontManager.SetColour(fontColourRed);
// FontStash.DrawText(float, float string) returns the end X-Position of the rendered string on the window.
float endX = fontManager.DrawText(20, 100, "I am the walrus!");

// render the font atlas in it's current state
fontManager.DrawDebug(800, 200);
```

#### OpenGL utillities
When using OpenGL FontStash.NET, the same as fontstash, provids utillity classes
to aid rendering. They are located in either ``src/FontStash.NET.GL.Legacy`` for legacy OpenGL
and ``src/FontStash.NET.GL`` for modern OpenGL respectively. These use [Silk.NET](https://github.com/dotnet/Silk.NET)
for rendering, so a compatible OpenGL object must be parsed.
```cs
// 'gl' should be set to the GL context, usually created with Silk.Net's 'window.CreateOpenGL()' method
var glFont = new GLFont(gl);
FontManager fontManager = glFont.Create(512, 512, FontFlags.ZeroTopLeft);
```
The example seen above has the same effect as the "manual" example.

### Examples
Two example projects using OpenGL can be found in 'samples' directory.<br><br>
*Without debug atlas displayed*:<br>
![Example without Debug](./docs/images/example_nodebug.PNG)
<br>
*With debug atlas displayed*:<br>
![Example without Debug](./docs/images/example_debug.PNG)

## Credits
[FontStash.NET](https://github.com/MatijaBrown/FontStash.NET) this fork's base implementation <br><br>
[mnemononen/fontstash](https://github.com/memononen/fontstash) the original implementation is a port of <br><br>
[StbTrueTypeSharp](https://github.com/StbSharp/StbTrueTypeSharp) for the StbTrueType implementation<br><br>
[Silk.NET](https://github.com/dotnet/Silk.NET) for the OpenGL implementation in the helper classes.<br><br>

## License
> FontStash.NET uses the MIT-License<br><br>
> fontstash uses the ZLib-License<br><br>
