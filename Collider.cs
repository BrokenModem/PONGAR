using System.Drawing;

namespace PONGAR;

public class Collider
{
    public Rectangle Collisionbox { get; set; }
    public string Tag { get; set; }


    public Collider(Rectangle collisionbox, string tag)
    {
        Collisionbox = collisionbox;
        Tag = tag;
    }
}