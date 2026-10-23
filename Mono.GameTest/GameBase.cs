using System.Drawing;
using Mono.Graphics;
using Mono.Util;
using OpenTK.Mathematics;
using SDL;
using Buffer = Mono.Graphics.Buffer;

namespace Mono.GameTest;


public class GameBase : Game
{
    private Texture _texture;
    private Font _font;
    private double time = 0;
    private Texture _testTexture;

    private Vector2i pos;

    private Camera2D camera;


    private Level level;
    private Player actor;
    private Solid solid;

    public override void Load()
    {
        base.Load();
        _testTexture = new Texture(RootTitleStorage, "Assets/Textures/67.png");

        _font = Font.Load(RootTitleStorage, "Assets/Fonts/roboto/roboto.png", "Assets/Fonts/roboto/roboto.json");

        camera = new Camera2D(GraphicsDevice.FramebufferSize);
        camera.Position = Vector2.Zero;

        level = new Level();

        actor = new Player();
        actor.BboxSize = new Vector2i(300, 300);

        solid = new Solid();
        solid.Position = new Vector2i(600, 1200);
        solid.BboxSize = new Vector2i(10000, 200);
        
        level.AddEntity(actor);
        level.AddEntity(solid);
        
        TrackVariable("OnFloor", () => actor.OnFloor);
        
        Buffer vertexBuffer = new Buffer(GraphicsDevice, typeof(uint), 2, BufferType.Vertex, BufferUse.Static);
        vertexBuffer.SetData([1, 1]);
    }
    
    bool right = true;

    public override void Update(double elapsedFrameTime)
    {
        base.Update(elapsedFrameTime);

        var hor = Convert.ToInt32(Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_D)) -
                  Convert.ToInt32(Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_A));
        var ver = Convert.ToInt32(Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_S)) -
                  Convert.ToInt32(Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_W));
        var speed = 10;
        //pos += new Vector2i(hor * speed, ver * speed);

        if (Input.IsMouseDown(SDLButton.SDL_BUTTON_RIGHT)) camera.Position += new Vector2(Input.DeltaX, Input.DeltaY);
        
        actor.Update(elapsedFrameTime);

        camera.Position = actor.Position;
        camera.ViewportSize = GraphicsDevice.FramebufferSize;

        // if (solid.Position.X >= 1000)
        // {
        //     right = false;
        // }
        // if (solid.Position.X <= 0)
        // {
        //     right = true;
        // }
        // solid.Move(right ? 2 : -2, right ? 2 : -2);
    }

    public override void Draw(double elapsedFrameTime)
    {
        base.Draw(elapsedFrameTime);

        time += elapsedFrameTime/10f;
        
        var size = GraphicsDevice.FramebufferSize;
        GraphicsDevice.SetViewport(new Rectangle(0, 0, (int)size.X, (int)size.Y));
        GraphicsDevice.Clear(Color4.Cornflowerblue);
        
        SpriteBatch.Begin(SpriteBatch.MakeDefault2DProjection(), camera.View);

        var foo = 32;
        for (int i = 0; i < foo; i++)
        {
            for (int j = 0; j < foo; j++)
            {
                SpriteBatch.Draw(_testTexture, new Rectangle(i*64, j*64, 64, 64), Color4.Darkblue, (float)time, new Vector2(0.5f, 0.5f));
            }
        }
        
        // draw actor
        SpriteBatch.Draw(_testTexture, new Rectangle(actor.Position.X, actor.Position.Y, actor.BboxSize.X, actor.BboxSize.Y), Color4.White, 0, new Vector2(0.5f, 0.5f));
        
        // solid
        SpriteBatch.Draw(_testTexture, new Rectangle(solid.Position.X, solid.Position.Y, solid.BboxSize.X, solid.BboxSize.Y), Color4.White, 0, new Vector2(0.5f, 0.5f));

        var _pos = camera.ScreenToWorld(new(Input.MouseX, Input.MouseY));
        SpriteBatch.Draw(_testTexture, new Rectangle((int)_pos.X, (int)_pos.Y, 200, 200), Color4.Cyan, (float)time*2, new Vector2(0.5f, 0.5f));
        
        SpriteBatch.DrawText(_font, "Hello, World!", new Vector2(10, 10), 40, Color4.Black);
        
        SpriteBatch.End();
    }
}