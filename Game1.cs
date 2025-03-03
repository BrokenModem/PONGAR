using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PONGAR;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D currentFrameTexture;
    private MarkerAR marker;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsFixedTimeStep = true;
        _graphics.SynchronizeWithVerticalRetrace = true;
        IsMouseVisible = true;
        Exiting += OnGameExit;
    }

    protected override void Initialize()
    {
        marker = new();

        // Set Window Size to Camera Size.
        _graphics.PreferredBackBufferWidth = marker.VideoCapture.Width;
        _graphics.PreferredBackBufferHeight = marker.VideoCapture.Height;
        _graphics.ApplyChanges();

        marker.StartTask();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape)) {Exit();}

        // TODO: Add your update logic here

        if (marker.FrameGrabbed && marker.GameFrame != null)
            currentFrameTexture = ConvertFrameToTexture(GraphicsDevice, marker.GameFrame);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _spriteBatch.Begin();
        
        if (marker.FrameGrabbed && marker.GameFrame != null)
            _spriteBatch.Draw(currentFrameTexture, Vector2.Zero, Color.White);   
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void OnGameExit(object sender, ExitingEventArgs e)
    {
        marker.IsRunning = true;
        marker.VideoCapture.Dispose();
    }

    private Texture2D ConvertFrameToTexture(GraphicsDevice graphicsDevice, Mat frame)
    {
        Image<Rgba, byte> imageFrame = frame.ToImage<Rgba, byte>();
        byte[] imageData = imageFrame.Bytes;
        Texture2D imageTexture = new(graphicsDevice, imageFrame.Width, imageFrame.Height);
        imageTexture.SetData(imageData);
        
        return imageTexture;
    }
}