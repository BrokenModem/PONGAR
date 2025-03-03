using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PONGAR;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private VideoCapture vc;
    private Mat frame;
    private bool frameRead;
    private Task cameraTask;
    private bool isRunning = true;
    private Texture2D currentFrameTexture;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Exiting += OnGameExit;
    }

    protected override void Initialize()
    {
        vc = new VideoCapture(0);
        frame = new Mat();
        
        // Start camera capture in a background thread
        cameraTask = Task.Run(() =>
        {
            while (isRunning)
            {
                frameRead = vc.Read(frame);
            }
        });

        // Set Window Size to Camera Size.
        _graphics.PreferredBackBufferWidth = vc.Width;
        _graphics.PreferredBackBufferHeight = vc.Height;
        _graphics.ApplyChanges();

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

        if (frameRead)
            currentFrameTexture = ConvertFrameToTexture(GraphicsDevice, frame);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _spriteBatch.Begin();
        
        if (frameRead)
            _spriteBatch.Draw(currentFrameTexture, Vector2.Zero, Color.White);   
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void OnGameExit(object sender, ExitingEventArgs e)
    {
        isRunning = false;
        cameraTask?.Wait();
        vc.Dispose();
    }

    private Texture2D ConvertFrameToTexture(GraphicsDevice graphicsDevice, Mat frame)
    {
        Image<Rgba, byte> imageFrame = frame.ToImage<Rgba, byte>();
        byte[] imageData = imageFrame.Bytes;
        Texture2D imageTexture = new Texture2D(graphicsDevice, imageFrame.Width, imageFrame.Height);
        imageTexture.SetData(imageData);
        
        return imageTexture;
    }
}