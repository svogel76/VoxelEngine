using Silk.NET.Input;

namespace VoxelEngine.Core;

public interface IInputState
{
    bool IsKeyPressed(Key key);
    (float deltaX, float deltaY) GetMouseDelta();
    int ConsumeLeftClicks();
    int ConsumeRightClicks();
    int ConsumeScrollSteps();
    void ClearTransientMouseState();
}