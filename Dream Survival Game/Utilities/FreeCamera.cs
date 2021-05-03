using Godot;
using System;

public class FreeCamera : Camera
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    float _sensitiviy = 0.25f;

    // Mouse state
    Vector2 _mouse_position = new Vector2(0.0f, 0.0f);
    float _total_pitch = 0.0f;

    // Movement state
    Vector3 _direction = new Vector3(0.0f, 0.0f, 0.0f);
    Vector3 _velocity = new Vector3(0.0f, 0.0f, 0.0f);
    float _acceleration = 30f;
    float _deceleration = -10f;
    float _vel_multiplier = 20f; // Movement speed multiplier

    // Keyboard state
    int _w = 0;
    int _s = 0;
    int _a = 0;
    int _d = 0;
    int _q = 0;
    int _e = 0;

    // World Editing State
    int _editingType = 0; // 0 for placing, 1 for removing
    int rayLength = 10000;

    public override void _Input(InputEvent @event)
    {
        // Receives mouse motion
        if (@event is InputEventMouseMotion)
        {
            _mouse_position = (@event as InputEventMouseMotion).Relative;
        }


        // Receives mouse button input
        if (@event is InputEventMouseButton)
        {
            InputEventMouseButton _mouseButtonEvent = @event as InputEventMouseButton;
            switch(_mouseButtonEvent.ButtonIndex)
            {
                case (int)ButtonList.Right:
                    Input.SetMouseMode(_mouseButtonEvent.Pressed ? Input.MouseMode.Captured : Input.MouseMode.Visible);
                    break;
            }
        }

        // Receives key input
        if (@event is InputEventKey)
        {
            InputEventKey _eventKeyEvent = @event as InputEventKey;
            switch(_eventKeyEvent.Scancode)
            {
                case (int)KeyList.W:
                    _w = _eventKeyEvent.Pressed ? 1 : 0;
                    break;
                case (int)KeyList.S:
                    _s = _eventKeyEvent.Pressed ? 1 : 0;
                    break;
                case (int)KeyList.A:
                    _a = _eventKeyEvent.Pressed ? 1 : 0;
                    break;
                case (int)KeyList.D:
                    _d = _eventKeyEvent.Pressed ? 1 : 0;
                    break;
                case (int)KeyList.Q:
                    _q = _eventKeyEvent.Pressed ? 1 : 0;
                    break;
                case (int)KeyList.E:
                    _e = _eventKeyEvent.Pressed ? 1 : 0;
                    break;
                case (int)KeyList.Key1:
                    _editingType = 0;
                    break;
                case (int)KeyList.Key2:
                    _editingType = 1;
                    break;
            }
        }
    }

    public override void _Process(float delta)
    {
        UpdateMouseLook();
        UpdateMovement(delta);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Input.IsActionJustPressed("player_action_edit"))
        {
            PhysicsDirectSpaceState spaceState = GetWorld().DirectSpaceState;

            Camera camera = this;
            Vector3 from = camera.ProjectRayOrigin(GetViewport().GetMousePosition());
            Vector3 to = from + camera.ProjectRayNormal(GetViewport().GetMousePosition()) * rayLength;

            Godot.Collections.Dictionary result = spaceState.IntersectRay(from, to, collideWithAreas: true);

            if (result.Count > 0)
            {
                Spatial chunkNode = ((Spatial)result["collider"]).GetParent<Spatial>();
                Vector3 chunkPos = chunkNode.Transform.origin;
                Vector3 hitPoint = (Vector3)result["position"];
                switch (_editingType)
                {
                    case 0:
                        World._instance.GetChunkFromVector3(chunkPos).PlaceTerrain(hitPoint);
                        break;
                    case 1:
                        World._instance.GetChunkFromVector3(chunkPos).RemoveTerrain(hitPoint);
                        break;
                }
            }
        }
    }

    ///<summary>
    /// Updates mouse look
    ///</summary>
    public void UpdateMouseLook()
    {
        // Only rotates mouse if the mouse is captured
        if (Input.GetMouseMode() == Input.MouseMode.Captured)
        {
            _mouse_position *= _sensitiviy;
            float yaw = _mouse_position.x;
            float pitch = _mouse_position.y;
            _mouse_position = new Vector2(0, 0);

            // Prevents looking up/down too far
            pitch = Mathf.Clamp(pitch, -90 - _total_pitch, 90 - _total_pitch);
            _total_pitch += pitch;

            RotateY(Mathf.Deg2Rad(-yaw));
            RotateObjectLocal(new Vector3(1, 0, 0), Mathf.Deg2Rad(-pitch));
        }
    }

    ///<summary>
    /// Updates camera movement
    ///</summary>
    public void UpdateMovement(float _delta)
    {
        // Calculates desired direction from key states
        float _dirX = _d - _a;
        float _dirY = _e - _q;
        float _dirZ = _s - _w;
        _direction = new Vector3(_dirX, _dirY, _dirZ);

        // Computes the change in velocity due to desired direction and "drag"
	    // The "drag" is a constant acceleration on the camera to bring it's velocity to 0
        Vector3 offset = _direction.Normalized() * _acceleration * _vel_multiplier * _delta 
                        + _velocity.Normalized() * _deceleration * _vel_multiplier * _delta;

        // Checks if we should bother translating the camera
        if (_direction == Vector3.Zero && offset.LengthSquared() > _velocity.LengthSquared())
        {
            // Sets the velocity to 0 to prevent jittering due to imperfect deceleration
            _velocity = Vector3.Zero;
        }
        else
        {
            // Clamps speed to stay within maximum value (_vel_multiplier)
            _velocity.x = Mathf.Clamp(_velocity.x + offset.x, -_vel_multiplier, _vel_multiplier);
            _velocity.y = Mathf.Clamp(_velocity.y + offset.y, -_vel_multiplier, _vel_multiplier);
            _velocity.z = Mathf.Clamp(_velocity.z + offset.z, -_vel_multiplier, _vel_multiplier);

            Translate(_velocity * _delta);
        }
    }
}
