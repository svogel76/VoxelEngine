using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree;

public sealed class Selector : IBehaviourNode
{
    private readonly IReadOnlyList<IBehaviourNode> _children;
    private int _runningChildIndex = -1;

    public Selector(IReadOnlyList<IBehaviourNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        _children = children;
    }

    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
    {
        int startIndex = _runningChildIndex >= 0 ? _runningChildIndex : 0;

        for (int index = startIndex; index < _children.Count; index++)
        {
            NodeResult result = _children[index].Tick(entity, context, deltaTime);
            switch (result)
            {
                case NodeResult.Success:
                    _runningChildIndex = -1;
                    return NodeResult.Success;
                case NodeResult.Running:
                    _runningChildIndex = index;
                    return NodeResult.Running;
            }
        }

        _runningChildIndex = -1;
        return NodeResult.Failure;
    }
}

