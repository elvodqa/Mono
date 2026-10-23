using System.Drawing;
using Mono;
using Mono.Audio;
using Mono.Graphics;
using OpenTK.Mathematics;
using SDL;

namespace BulletGame;


public class BulletGame : Game
{
    private class Bullet
    {
        public int XTween;
        public int YTween;
        public float Scale;
        public Vector2 Position;
        public Vector2 FinalPosition;
    }

    private enum PlayerState
    {
        Aiming,
        Shooting,
        Reloading,
    }

    private Texture rectTexture;
    
    private Texture navTexture;
    private Texture gunCursorTexture;
    private Texture bulletTexture;
    private Texture cylinderTexture;
    private bool cylinderSpinning = false;
    private float cursorRotation = 0;
    private float cursorScale = 1;
    private float cylinderRotation = 0;

    private Vector2 cursorPosition = new Vector2(0, 0);

    private bool bulletLimit = true;
    private Audio gunshotAudio; 
    private Audio gunAmmoInsertAudio; 

    private float bulletSpeed = 0.6f;
    private List<Bullet> bullets = new();

    private Texture playerTexture;
    private Texture playerTexture_aiming;
    private Texture playerTexture_shooting;
    private Texture playerTexture_reloading;

    private PlayerState playerState = PlayerState.Aiming;
    private float playerSpriteCountdown = 0;
    private Action? playerCountdownCallback = null;
    
    private int playerShootingCountdown = -333;

    private RectangleF playfieldRect;
    private RenderTarget playfieldTarget;
    private Vector2 prevPlayfieldSize = new Vector2(0, 0);

    private List<Card> cardsOnDeck = new();
    
    public override void Load()
    {
        rectTexture = new Texture(RootTitleStorage, "Assets/Textures/rect.png");
        
        navTexture = new Texture(RootTitleStorage, "Assets/Textures/de_nav.png", FilterMode.Linear, WrapMode.ClampToEdge);
        gunCursorTexture = new Texture(RootTitleStorage, "Assets/Textures/gun_cursor.png");
        bulletTexture = new Texture(RootTitleStorage, "Assets/Textures/bullet.png");
        cylinderTexture = new Texture(RootTitleStorage, "Assets/Textures/cylinder.png");
        
        playerTexture_aiming = new Texture(RootTitleStorage, "Assets/Textures/Player/aiming.png");
        playerTexture_shooting = new Texture(RootTitleStorage, "Assets/Textures/Player/shooting.png");
        playerTexture_reloading = new Texture(RootTitleStorage, "Assets/Textures/Player/reloading.png");

        gunshotAudio = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/gunshot.mp3");
        gunAmmoInsertAudio = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/gun_insert_ammo.mp3");
        
        /*TweenSystem.Tween(f => cursorRotation = f, 10, -10, 1, new TweenOptions()
        {
            Func = EasingFunctions.OutExpo,
            Loops = -1,
            Yoyo = true,
        });*/
        
        TrackVariable("Live bullets", () => bullets.Count);
        
        RegisterCommand("infinite_bullets", args =>
        {
            if (args.Count != 1) return;
            int.TryParse(args[0], out var value);
            if (value == 0) bulletLimit = true;
            if (value == 1) bulletLimit = false;
        });


        AudioManager.SampleVolume = 0.05f;

        Window.CursorVisible = false;

        playerTexture = playerTexture_aiming;

        Window.Resizable = false;
        Window.Size = new Vector2i(1280, 720);
        Window.Title = "Bullet Game";

        playfieldTarget = new RenderTarget(300, 300);
        
        cardsOnDeck.Add(new Card());
        cardsOnDeck.Add(new Card());
        cardsOnDeck.Add(new Card());
        cardsOnDeck.Add(new Card());
        
        base.Load();
    }

    public override void Update(double elapsedFrameTime)
    {
        base.Update(elapsedFrameTime);

        var pfposition = new Vector2(Window.SizeInPixels.X / 4f, Window.SizeInPixels.Y / 10f);
        var pfsize = new Vector2(Window.SizeInPixels .X/ 1.5f, Window.SizeInPixels.Y / 1.5f);
        playfieldRect = new RectangleF(pfposition.X, pfposition.Y, pfsize.X, pfsize.Y);
        if (playfieldTarget.Width != (int)prevPlayfieldSize.X || playfieldTarget.Height != (int)prevPlayfieldSize.Y)
        {
            playfieldTarget.Resize((int)playfieldRect.Width, (int)playfieldRect.Height);
        }
        
        
        if (Input.IsMousePressed(SDLButton.SDL_BUTTON_LEFT) && (!bulletLimit || bullets.Count == 0) && playfieldRect.Contains(Input.MouseX, Input.MouseY))
        {
            TweenSystem.Tween(f => cursorRotation = f, 0, 360, 1, new TweenOptions()
            {
                Func = EasingFunctions.OutBack,
            });
            
            TweenSystem.Tween(f => cursorScale = f, 1, 1.5f, 0.3f, new TweenOptions()
            {
                Func = EasingFunctions.OutBack,
                Yoyo = true,
            });
            
            Bullet bullet = new Bullet()
            {
                Position = new Vector2(0, 0),
                FinalPosition = new Vector2(Input.MouseX, Input.MouseY),
            };
            bullet.XTween = TweenSystem.Tween(f => bullet.Position.X = f, 0, bullet.FinalPosition.X, bulletSpeed, new TweenOptions()
            {
                Func = EasingFunctions.InExpo,
            });
            bullet.YTween = TweenSystem.Tween(f => bullet.Position.Y = f, GraphicsDevice.FramebufferSize.Y, bullet.FinalPosition.Y, bulletSpeed, new TweenOptions()
            {
                Func = EasingFunctions.InExpo,
            });
            TweenSystem.Tween(f => bullet.Scale = f, 1, 0.5f, bulletSpeed, new TweenOptions()
            {
                Func = EasingFunctions.InExpo,
            });
            bullets.Add(bullet);
            
            TweenSystem.Tween(f => cylinderRotation = f, cylinderRotation, cylinderRotation + 60, .250f, new TweenOptions()
            {
                Func = EasingFunctions.OutBack,
            });

            playerShootingCountdown = TweenSystem.Tween(f => { }, 0, 1, 0.3f, new TweenOptions());

            playerSpriteCountdown = 300;
            playerState = PlayerState.Shooting;
            playerCountdownCallback = () =>
            {
                playerState = PlayerState.Aiming;
            };
            
            AudioManager.PlaySample(gunshotAudio);
        }

        if (playerSpriteCountdown != -333f)
        {
            playerSpriteCountdown -= (float)elapsedFrameTime;

            if (playerSpriteCountdown <= 0)
            {
                playerCountdownCallback?.Invoke();
                playerCountdownCallback = null;
                playerSpriteCountdown = -333f;
            }
            
        }

        if (Input.IsMousePressed(SDLButton.SDL_BUTTON_RIGHT) && playerSpriteCountdown == -333f)
        {
            playerSpriteCountdown = 300;
            playerState = PlayerState.Reloading;
            playerCountdownCallback = () => playerState = PlayerState.Aiming;
            AudioManager.PlaySample(gunAmmoInsertAudio);
            
            TweenSystem.Tween(f => cylinderRotation = f, cylinderRotation, cylinderRotation - 60, 0.150f, new TweenOptions()
            {
                Func = EasingFunctions.OutBack,
            });
        }
        
        cursorPosition = Vector2.Lerp(cursorPosition, new Vector2(Input.MouseX, Input.MouseY), new Vector2(0.2f, 0.2f));

        if (TweenSystem.IsRunning(playerShootingCountdown)) playerTexture = playerTexture_shooting;
        else playerTexture = playerTexture_aiming;

        playerTexture = playerState switch {
            PlayerState.Aiming => playerTexture_aiming,
            PlayerState.Shooting => playerTexture_shooting,
            PlayerState.Reloading => playerTexture_reloading,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        foreach (var bullet in bullets.ToList())
        {
            if (!TweenSystem.IsRunning(bullet.XTween) && !TweenSystem.IsRunning(bullet.YTween))
            {
                bullets.Remove(bullet);
            }
        }

        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_R))
        {
            var roll = Random.Shared.Next(4, 12);
            cylinderSpinning = true;
            TweenSystem.Tween(f => cylinderRotation = f, cylinderRotation, cylinderRotation + 60 * roll, 0.150f * roll, new TweenOptions()
            {
                Func = EasingFunctions.OutBack,
                Callback = () => cylinderSpinning = false,
            });
        }

        if (cylinderSpinning)
        {
            if (Math.Abs(cylinderRotation % 60) < 10)
            {
                AudioManager.PlaySample(gunAmmoInsertAudio);
            }
        }
        
        prevPlayfieldSize = pfsize;
    }

    private double time = 0;

    public override void Draw(double elapsedFrameTime)
    {
        time += elapsedFrameTime;
        
        base.Draw(elapsedFrameTime);
        
        
        
        GraphicsDevice.PushRenderTarget(playfieldTarget);
        GraphicsDevice.SetViewport(new Rectangle(0, 0, (int)playfieldRect.Width,
            (int)playfieldRect.Height));
        GraphicsDevice.Clear(Color4.Pink);
        SpriteBatch.Begin(SpriteBatch.Make2DProjection(new(playfieldRect.Width, playfieldRect.Height)), Matrix4.Identity);
        
        DrawRect(new RectangleF((float)(300 * Math.Cos(time/100)), 0, 300, 300), Color4.White, 0, Vector2.Zero);
        
        SpriteBatch.End();
        GraphicsDevice.PopRenderTarget();
        
        
        
        GraphicsDevice.SetViewport(new Rectangle(0, 0, (int)GraphicsDevice.FramebufferSize.X, (int)GraphicsDevice.FramebufferSize.Y));
        GraphicsDevice.Clear(Color4.Gray);
        SpriteBatch.Begin(SpriteBatch.MakeDefault2DProjection(), Matrix4.Identity);
        
        SpriteBatch.Draw(playfieldTarget, playfieldRect, Color4.White, 0, Vector2.Zero); 
        
        var navWidth = GraphicsDevice.FramebufferSize.X / 1.5;
        var navHeight = navWidth / navTexture.Width * navTexture.Height;
        SpriteBatch.Draw(navTexture, new Rectangle(GraphicsDevice.FramebufferSize.X, GraphicsDevice.FramebufferSize.Y, (int)navWidth, (int)navHeight), 
            Color4.White, 0, new(1, 1));
        
        var cylinderSize = GraphicsDevice.FramebufferSize.X / 4;
        var cylinderOffset = cylinderSize / 2;
        
        var playerHeight = GraphicsDevice.FramebufferSize.X / 3f;
        var playerWidth = playerHeight / playerTexture_aiming.Height * playerTexture_aiming.Width;
        SpriteBatch.Draw(playerTexture, 
            new(cylinderOffset, GraphicsDevice.FramebufferSize.Y - cylinderOffset * 2 + (cylinderSize / 4f), playerWidth, playerHeight), 
            Color4.White, 0, new Vector2(0.5f, 1.0f));
        
        SpriteBatch.Draw(cylinderTexture, new RectangleF(cylinderOffset, GraphicsDevice.FramebufferSize.Y - cylinderOffset, cylinderSize, cylinderSize), Color4.White, cylinderRotation, new Vector2(0.5f, 0.5f));

        foreach (var bullet in bullets)
        {
            var start = new Vector2(0, GraphicsDevice.FramebufferSize.Y);
            var diff = bullet.FinalPosition - start;
            var angle = MathF.Atan2(diff.Y, diff.X);

            var bulletSize = 600;
            
            SpriteBatch.Draw(bulletTexture, new RectangleF(bullet.Position.X, bullet.Position.Y, bulletSize * bullet.Scale, bulletSize * bullet.Scale),
                Color4.White, angle * MathHelper.RadToDeg - 180, new Vector2(0.5f, 0.5f));
        }
        
        var cardWidth = GraphicsDevice.FramebufferSize.X / 10f;
        var cardHeight = cardWidth * 1.5f;

        var leftLimit = playfieldRect.X + cardWidth / 2;
        var rightLimit = (playfieldRect.X + playfieldRect.Width) - cardWidth / 2;
        var cardDelta = (rightLimit - leftLimit) / (cardsOnDeck.Count - 1);
        var i = 0;
        foreach (var card in cardsOnDeck)
        {
            DrawRect(new RectangleF(leftLimit + i * cardDelta, Input.MouseY, cardWidth, cardHeight), Color4.Black with {W=0.6f}, 0, new Vector2(0.5f, 0.5f));

            i++;
        }

        
        var cursorSize = GraphicsDevice.FramebufferSize.Y / 15;
        SpriteBatch.Draw(gunCursorTexture, new RectangleF(cursorPosition.X, cursorPosition.Y, cursorSize * cursorScale, cursorSize * cursorScale),
            Color4.White, cursorRotation, new Vector2(0.5f, 0.5f));
        
        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_A)) cardsOnDeck.Add(new Card());
        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_D) && cardsOnDeck.Count != 0)
            cardsOnDeck.Remove(cardsOnDeck.Last());
        
        SpriteBatch.End();
    }

    public void DrawRect(RectangleF rect, Color4<Rgba> color, float rotation, Vector2 origin) =>
        SpriteBatch.Draw(rectTexture, rect, color, rotation, origin);
}