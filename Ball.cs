using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = System.Drawing.Rectangle;

namespace PONGAR;

public class Ball(ARHandler arHandler)
{
    private Texture2D texture;
    private float speed;
    private float speedConst;
    private Vector2 velocity;
    private Vector2 position;
    private Rectangle collisionBox;
    private string previousCollisionTag;
    private float gameTimer;
    private readonly ARHandler arHandler = arHandler;

    public void Initialize()
    {
        speedConst = 15f;
        speed = 0f;
        velocity = new Vector2(0f, 1f);
        position = new Vector2(arHandler.VideoCapture.Width / 2, arHandler.VideoCapture.Height / 2);
    }

    public void LoadContent(ContentManager contentManager)
    {
        texture = contentManager.Load<Texture2D>("BALL");
        collisionBox = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
    }

    public void Update(GameTime gameTime)
    {
        gameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        speed = speedConst * (gameTimer / 10 + 1);
        Move(gameTime);
        CheckCollision();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, position, Color.White);
    }
    
    //--------------------------------------------------------------------------------------------

    private void Move(GameTime gameTime)
    {
        velocity.Normalize();
        position += velocity * speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        position += arHandler.GameCenterPosition;
        
        collisionBox.X = (int)position.X;
        collisionBox.Y = (int)position.Y;
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
                previousCollisionTag = collider.Tag;
                OnCollision(collider);
            }
        }
    }

    private void OnCollision(Collider collider)
    {
        if (collider.Tag.Contains("Player"))
        {
            velocity.Y = -velocity.Y;
        }
        else if (collider.Tag.Contains("Wall"))
        {
            velocity.X = -velocity.X;
        }
        speedConst += 2.45f;
    }
}