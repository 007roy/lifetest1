using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

public class MasterSystem : SystemBase
{
    static public int BoardSizeX = 100;
    static public int BoardSizeY = 100;
    private EntityManager entityManager;
    
    protected override void OnCreate()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(CellComponent));
        Mesh cellMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        Material cellMaterial = Resources.Load<Material>("BasicBlue");
        for (int x = 0; x < 100; x++)
        {
            for(int y=0; y < 100; y++)
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
                entityManager.AddComponentData(newCell, new CellComponent { x = x, y = y, Alive = false});
            }
        }
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
