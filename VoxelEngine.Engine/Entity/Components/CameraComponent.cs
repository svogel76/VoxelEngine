using System.Numerics;
using Silk.NET.Maths;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Rendering;

namespace VoxelEngine.Entity.Components;

/// <summary>
/// Hält die Kamera synchron mit der Entity-Position.
/// Update() schreibt Camera.Position = entityPosition + EyeOffset.
/// </summary>
public sealed class CameraComponent : IComponent
{
    private readonly Camera _camera;

    public string ComponentId => "camera";

    public CameraComponent(Camera camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
    }

    public void Update(IEntity iEntity, IModContext context, double deltaTime)
    {
        if (iEntity is not Entity entity) return;

        var phys   = entity.GetComponent<PhysicsComponent>();
        var eyePos = phys is not null
            ? phys.GetEyePosition(entity.InternalPosition)
            : entity.InternalPosition;

        _camera.Position = new Vector3D<float>(eyePos.X, eyePos.Y, eyePos.Z);
    }
}
