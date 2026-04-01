using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoxelEngine.Api;

namespace VoxelEngine.Core;

public sealed class ModLoader
{
    private readonly Func<string, Assembly> _assemblyLoader;
    private readonly Func<Assembly, string, object?> _instanceFactory;

    public ModLoader(
        Func<string, Assembly>? assemblyLoader = null,
        Func<Assembly, string, object?>? instanceFactory = null)
    {
        _assemblyLoader = assemblyLoader ?? Assembly.LoadFrom;
        _instanceFactory = instanceFactory ?? ((assembly, typeName) => Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!));
    }

    public IReadOnlyList<IGameMod> LoadAll(string modsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modsDirectory);

        if (!Directory.Exists(modsDirectory))
            return Array.Empty<IGameMod>();

        var manifests = new Dictionary<string, ModManifest>(StringComparer.OrdinalIgnoreCase);

        foreach (string modDirectory in Directory.EnumerateDirectories(modsDirectory))
        {
            string manifestPath = Path.Combine(modDirectory, "mod.json");
            if (!File.Exists(manifestPath))
                continue;

            ModManifest manifest = ParseManifest(manifestPath, modDirectory);
            if (manifests.ContainsKey(manifest.Id))
                throw new InvalidOperationException($"Duplicate mod id '{manifest.Id}' found in '{modDirectory}'.");

            manifests.Add(manifest.Id, manifest);
        }

        IReadOnlyList<ModManifest> orderedManifests = ResolveLoadOrder(manifests);
        var loaded = new List<IGameMod>(orderedManifests.Count);

        foreach (ModManifest manifest in orderedManifests)
        {
            string assemblyPath = ResolveAssemblyPath(manifest);
            Assembly assembly = _assemblyLoader(assemblyPath);

            object? instance;
            try
            {
                instance = _instanceFactory(assembly, manifest.EntryClass);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to instantiate entry class '{manifest.EntryClass}' for mod '{manifest.Id}'.", ex);
            }

            if (instance is not IGameMod mod)
                throw new InvalidOperationException($"Entry class '{manifest.EntryClass}' for mod '{manifest.Id}' does not implement IGameMod.");

            if (!string.Equals(mod.Id, manifest.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Mod id mismatch: manifest declares '{manifest.Id}', but entry class reports '{mod.Id}'.");

            string assetBasePath = Path.GetFullPath(Path.Combine(manifest.DirectoryPath, "Assets"));
            if (mod is IModAssetAware assetAware)
                assetAware.SetAssetBasePath(assetBasePath);

            loaded.Add(new LoadedGameMod(mod, manifest.Id, assetBasePath));
        }

        return loaded;
    }

    private static ModManifest ParseManifest(string manifestPath, string modDirectory)
    {
        ModManifestDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ModManifestDocument>(File.ReadAllText(manifestPath), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid mod manifest JSON in '{manifestPath}'.", ex);
        }

        if (document is null)
            throw new InvalidOperationException($"Mod manifest '{manifestPath}' is empty.");

        string id = RequireText(document.Id, "id", manifestPath);
        string name = RequireText(document.Name, "name", manifestPath);
        string version = RequireText(document.Version, "version", manifestPath);
        string entryClass = RequireText(document.EntryClass, "entry_class", manifestPath);

        var dependencies = (document.Dependencies ?? Array.Empty<string>())
            .Select(dep => RequireText(dep, "dependencies[]", manifestPath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ModManifest(id, name, version, entryClass, dependencies, modDirectory);
    }

    private static IReadOnlyList<ModManifest> ResolveLoadOrder(IReadOnlyDictionary<string, ModManifest> manifests)
    {
        var indegree = manifests.Keys.ToDictionary(key => key, _ => 0, StringComparer.OrdinalIgnoreCase);
        var outgoing = manifests.Keys.ToDictionary(key => key, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (ModManifest manifest in manifests.Values)
        {
            foreach (string dependency in manifest.Dependencies)
            {
                if (!manifests.ContainsKey(dependency))
                    throw new InvalidOperationException($"Mod '{manifest.Id}' depends on missing mod '{dependency}'.");

                outgoing[dependency].Add(manifest.Id);
                indegree[manifest.Id]++;
            }
        }

        var ready = new SortedSet<string>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key), StringComparer.OrdinalIgnoreCase);
        var ordered = new List<ModManifest>(manifests.Count);

        while (ready.Count > 0)
        {
            string current = ready.Min!;
            ready.Remove(current);
            ordered.Add(manifests[current]);

            foreach (string dependent in outgoing[current])
            {
                indegree[dependent]--;
                if (indegree[dependent] == 0)
                    ready.Add(dependent);
            }
        }

        if (ordered.Count != manifests.Count)
        {
            string cycleIds = string.Join(", ", indegree.Where(pair => pair.Value > 0).Select(pair => pair.Key).OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException($"Circular mod dependency detected among: {cycleIds}.");
        }

        return ordered;
    }

    private static string ResolveAssemblyPath(ModManifest manifest)
    {
        if (!Directory.Exists(manifest.DirectoryPath))
            throw new DirectoryNotFoundException($"Mod directory '{manifest.DirectoryPath}' was not found.");

        string? candidate = Directory
            .EnumerateFiles(manifest.DirectoryPath, "*.dll", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), manifest.Id, StringComparison.OrdinalIgnoreCase));

        if (candidate is null)
            throw new FileNotFoundException($"No assembly '{manifest.Id}.dll' found for mod '{manifest.Id}' in '{manifest.DirectoryPath}'.");

        return Path.GetFullPath(candidate);
    }

    private static string RequireText(string? value, string fieldName, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Manifest '{Path.GetFileName(sourcePath)}' is missing required field '{fieldName}'.");

        return value;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record ModManifest(
        string Id,
        string Name,
        string Version,
        string EntryClass,
        IReadOnlyList<string> Dependencies,
        string DirectoryPath);

    private sealed class ModManifestDocument
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Version { get; init; }

        [JsonPropertyName("entry_class")]
        public string? EntryClass { get; init; }

        public string[]? Dependencies { get; init; }
    }

    private sealed class LoadedGameMod : IGameMod, IModAssetProvider
    {
        private readonly IGameMod _inner;

        public LoadedGameMod(IGameMod inner, string id, string assetBasePath)
        {
            _inner = inner;
            Id = id;
            AssetBasePath = assetBasePath;
        }

        public string Id { get; }
        public string AssetBasePath { get; }

        public void RegisterComponents(Api.Entity.IComponentRegistry registry) => _inner.RegisterComponents(registry);
        public void RegisterBehaviours(Api.Entity.IBehaviourRegistry registry) => _inner.RegisterBehaviours(registry);
        public void RegisterBlocks(Api.World.IBlockRegistry registry) => _inner.RegisterBlocks(registry);
        public void Initialize(IModContext context) => _inner.Initialize(context);
        public void Update(double deltaTime) => _inner.Update(deltaTime);
        public void Render(double deltaTime) => _inner.Render(deltaTime);
        public void Shutdown() => _inner.Shutdown();
    }

    internal interface IModAssetProvider
    {
        string AssetBasePath { get; }
    }
}

