namespace VoxelEngine.Core.Debug;

public interface ICommand
{
    string Name        { get; }
    string Description { get; }
    string Usage       { get; }
    void Execute(string[] args, GameContext context);
}
