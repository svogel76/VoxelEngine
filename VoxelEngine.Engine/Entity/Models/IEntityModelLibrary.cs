namespace VoxelEngine.Entity.Models;

public interface IEntityModelLibrary
{
    EntityAtlasDefinition Atlas { get; }
    IReadOnlyCollection<IVoxelModelDefinition> GetAllModels();
    IVoxelModelDefinition GetModel(string modelId);
}
