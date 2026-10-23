using System.Drawing;
using Mono;
using Mono.Graphics;
using OpenTK.Mathematics;

namespace SceneTreeTest;

public class GameBase : Game
{
    public Texture whiteTexture;
    public float x;
    public float y;

    public override void Load()
    {
        base.Load();

        TweenSystem.Tween(f =>
        {
            x = f;
            y = f;
        }, 0, 800, 1, new TweenOptions()
        {
            Yoyo = true,
            Loops = -1, 
            Func = EasingFunctions.InCirc,
        });

    }

    public override void Draw(double elapsedFrameTime)
    {
        GraphicsDevice.Clear(Color4.Cornflowerblue);
        
        SpriteBatch.Begin(SpriteBatch.MakeDefault2DProjection(), Matrix4.Identity);
        SpriteBatch.Draw(Texture.White, new RectangleF(x, y, 100, 100), Color4.Wheat, 0, new Vector2(0, 0));
        SpriteBatch.End();
        
        base.Draw(elapsedFrameTime);
    }
}