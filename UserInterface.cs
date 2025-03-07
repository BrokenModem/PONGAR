using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PONGAR;

public class UserInterface
{
    private readonly Texture2D scoreboard;
    private readonly SpriteFont font;
    private int winCondition = 3;
    private float elapsedTime = 0;
    private int playerOneScore = 0;
    private int playerTwoScore = 0;

    public void Initialize()
    {

    }

    public void LoadContent(ContentManager content)
    {
    }

    public void Update(GameTime gameTime)
    {
        elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw scoreboard, timer and font.
    }
    public void UpdateScoreboard(string tag)
    {
        if (tag == "Player1")
            playerOneScore++;
        else if( tag == "Player2")
            playerTwoScore++;
    }
}