using System.Text.Json;
using VoxelEngine.Api.Entity;
using VoxelEngine.Entity.BehaviourTree;

namespace VoxelEngine;

public static class BehaviourTreeLoader
{
    public static IBehaviourNode Load(JsonElement treeRoot, IBehaviourRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return ParseNode(treeRoot, registry);
    }

    private static IBehaviourNode ParseNode(JsonElement element, IBehaviourRegistry registry)
    {
        if (!element.TryGetProperty("type", out var typeProperty))
            throw new InvalidOperationException("Behaviour tree node is missing required property 'type'.");

        string type = typeProperty.GetString() ?? throw new InvalidOperationException("Behaviour tree node property 'type' must be a string.");

        return type switch
        {
            "selector" => new Selector(ParseChildren(element, registry)),
            "sequence" => new Sequence(ParseChildren(element, registry)),
            "condition" => registry.Create("condition", ReadName(element, type), element),
            "action" => registry.Create("action", ReadName(element, type), element),
            _ => throw new InvalidOperationException($"Unknown behaviour tree node type '{type}'.")
        };
    }

    private static IReadOnlyList<IBehaviourNode> ParseChildren(JsonElement element, IBehaviourRegistry registry)
    {
        if (!element.TryGetProperty("children", out var childrenElement) || childrenElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Composite behaviour node '{element.GetProperty("type").GetString()}' is missing array property 'children'.");

        var children = new List<IBehaviourNode>();
        foreach (var child in childrenElement.EnumerateArray())
            children.Add(ParseNode(child, registry));

        return children;
    }

    private static string ReadName(JsonElement element, string type)
    {
        if (!element.TryGetProperty("name", out var nameProperty))
            throw new InvalidOperationException($"Behaviour tree {type} node is missing required property 'name'.");

        return nameProperty.GetString() ?? throw new InvalidOperationException($"Behaviour tree {type} node property 'name' must be a string.");
    }
}
