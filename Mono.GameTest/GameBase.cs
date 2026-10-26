using System.Drawing;
using Mono.Graphics;
using Mono.Audio;
using GameAudio = global::Mono.Audio.Audio;
using Mono.Util;
using OpenTK.Mathematics;
using SDL;

namespace Mono.GameTest;

public class GameBase : Game
{
    private const int TileSize = 64;
    private const int LevelWidth = 6400;
    private const int LevelHeight = 960;
    private const float DesignWidth = 1280f;
    private const float DesignHeight = 720f;

    private static readonly string[] Map =
    {
        "....................................................................................................",
        "....................................................................................................",
        "....................................................................................................",
        "....................................................................................................",
        "...........................................oooo.....................................................",
        "..........................................######.........................................ooo........",
        "..................................oooo..............oooo................................#####.......",
        ".................ooo.............######............######.......................ooeoo...............",
        "................#####....eooo................................oooooe............######...............",
        ".........oooo...........######..............................#######....oooo.........................",
        "........######........................................................######........................",
        "....................................................................................................",
        "..S............e......o.................o........e........o..................o........e.......F.....",
        "#####################...###############...###############...################...#####################",
        "#####################...###############...###############...################...#####################",
    };

    private Texture _skyTexture = null!;
    private Texture _cloudTexture = null!;
    private Texture _grassTopTexture = null!;
    private Texture _grassTopLeftTexture = null!;
    private Texture _grassTopRightTexture = null!;
    private Texture _grassLeftTexture = null!;
    private Texture _grassRightTexture = null!;
    private Texture _grassCenterTexture = null!;
    private Texture _coinTexture = null!;
    private Texture _flagTexture = null!;
    private Texture _flagWonTexture = null!;
    private Texture _hudCoinTexture = null!;
    private Texture _heartTexture = null!;
    private Texture _heartEmptyTexture = null!;
    private Texture _pixelTexture = null!;
    private Texture _shadowTexture = null!;
    private Texture _playerIdleTexture = null!;
    private Texture _playerWalkATexture = null!;
    private Texture _playerWalkBTexture = null!;
    private Texture _playerJumpTexture = null!;
    private Texture _slimeWalkATexture = null!;
    private Texture _slimeWalkBTexture = null!;
    private Texture _slimeFlatTexture = null!;

    private GameAudio _jumpSound = null!;
    private GameAudio _coinSound = null!;
    private GameAudio _hurtSound = null!;
    private GameAudio _stompSound = null!;
    private GameAudio _winSound = null!;

    private Font _font = null!;
    private Camera2D _camera = null!;

    private Level _level = null!;
    private Player _player = null!;
    private Goal _goal = null!;
    private readonly List<Tile> _tiles = new();
    private readonly List<Coin> _coins = new();
    private readonly List<Slime> _enemies = new();

    private Vector2i _spawn;
    private Vector2i _checkpoint;
    private double _elapsed;
    private float _checkpointTimer;
    private int _coinsCollected;
    private int _lives;
    private bool _won;
    private bool _gameOver;

    public override void Load()
    {
        base.Load();

        Window.Title = "Mono Platformer";
        DebugInfo = false;

        _skyTexture = LoadTexture("Assets/Sprites/Backgrounds/Default/background_solid_sky.png");
        _cloudTexture = LoadTexture("Assets/Sprites/Backgrounds/Default/background_clouds.png");
        _grassTopTexture = LoadTexture("Assets/Sprites/Tiles/Default/terrain_grass_block_top.png");
        _grassTopLeftTexture = LoadTexture("Assets/Sprites/Tiles/Default/terrain_grass_block_top_left.png");
        _grassTopRightTexture = LoadTexture("Assets/Sprites/Tiles/Default/terrain_grass_block_top_right.png");
        _grassLeftTexture = LoadTexture("Assets/Sprites/Tiles/Default/terrain_grass_block_left.png");
        _grassRightTexture = LoadTexture("Assets/Sprites/Tiles/Default/terrain_grass_block_right.png");
        _grassCenterTexture = LoadTexture("Assets/Sprites/Tiles/Default/terrain_grass_block_center.png");
        _coinTexture = LoadTexture("Assets/Sprites/Tiles/Default/coin_gold.png");
        _flagTexture = LoadTexture("Assets/Sprites/Tiles/Default/flag_green_a.png");
        _flagWonTexture = LoadTexture("Assets/Sprites/Tiles/Default/flag_green_b.png");
        _hudCoinTexture = LoadTexture("Assets/Sprites/Tiles/Default/hud_coin.png");
        _heartTexture = LoadTexture("Assets/Sprites/Tiles/Default/hud_heart.png");
        _heartEmptyTexture = LoadTexture("Assets/Sprites/Tiles/Default/hud_heart_empty.png");
        _pixelTexture = LoadTexture("Assets/Textures/rect.png");
        _shadowTexture = LoadTexture("Assets/Textures/circle.png");
        _playerIdleTexture = LoadTexture("Assets/Sprites/Characters/Default/character_green_idle.png");
        _playerWalkATexture = LoadTexture("Assets/Sprites/Characters/Default/character_green_walk_a.png");
        _playerWalkBTexture = LoadTexture("Assets/Sprites/Characters/Default/character_green_walk_b.png");
        _playerJumpTexture = LoadTexture("Assets/Sprites/Characters/Default/character_green_jump.png");
        _slimeWalkATexture = LoadTexture("Assets/Sprites/Enemies/Default/slime_normal_walk_a.png");
        _slimeWalkBTexture = LoadTexture("Assets/Sprites/Enemies/Default/slime_normal_walk_b.png");
        _slimeFlatTexture = LoadTexture("Assets/Sprites/Enemies/Default/slime_normal_flat.png");

        _font = Font.Load(RootTitleStorage, "Assets/Fonts/roboto/roboto.png", "Assets/Fonts/roboto/roboto.json");

        _jumpSound = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/sfx_jump.ogg");
        _coinSound = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/sfx_coin.ogg");
        _hurtSound = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/sfx_hurt.ogg");
        _stompSound = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/sfx_bump.ogg");
        _winSound = AudioManager.LoadAudio(RootTitleStorage, "Assets/Sounds/sfx_magic.ogg");

        _camera = new Camera2D(GraphicsDevice.FramebufferSize);

        BuildLevel();

        TrackVariable("Coins", () => _coinsCollected);
        TrackVariable("Lives", () => _lives);
        TrackVariable("Grounded", () => _player.Grounded);
    }

    private Texture LoadTexture(string path)
    {
        return new Texture(RootTitleStorage, path, FilterMode.Linear, WrapMode.ClampToEdge, false);
    }

    private void BuildLevel()
    {
        _tiles.Clear();
        _coins.Clear();
        _enemies.Clear();

        _level = new Level();
        _coinsCollected = 0;
        _lives = 3;
        _elapsed = 0;
        _checkpointTimer = 0;
        _won = false;
        _gameOver = false;

        for (int y = 0; y < Map.Length; y++)
        {
            for (int x = 0; x < Map[y].Length; x++)
            {
                char cell = Map[y][x];
                Vector2i center = new(x * TileSize + TileSize / 2, y * TileSize + TileSize / 2);

                switch (cell)
                {
                    case '#':
                    {
                        Tile tile = new()
                        {
                            Position = center,
                            BboxSize = new Vector2i(TileSize, TileSize),
                            Texture = ChooseTileTexture(x, y)
                        };

                        _tiles.Add(tile);
                        _level.AddEntity(tile);
                        break;
                    }

                    case 'o':
                    {
                        Coin coin = new()
                        {
                            Position = center,
                            BboxSize = new Vector2i(30, 30),
                            Phase = (x * 37 + y * 61) % 628 / 100f
                        };

                        _coins.Add(coin);
                        _level.AddEntity(coin);
                        break;
                    }

                    case 'e':
                    {
                        Slime slime = new(_slimeWalkATexture, _slimeWalkBTexture, _slimeFlatTexture)
                        {
                            Position = new Vector2i(center.X, (y + 1) * TileSize - 19),
                            Direction = x % 2 == 0 ? 1 : -1,
                            PatrolLeft = center.X - 160,
                            PatrolRight = center.X + 160
                        };

                        _enemies.Add(slime);
                        _level.AddEntity(slime);
                        break;
                    }

                    case 'S':
                        _spawn = new Vector2i(center.X, (y + 1) * TileSize - 29);
                        break;

                    case 'F':
                        _goal = new Goal
                        {
                            Position = center,
                            BboxSize = new Vector2i(56, 64)
                        };

                        _level.AddEntity(_goal);
                        break;
                }
            }
        }

        _player = new Player(_playerIdleTexture, _playerWalkATexture, _playerWalkBTexture, _playerJumpTexture)
        {
            Position = _spawn
        };

        _player.Jumped = () => AudioManager.PlaySample(_jumpSound);
        _level.AddEntity(_player);

        _checkpoint = _spawn;
        UpdateCamera(0, true);
    }

    private Texture ChooseTileTexture(int x, int y)
    {
        bool above = IsSolid(x, y - 1);
        bool left = IsSolid(x - 1, y);
        bool right = IsSolid(x + 1, y);

        if (!above)
        {
            if (!left && !right) return _grassTopTexture;
            if (!left) return _grassTopLeftTexture;
            if (!right) return _grassTopRightTexture;
            return _grassTopTexture;
        }

        if (!left && !right) return _grassCenterTexture;
        if (!left) return _grassLeftTexture;
        if (!right) return _grassRightTexture;
        return _grassCenterTexture;
    }

    private static bool IsSolid(int x, int y)
    {
        if (y < 0 || y >= Map.Length) return false;
        if (x < 0 || x >= Map[y].Length) return false;
        return Map[y][x] == '#';
    }

    public override void Update(double elapsedFrameTime)
    {
        base.Update(elapsedFrameTime);

        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_R))
        {
            BuildLevel();
            return;
        }

        if (!_won && !_gameOver)
        {
            _player.Update(elapsedFrameTime);

            foreach (Slime slime in _enemies)
                slime.Update(elapsedFrameTime);

            CollectCoins();
            CheckEnemies();

            if (_player.Position.Y > LevelHeight + 160)
                Hurt();

            if (!_won && !_gameOver && _player.Overlaps(_goal))
            {
                _won = true;
                AudioManager.PlaySample(_winSound);
            }

            if (!_won && !_gameOver)
                _elapsed += elapsedFrameTime;

            UpdateCheckpoint(elapsedFrameTime);
        }

        UpdateCamera(elapsedFrameTime, false);
    }

    private void CollectCoins()
    {
        foreach (Coin coin in _coins)
        {
            if (coin.Collected || !_player.Overlaps(coin)) continue;

            coin.Collected = true;
            _coinsCollected++;
            AudioManager.PlaySample(_coinSound);
        }
    }

    private void CheckEnemies()
    {
        foreach (Slime slime in _enemies)
        {
            if (slime.Dead || slime.Finished || !_player.Overlaps(slime)) continue;

            bool stomped = _player.Velocity.Y > 0.12f &&
                           _player.Position.Y < slime.Position.Y - 6;

            if (stomped)
            {
                slime.Stomp();
                _player.Bounce();
                AudioManager.PlaySample(_stompSound);
            }
            else
            {
                Hurt();
                break;
            }
        }
    }

    private void Hurt()
    {
        if (_won || _gameOver || _player.Invulnerable > 0f) return;

        AudioManager.PlaySample(_hurtSound);
        _lives--;

        if (_lives <= 0)
        {
            _lives = 0;
            _gameOver = true;
            return;
        }

        _player.Respawn(_checkpoint);
        _checkpointTimer = 0;
        UpdateCamera(0, true);
    }

    private void UpdateCheckpoint(double elapsedFrameTime)
    {
        if (!_player.Grounded)
        {
            _checkpointTimer = 0;
            return;
        }

        _checkpointTimer += (float)elapsedFrameTime;

        if (_checkpointTimer < 350f) return;

        _checkpoint = _player.Position;
        _checkpointTimer = 0;
    }

    private void UpdateCamera(double elapsedFrameTime, bool snap)
    {
        Vector2 size = GraphicsDevice.FramebufferSize;
        float scale = MathF.Max(1f, size.Y / DesignHeight);

        _camera.Zoom = scale;
        _camera.ViewportSize = size;

        float halfWidth = size.X / scale * 0.5f;
        float halfHeight = size.Y / scale * 0.5f;

        Vector2 target = _player.Position;
        target.X = LevelWidth <= halfWidth * 2f
            ? LevelWidth * 0.5f
            : Math.Clamp(target.X, halfWidth, LevelWidth - halfWidth);
        target.Y = LevelHeight <= halfHeight * 2f
            ? LevelHeight * 0.5f
            : Math.Clamp(target.Y, halfHeight, LevelHeight - halfHeight);

        if (snap)
        {
            _camera.Position = target;
            return;
        }

        float smoothing = 1f - MathF.Exp(-(float)elapsedFrameTime / 85f);
        _camera.Position = Vector2.Lerp(_camera.Position, target, smoothing);
    }

    public override void Draw(double elapsedFrameTime)
    {
        base.Draw(elapsedFrameTime);

        var size = GraphicsDevice.FramebufferSize;
        GraphicsDevice.SetViewport(new Rectangle(0, 0, (int)size.X, (int)size.Y));
        GraphicsDevice.Clear(new Color4<Rgba>(0.76f, 0.89f, 1f, 1f));

        DrawBackground();
        DrawWorld();
        DrawHud();
    }

    private void DrawBackground()
    {
        SpriteBatch.Begin(SpriteBatch.Make2DProjection(new Vector2(DesignWidth, DesignHeight)), Matrix4.Identity);
        SpriteBatch.Draw(_skyTexture, new RectangleF(0, 0, DesignWidth, DesignHeight), Color4.White, 0f, Vector2.Zero);

        const float cloudSize = 512f;
        float offset = -(_camera.Position.X * 0.18f) % cloudSize;
        if (offset > 0f) offset -= cloudSize;

        for (float x = offset; x < DesignWidth + cloudSize; x += cloudSize)
        {
            SpriteBatch.Draw(
                _cloudTexture,
                new RectangleF(x, DesignHeight - cloudSize, cloudSize, cloudSize),
                Color4.White,
                0f,
                Vector2.Zero);
        }

        SpriteBatch.End();
    }

    private void DrawWorld()
    {
        SpriteBatch.Begin(SpriteBatch.MakeDefault2DProjection(), _camera.View);

        foreach (Tile tile in _tiles)
        {
            SpriteBatch.Draw(
                tile.Texture,
                new RectangleF(tile.Position.X - TileSize * 0.5f, tile.Position.Y - TileSize * 0.5f, TileSize, TileSize),
                Color4.White,
                0f,
                Vector2.Zero);
        }

        foreach (Coin coin in _coins)
        {
            if (coin.Collected) continue;

            float bob = MathF.Sin((float)_elapsed / 240f + coin.Phase) * 5f;

            SpriteBatch.Draw(
                _coinTexture,
                new RectangleF(coin.Position.X - 24f, coin.Position.Y - 24f + bob, 48f, 48f),
                Color4.White,
                0f,
                Vector2.Zero);
        }

        SpriteBatch.Draw(
            _won ? _flagWonTexture : _flagTexture,
            new RectangleF(_goal.Position.X - 32f, _goal.Position.Y - 32f, 64f, 64f),
            Color4.White,
            0f,
            Vector2.Zero);

        foreach (Slime slime in _enemies)
            slime.Draw(SpriteBatch);

        if (_player.Grounded)
        {
            SpriteBatch.Draw(
                _shadowTexture,
                new RectangleF(_player.Position.X - 23f, _player.Position.Y + 22f, 46f, 12f),
                new Color4<Rgba>(0f, 0f, 0f, 0.22f),
                0f,
                Vector2.Zero);
        }

        _player.Draw(SpriteBatch);

        SpriteBatch.End();
    }

    private void DrawHud()
    {
        SpriteBatch.Begin(SpriteBatch.Make2DProjection(new Vector2(DesignWidth, DesignHeight)), Matrix4.Identity);

        var panelColor = new Color4<Rgba>(0.05f, 0.07f, 0.11f, 0.55f);
        SpriteBatch.Draw(_pixelTexture, new RectangleF(24, 24, 420, 64), panelColor, 0f, Vector2.Zero);

        for (int i = 0; i < 3; i++)
        {
            SpriteBatch.Draw(
                i < _lives ? _heartTexture : _heartEmptyTexture,
                new RectangleF(40 + i * 38, 38, 36, 36),
                Color4.White,
                0f,
                Vector2.Zero);
        }

        SpriteBatch.Draw(_hudCoinTexture, new RectangleF(178, 36, 40, 40), Color4.White, 0f, Vector2.Zero);
        SpriteBatch.DrawText(_font, $"{_coinsCollected} / {_coins.Count}", new Vector2(226, 38), 30, Color4.White);

        SpriteBatch.Draw(_pixelTexture, new RectangleF(DesignWidth - 214, 24, 190, 64), panelColor, 0f, Vector2.Zero);
        SpriteBatch.DrawText(_font, "TIME", new Vector2(DesignWidth - 194, 30), 20, new Color4<Rgba>(0.75f, 0.82f, 0.9f, 1f));
        SpriteBatch.DrawText(_font, $"{_elapsed / 1000.0:0.0}s", new Vector2(DesignWidth - 194, 50), 28, Color4.White);

        SpriteBatch.Draw(_pixelTexture, new RectangleF(24, DesignHeight - 62, 540, 40), panelColor, 0f, Vector2.Zero);
        SpriteBatch.DrawText(_font, "A / D  MOVE     SPACE  JUMP     R  RESTART", new Vector2(40, DesignHeight - 56), 22,
            new Color4<Rgba>(0.92f, 0.95f, 1f, 1f));

        if (_won)
        {
            DrawOverlay("LEVEL COMPLETE!", $"COINS  {_coinsCollected} / {_coins.Count}     TIME  {_elapsed / 1000.0:0.0}s",
                "Press R to play again");
        }
        else if (_gameOver)
        {
            DrawOverlay("GAME OVER", $"COINS  {_coinsCollected} / {_coins.Count}", "Press R to try again");
        }

        SpriteBatch.End();
    }

    private void DrawOverlay(string title, string subtitle, string hint)
    {
        SpriteBatch.Draw(
            _pixelTexture,
            new RectangleF(0, 0, DesignWidth, DesignHeight),
            new Color4<Rgba>(0.02f, 0.03f, 0.06f, 0.62f),
            0f,
            Vector2.Zero);

        DrawCenteredText(title, DesignWidth, DesignHeight * 0.34f, 68, Color4.White);
        DrawCenteredText(subtitle, DesignWidth, DesignHeight * 0.34f + 88, 30, new Color4<Rgba>(0.9f, 0.94f, 1f, 1f));
        DrawCenteredText(hint, DesignWidth, DesignHeight * 0.34f + 142, 26, new Color4<Rgba>(0.74f, 0.82f, 0.92f, 1f));
    }

    private void DrawCenteredText(string text, float width, float y, float lineHeight, Color4<Rgba> color)
    {
        TextBounds bounds = Font.MeasureText(_font, text, lineHeight);
        SpriteBatch.DrawText(_font, text, new Vector2((width - bounds.Width) * 0.5f, y), lineHeight, color);
    }
}
