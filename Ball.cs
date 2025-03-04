using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = System.Drawing.Rectangle;

namespace PONGAR;

public class Ball
{
    private Texture2D texture;
    private float speed;
    private Vector2 velocity;
    private Vector2 position;
    private Rectangle collisionBox;

    private float gameTimer;
    private ARHandler arHandler;

    public Ball(ARHandler arHandler)
    {
        this.arHandler = arHandler;
    }

    public void Initialize()
    {
        speed = 5f;
        velocity = new Vector2(0f, 1f);
        position = new Vector2(arHandler.VideoCapture.Width / 2, arHandler.VideoCapture.Height / 2);
    }

    public void LoadContent(ContentManager contentManager)
    {
        texture = contentManager.Load<Texture2D>("Ball");
        collisionBox = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
    }

    public void Update(GameTime gameTime)
    {
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
        
        collisionBox.X = (int)position.X;
        collisionBox.Y = (int)position.Y;
    }

    private void CheckCollision()
    {
        Rectangle[] colliders = arHandler.GetArrayOfCollisionBoxes();

        foreach (var collider in colliders)
        {
            if (collisionBox.IntersectsWith(collider))
            {
                OnCollision();
            }
        }
    }

    private void OnCollision()
    {
        velocity.Y = -velocity.Y;
    }
}