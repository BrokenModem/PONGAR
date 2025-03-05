using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PONGAR;

public class UserInterface
{
    private readonly Texture2D scoreboard;
    private readonly SpriteFont font;
    private int winCondition = 3;
    private float elapsedTime;

    public UserInterface()
    {

    }
    public void Initialize()
    {
    }

    public void LoadContent(ContentManager content)
    {
        //LOAD scoreboard and font.
    }

    public void Update(GameTime gameTime)
    {
        //Update timer.
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw scoreboard, timer and font.
    }
    public void UpdateScoreboard()
    {
        //Update the score.
    }
}