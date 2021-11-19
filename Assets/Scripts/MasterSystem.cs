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
    static public int BoardSizeX = 100;
    static public int BoardSizeY = 100;
    private EntityManager entityManager;
    private CellComponent[,] Cells = new CellComponent[BoardSizeX,BoardSizeY];
    protected override void OnCreate()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(CellComponent),
            typeof(PhysicsCollider));
        Mesh cellMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        UnityEngine.Material cellMaterial = Resources.Load<UnityEngine.Material>("BasicBlue");
        for (int x = 0; x < BoardSizeX; x++)
            for(int y=0; y < BoardSizeY; y++)
            {
                Entity newCell = entityManager.CreateEntity(archetype);
                //entityManager.AddComponentData(newCell, new Translation { Value = new float3(x*1.5f,y*1.5f,0f)});
                entityManager.AddComponentData(newCell, new Rotation { Value =
                    quaternion.EulerXYZ(new float3(0f,0f,0f))});
                entityManager.AddSharedComponentData(newCell, new RenderMesh
                {
                    mesh = cellMesh,
                    material = cellMaterial
                });
                CellComponent cellComponent = new CellComponent { x = x, y = y, Alive = false, toggleState = false};
                Cells[x, y] = cellComponent;
                entityManager.AddComponentData(newCell, cellComponent);
                entityManager.SetComponentData(newCell, new PhysicsCollider {});
                //entityManager.AddBuffer<EntityRef>(newCell);
            }
        /*
        Entities.ForEach((ref CellComponent cell) =>
        {
            
            using BlobBuilder bb = new BlobBuilder(Allocator.Temp);
            ref var neighboorCellAsset = ref bb.ConstructRoot<CellComponent>();
            var neighboorArrayBuilder = bb.Allocate(ref neighboorCellAsset.neighboors, 8);
            CellComponent[] nc = new CellComponent[8];
            int index = 0;
                var entity = Cells[cell.x, cell.y];
            for (int i = cell.x - 1; i <= cell.x + 1; i++)
                for (int j = cell.y - 1; j <= cell.y + 1; j++)
                {
                    if (i < 0 || j < 0) continue;
                    if (i >= MasterSystem.BoardSizeX || j >= MasterSystem.BoardSizeY) continue;
                    if (i == cell.x && j == cell.y) continue;
                    nc[index] = Cells[i, j];
                }
            bb.Construct<CellComponent>(ref cell.neighboors, nc);
        }).WithoutBurst().Run();
          */ 
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation, in CellComponent cell) => {
            if (cell.Alive)
            {
                translation.Value = new float3(cell.x * 1.5f, cell.y * 1.5f, 0f);
                return;
            }
            translation.Value = new float3(-10000, -10000, -10000);  
        }).Schedule();
    }
}
