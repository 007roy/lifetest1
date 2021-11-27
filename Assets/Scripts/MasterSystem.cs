using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

[DisableAutoCreation]
public class MasterSystem : SystemBase
{
    public static int BoardSizeX = 1000;
    public static int BoardSizeY = 1000;
    private EntityManager entityManager;
    public static NativeArray<CellComponent> neighbors;
    protected override void OnCreate()
    {
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random(2112);
        neighbors = new NativeArray<CellComponent>(BoardSizeX * BoardSizeY, Allocator.Persistent);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(CellComponent),
            typeof(EntityBufferElement));
        
        Mesh cellMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        UnityEngine.Material cellMaterial = Resources.Load<UnityEngine.Material>("SomeColor");
        
        for (int y = 0; y < BoardSizeY; y++)
            for (int x = 0; x < BoardSizeX; x++)
            { 
                
                Entity newCell = entityManager.CreateEntity(archetype);
                entityManager.AddBuffer<EntityBufferElement>(newCell);
                entityManager.AddSharedComponentData(newCell, new RenderMesh
                {
                    mesh = cellMesh,
                    material = cellMaterial
                });
                CellComponent cellComponent = new CellComponent
                {
                    x = x,
                    y = y,
                    alive = rnd.NextInt(100) > 50,
                    count = 0
                };
                entityManager.AddComponentData(newCell, cellComponent);
                SetNeighbors(new int2(x, y), cellComponent);
            }
    }

    protected override void OnUpdate()
    {
        //Check all the cells for dead and move out of render range
        //TODO try pooling to bulk enable disable?
        Entities.ForEach((ref Translation translation, in CellComponent cell) =>
        {
            translation.Value =
                cell.alive ? new float3(cell.x * 1.5f, cell.y * 1.5f, 0f) :
                    new float3(-10000, -10000, -10000);
        }).Schedule();
    }
    private void SetNeighbors(int2 gridLoc,CellComponent cellComponent)
    {
        var index = gridLoc.y * BoardSizeX + gridLoc.x;
        neighbors[index] = cellComponent;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        neighbors.Dispose();
    }
}
