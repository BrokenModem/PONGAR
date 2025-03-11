using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = System.Drawing.Rectangle;

namespace PONGAR;

public class Ball(ARHandler arHandler, UserInterface userInterface)
{
    private Texture2D texture;
    private float speed;
    private float speedConst;
    private Vector2 velocity;
    private Vector2 position;
    public Rectangle collisionBox;
    private string previousCollisionTag;
    private string lastPlayerCollisionTag;
    private float gameTimer;
    private readonly ARHandler arHandler = arHandler;
    private readonly UserInterface gameInterface = userInterface;

    public void Initialize()
    {
        ResetBall();
        arHandler.GameBall = this;
    }

    public void LoadContent(ContentManager contentManager)
    {
        texture = contentManager.Load<Texture2D>("Ball");
        collisionBox = new Rectangle((int)(position.X + arHandler.GameCenterPosition.X), (int)(position.Y + arHandler.GameCenterPosition.Y), texture.Width, texture.Height);
    }   

    public void Update(GameTime gameTime)
    {
        gameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        speed = speedConst * (gameTimer / 10 + 1);
        Move(gameTime);
        CheckCollision();
        CheckForGoal();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, position + arHandler.GameCenterPosition, null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }
    
    //--------------------------------------------------------------------------------------------

    private void Move(GameTime gameTime)
    {
        velocity.Normalize();
        position += velocity * speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        collisionBox.X = (int)(position.X + arHandler.GameCenterPosition.X);
        collisionBox.Y = (int)(position.Y + arHandler.GameCenterPosition.Y);
    }

    private void CheckCollision()
    {
        Collider[] colliders = arHandler.GetArrayOfCollisionBoxes();

        if (colliders == null)
            return;

        foreach (var collider in colliders)
        {
            if (collisionBox.IntersectsWith(collider.Collisionbox) && collider.Tag != previousCollisionTag)
            {
                Console.WriteLine(collider.Tag + " prev tag:" + previousCollisionTag);
                previousCollisionTag = collider.Tag;
                OnCollision(collider);
            }
        }
    }

    private void OnCollision(Collider collider)
    {
        if (collider.Tag.Contains("Player"))
        {
            lastPlayerCollisionTag = collider.Tag;
            velocity.Y = -velocity.Y;
            speedConst += 2.45f;
        }
        else if (collider.Tag.Contains("Wall"))
        {
            velocity.X = -velocity.X;
        }
    }
    private void CheckForGoal()
    {
        if (position.Y + arHandler.GameCenterPosition.Y > arHandler.GameCenterPosition.Y + 250 
        || position.X + arHandler.GameCenterPosition.X > arHandler.GameCenterPosition.X + 250)
        {
            if(!string.IsNullOrEmpty(lastPlayerCollisionTag))
                gameInterface.UpdateScoreboard(lastPlayerCollisionTag);

            ResetBall();
        }
        else if (position.Y + arHandler.GameCenterPosition.Y  < arHandler.GameCenterPosition.Y - 250 
        || position.X + arHandler.GameCenterPosition.X < arHandler.GameCenterPosition.X - 250)
        {
            if(!string.IsNullOrEmpty(lastPlayerCollisionTag))
                gameInterface.UpdateScoreboard(lastPlayerCollisionTag);
                
            ResetBall();
        }
    }
    private void ResetBall()
    {
        Random rand = new();
        float randomX = (float)(rand.NextDouble() * 2 - 1);
        float randomY = (float)(rand.NextDouble() * 2 - 1);
        speedConst = 20f;
        speed = 0f;
        velocity = new Vector2(randomX, randomY);
        position = Vector2.Zero;
        previousCollisionTag = string.Empty;
        lastPlayerCollisionTag = string.Empty;
    }
}