using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using Unity.Physics;

[InternalBufferCapacity(8)]
public struct EntityRef : IBufferElementData
{
    public Entity entity;
}
public class MasterSystem : SystemBase
{
    static public int BoardSizeX = 1000;
    static public int BoardSizeY = 1000;
    private EntityManager entityManager;
    //private CellComponent[,] Cells = new CellComponent[BoardSizeX,BoardSizeY];
    static public NativeArray<Entity> neighboors;

    protected override void OnCreate()
    {
        neighboors = new NativeArray<Entity>(BoardSizeX * BoardSizeY, Allocator.Persistent);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(Translation),
            //typeof(Rotation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(CellComponent),
            typeof(PhysicsCollider));
        Mesh cellMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        UnityEngine.Material cellMaterial = Resources.Load<UnityEngine.Material>("SomeColor");

        for (int y = 0; y < BoardSizeY; y++)
            for (int x = 0; x < BoardSizeX; x++)
            {
                Entity newCell = entityManager.CreateEntity(archetype);
                /// entityManager.AddComponentData(newCell, new Rotation { Value =  quaternion.EulerXYZ(new float3(0f,0f,0f))});
                entityManager.AddSharedComponentData(newCell, new RenderMesh
                {
                    mesh = cellMesh,
                    material = cellMaterial
                });
                CellComponent cellComponent = new CellComponent
                {
                    x = x,
                    y = y,
                    Alive = false,
                    count = 0
                };
                SetNeighboors(new int2(x, y), newCell);
                entityManager.AddComponentData(newCell, cellComponent);

                BlobAssetReference<Unity.Physics.Collider> boxCollider = Unity.Physics.BoxCollider.Create(new BoxGeometry
                {
                    Center = cellMesh.bounds.center,
                    BevelRadius = 0f,
                    Orientation = quaternion.identity,
                    Size = cellMesh.bounds.extents * 2.0f
                });

                entityManager.AddComponentData(newCell, new PhysicsCollider { Value = boxCollider });
                /*
                for (int j = -1; j <= 1; j++)
                    for (int i = -1; i <= 1; i++)
                    {
                        if (i == 0 && j == 0) continue;
                        if (x == 2 && y == 8)
                        {
                            var nx = x + i;
                            var ny = y + j;
                            Debug.Log("2,8 => (" + nx +","+ny+") => "  + ny * BoardSizeX + nx);
                        }
                    }
                */
            }


    }

    protected override void OnUpdate()
    {
        //Check all the cells for dead and move out of render range
        //TODO try pooling to bulk enable disable?
        Entities.ForEach((ref Translation translation, in CellComponent cell) => {
            if (cell.Alive)
            {
                translation.Value = new float3(cell.x * 1.5f, cell.y * 1.5f, 0f);
                return;
            }
            translation.Value = new float3(-10000, -10000, -10000);
        }).Schedule();
    }
    private void SetNeighboors(int2 gridLoc,Entity entity)
    {
        var index = gridLoc.y * BoardSizeX + gridLoc.x;
        neighboors[index] = entity;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        neighboors.Dispose();
    }
}
