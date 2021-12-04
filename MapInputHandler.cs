using Godot;
using System;

public class MapInputHandler : Sprite
{
    Vector2 clickStart = Vector2.Zero;
    Vector2 spriteStart;

    public bool processInput = true;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (processInput)
        {
            //handle Click to drage
            if (Input.IsActionJustPressed("map_click"))
            {
                clickStart = new Vector2(GetGlobalMousePosition().x, GetGlobalMousePosition().y);
                spriteStart = this.Position;
            }
            if (Input.IsActionPressed("map_click"))
            {
                Vector2 moveVector = clickStart - GetGlobalMousePosition();
                this.Position = spriteStart - moveVector;
            }
            if (Input.IsActionJustReleased("map_click"))
            {
                clickStart = Vector2.Zero;
                spriteStart = Vector2.Zero;
            }

            //Handle Zooming
            if (Input.IsActionPressed("zoom_out"))
            {
                Scale -= new Vector2(0.3f, 0.3f) * delta;
            }
            else if (Input.IsActionPressed("zoom_in"))
            {
                Scale += new Vector2(0.3f, 0.3f) * delta;
            }

            //Clip Scaling
            if (Scale.x < 0.3f) Scale = new Vector2(0.3f, 0.3f);
            if (Scale.x > 3f) Scale = new Vector2(3f, 3f);

            Vector2 tL = (Position - new Vector2(Texture.GetWidth() / 2 * Scale.x, Texture.GetHeight() / 2 * Scale.y));
            Vector2 bR = (Position + new Vector2(Texture.GetWidth() / 2 * Scale.x, Texture.GetHeight() / 2 * Scale.y));

            //Clip to Viewport
            if (bR.x < 15f)
                Position = new Vector2(-Texture.GetWidth() / 2 * Scale.x + 15f, Position.y);
            if (bR.y < 15f)
                Position = new Vector2(Position.x, -Texture.GetHeight() / 2 * Scale.y + 15f);

            if (tL.x > GetViewportRect().Size.x - 15f)
                Position = new Vector2(
                    GetViewportRect().Size.x + Texture.GetWidth() / 2 * Scale.x - 15f,
                    Position.y);
            if (tL.y > GetViewportRect().Size.y - 15f)
                Position = new Vector2(
                    Position.x,
                    GetViewportRect().Size.y + Texture.GetHeight() / 2 * Scale.y - 15f);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (processInput)
        {
            if (@event is InputEventMouseButton)
            {
                InputEventMouseButton emb = (InputEventMouseButton)@event;
                if (emb.IsPressed())
                {
                    if (emb.ButtonIndex == (int)ButtonList.WheelUp)
                    {
                        Scale += new Vector2(0.1f, 0.1f);
                    }
                    if (emb.ButtonIndex == (int)ButtonList.WheelDown)
                    {
                        Scale -= new Vector2(0.1f, 0.1f);
                    }
                }
            }
        }
    }
}
