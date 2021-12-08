using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BuildGridSystem : SystemBase
{
    public static int2 BoardSize = new int2(1000, 1000);
    private EntityManager _entityManager;
    public static NativeArray<Entity> NeighborEntities;

    protected override void OnCreate()
    {
        Unity.Mathematics.Random rnd = new Random(2100);
        NeighborEntities = new NativeArray<Entity>(BoardSize.x * BoardSize.y, Allocator.Persistent);
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype cellArchetype = _entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(EntityBufferElement),
            typeof(GridLocationComponent),
            typeof(LifeComponent),
            typeof(NeighborCountComponent));
        var cellMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var cellMaterial = Resources.Load<Material>("SomeColor");
        for(var y = 0; y < BoardSize.y; y++)
        for (var x = 0; x < BoardSize.x; x++)
        {
            Entity newCell = _entityManager.CreateEntity(cellArchetype);
            _entityManager.AddSharedComponentData(newCell, new RenderMesh
            {
                mesh = cellMesh,
                material = cellMaterial
            });
            _entityManager.AddBuffer<EntityBufferElement>(newCell);
            _entityManager.AddComponentData(newCell, new GridLocationComponent
            {
                location = new int2(x,y)
            });
            _entityManager.AddComponentData(newCell, new LifeComponent{alive = rnd.NextBool()});
            NeighborEntities[y * BoardSize.x + x] = newCell;

        }
    }

    protected override void OnUpdate()
    {

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        NeighborEntities.Dispose();
    }
}
