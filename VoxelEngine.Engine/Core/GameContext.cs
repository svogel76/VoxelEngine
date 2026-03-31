using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Core.Debug;
using VoxelEngine.Core.Hud;
using VoxelEngine.Core.UI;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Components;
using VoxelEngine.Entity.Models;
using VoxelEngine.Entity.Spawning;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;
using VoxelEngine.World.Inventories;
using System.Numerics;

namespace VoxelEngine.Core;

public class GameContext : IDisposable, IGameContext
{
    public EngineSettings      Settings      { get; }
    public IKeyBindings        KeyBindings   { get; }
    public World.World         World         { get; }
    public Entity.Entity       Player        { get; }
    public Camera              Camera        { get; }
    public Renderer            Renderer      { get; }
    public InputHandler        Input         { get; }
    public DebugConsole        Console       { get; }
    public WorldGenerator      Generator     { get; }
    public ChunkManager        ChunkManager  { get; }
    public WorldTime           Time          { get; }
    public EntityManager       EntityManager { get; }
    public SpawnManager        SpawnManager  { get; }
    public IEntityModelLibrary EntityModels  { get; }
    public Vector3             PlayerSpawnPoint { get; }
    public HudRegistry         HudRegistry   { get; } = new HudRegistry();
    public UIStateManager      UI            { get; } = new UIStateManager();
    public IWorldPersistence   Persistence   { get; }

    // Explicit interface implementations
    IBlockRegistry IGameContext.BlockRegistry => BlockRegistryAdapter.Instance;
    IWorldAccess   IGameContext.World         => World;
    IInputState    IGameContext.Input         => Input;
    IKeyBindings   IGameContext.KeyBindings   => KeyBindings;
    IEntity        IGameContext.Player        => Player;

    /// <summary>Standalone Spieler-Inventar (nicht an Entity-Klasse gebunden).</summary>
    public PlayerInventory Inventory { get; }

    public BlockRaycastHit?       TargetedBlock    { get; set; }
    public BlockPlacementPreview? PlacementPreview { get; set; }
    public bool ShutdownRequested { get; set; }

    private bool _disposed;

    public GameContext(
        EngineSettings      settings,
        IKeyBindings        keyBindings,
        World.World         world,
        Entity.Entity       player,
        Camera              camera,
        Renderer            renderer,
        InputHandler        inputHandler,
        WorldGenerator      generator,
        IEntityModelLibrary entityModels,
        IWorldPersistence   persistence,
        Vector3?            playerSpawnPoint = null)
    {
        Settings     = settings;
        KeyBindings  = keyBindings;
        World        = world;
        Player       = player;
        Camera       = camera;
        Renderer     = renderer;
        Input        = inputHandler;
        Generator    = generator;
        EntityModels = entityModels;
        Persistence  = persistence;
        PlayerSpawnPoint = playerSpawnPoint ?? player.InternalPosition;
        Console      = new DebugConsole(this);
        ChunkManager = new ChunkManager(world, generator, settings, persistence);
        Time         = new WorldTime { TimeScale = settings.TimeScale };
        Inventory    = new PlayerInventory(new global::VoxelEngine.World.Inventory());

        EntityManager = new EntityManager(
            world, settings, generator, Time, entityModels,
            () => Player.InternalPosition,
            () => global::VoxelEngine.Entity.ViewFrustum.FromViewProjection(ToNumerics(Camera.ViewMatrix * Camera.ProjectionMatrix)));

        SpawnManager = new SpawnManager(
            world, EntityManager, settings, generator, Time, entityModels,
            () => Player.InternalPosition,
            () => global::VoxelEngine.Entity.ViewFrustum.FromViewProjection(ToNumerics(Camera.ViewMatrix * Camera.ProjectionMatrix)));

        EntityManager.SpawnManager = SpawnManager;
        Time.SetTime(settings.InitialTime);

        // Hotbar mit Startzustand befüllen
        Inventory.SetSlot(SlotAddress.Hotbar(0), new ItemStack(BlockType.Grass, 10));
        Inventory.SetSlot(SlotAddress.Hotbar(1), new ItemStack(BlockType.Dirt,  10));
        Inventory.SetSlot(SlotAddress.Hotbar(2), new ItemStack(BlockType.Stone, 10));
        Inventory.SetSlot(SlotAddress.Hotbar(3), new ItemStack(BlockType.Sand,  10));
    }

    public bool TryDequeueResult(out ChunkResult result) =>
        ChunkManager.TryDequeueResult(out result);

    public async Task SaveGameStateAsync()
    {
        await WorldStatePersistence.SaveLoadedChunkEditsAsync(World, Persistence).ConfigureAwait(false);

        var hotbar = new ItemStackData?[global::VoxelEngine.World.Inventory.HotbarSize];
        for (int i = 0; i < global::VoxelEngine.World.Inventory.HotbarSize; i++)
        {
            var stack = Inventory.Hotbar.Hotbar[i];
            hotbar[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var inventoryGrid = new ItemStackData?[InventoryGrid.TotalSlots];
        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var stack = Inventory.Grid.Get(i);
            inventoryGrid[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var equipmentSlots = new ItemStackData?[EquipmentSlots.Count];
        for (int i = 0; i < EquipmentSlots.Count; i++)
        {
            var stack = Inventory.Equipment.Get((EquipmentSlotType)i);
            equipmentSlots[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var phys   = Player.GetComponent<PhysicsComponent>();
        var health = Player.GetComponent<HealthComponent>();

        var playerState = new PlayerState(
            Player.InternalPosition,
            Camera.Yaw,
            Camera.Pitch,
            phys?.FlyMode ?? false,
            Inventory.Hotbar.SelectedSlot,
            hotbar,
            inventoryGrid,
            equipmentSlots,
            health?.CurrentHp ?? 20f,
            20f);

        var worldMeta = new WorldMeta(Time.Time, Time.DayCount, Settings.Terrain.Seed, Time.TimeScale);

        await Persistence.SavePlayerStateAsync(playerState).ConfigureAwait(false);
        await Persistence.SaveWorldMetaAsync(worldMeta).ConfigureAwait(false);
    }

    public async Task<(PlayerState? Player, WorldMeta? World)> LoadGameStateAsync()
    {
        var playerState = await Persistence.LoadPlayerStateAsync().ConfigureAwait(false);
        var worldMeta   = await Persistence.LoadWorldMetaAsync().ConfigureAwait(false);
        return (playerState, worldMeta);
    }

    public void ApplyLoadedState(PlayerState playerState, WorldMeta worldMeta)
    {
        var phys   = Player.GetComponent<PhysicsComponent>();
        var health = Player.GetComponent<HealthComponent>();

        if (phys is not null)
        {
            phys.Teleport(Player, playerState.Position);
            phys.SetFlyMode(playerState.FlyMode);
        }
        else
        {
            Player.InternalPosition = playerState.Position;
        }

        Camera.SetRotation(playerState.Yaw, playerState.Pitch);
        Camera.Position = new Silk.NET.Maths.Vector3D<float>(
            Player.InternalPosition.X,
            Player.InternalPosition.Y + (phys?.EyeOffset ?? 0f),
            Player.InternalPosition.Z);

        Inventory.Hotbar.SelectSlot(playerState.SelectedSlot);

        for (int i = 0; i < global::VoxelEngine.World.Inventory.HotbarSize; i++)
        {
            var data = i < playerState.Hotbar.Count ? playerState.Hotbar[i] : null;
            Inventory.SetSlot(SlotAddress.Hotbar(i), data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var data = i < playerState.InventoryGrid.Count ? playerState.InventoryGrid[i] : null;
            Inventory.Grid.Set(i, data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

        for (int i = 0; i < EquipmentSlots.Count; i++)
        {
            var data = i < playerState.EquipmentSlots.Count ? playerState.EquipmentSlots[i] : null;
            Inventory.Equipment.Set((EquipmentSlotType)i, data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

        health?.RestoreHealth(playerState.Health);

        Time.Restore(worldMeta.Time, worldMeta.DayCount);
        Time.TimeScale = worldMeta.TimeScale;
    }

    public bool RespawnPlayerIfDead()
    {
        var health = Player.GetComponent<HealthComponent>();
        if (health is null || !health.IsDead)
            return false;

        Console.Log("You died.");
        health.RestoreHealth(health.MaxHp);

        var physics = Player.GetComponent<PhysicsComponent>();
        if (physics is not null)
        {
            physics.Teleport(Player, PlayerSpawnPoint);
            physics.SyncPhysics(Player);
        }
        else
        {
            Player.InternalPosition = PlayerSpawnPoint;
            Player.InternalVelocity = Vector3.Zero;
        }

        Camera.Position = new Silk.NET.Maths.Vector3D<float>(
            Player.InternalPosition.X,
            Player.InternalPosition.Y + (physics?.EyeOffset ?? 0f),
            Player.InternalPosition.Z);

        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ChunkManager.Dispose();
        Renderer.Dispose();
        Console.Dispose();
        GC.SuppressFinalize(this);
    }

    private static System.Numerics.Matrix4x4 ToNumerics(Silk.NET.Maths.Matrix4X4<float> matrix) =>
        new(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
}

