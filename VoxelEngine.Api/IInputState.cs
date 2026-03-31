using VoxelEngine.Api.Input;

namespace VoxelEngine.Api;

public interface IInputState
{
    bool IsKeyPressed(Key key);
    (float deltaX, float deltaY) GetMouseDelta();
    int ConsumeMouseClicks(MouseButton button);
    int ConsumeLeftClicks();
    int ConsumeRightClicks();
    int ConsumeScrollSteps();
    void ClearTransientMouseState();
}
