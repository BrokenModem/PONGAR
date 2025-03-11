using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PONGAR;

public class UserInterface
{
    private SpriteFont font;
    private float elapsedTime = 0;
    private double timer = 0; 
    private int playerOneScore = 0;
    private int playerTwoScore = 0;

    public void LoadContent(ContentManager content)
    {
        font = content.Load<SpriteFont>("GameFont");
    }

    public void Update(GameTime gameTime)
    {
        elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        timer = (float)Math.Round(elapsedTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(font, playerOneScore + " : " + playerTwoScore, new Vector2(275, 25), Color.Yellow, 0f, Vector2.Zero, 3, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, timer.ToString(), new Vector2(300, 75), Color.Yellow, 0f, Vector2.Zero, 3, SpriteEffects.None, 0f);
    }
    public void UpdateScoreboard(string tag)
    {
        if (tag == "Player1")
            playerOneScore++;
        else if(tag == "Player2")
            playerTwoScore++;
    }
}