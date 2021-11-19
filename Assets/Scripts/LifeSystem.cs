using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;
using Unity.Physics;
using UnityEngine;
using Unity.Physics.Systems;

public class LifeSystem : SystemBase
{
    public int generation;


    protected override void OnCreate()
    {
        generation = 0;
    }
    protected override void OnStartRunning()
    {
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random(1211);
        Entities.ForEach((ref CellComponent cell) =>
        {
            cell.Alive = rnd.NextInt(100) > 50;
        }).WithoutBurst().Run();
    }

    protected override void OnUpdate()
    {
        var physicWorldSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        var collisionWorldSystem = physicWorldSystem.PhysicsWorld.CollisionWorld;

        CollisionFilter filter = new CollisionFilter()
        {
            BelongsTo = ~0u,
            CollidesWith = ~0u,
            GroupIndex = 0
        };
        float3[] rays =
        {
                new float3(0, 1, 0) * 1.5f,
                new float3(0, -1, 0) * 1.5f,
                new float3(-1, 0, 0) * 1.5f,
                new float3(1, 0, 0) * 1.5f,
                new float3(1, 1, 0) * 1.5f,
                new float3(-1, 1, 0) * 1.5f,
                new float3(1, -1, 0) * 1.5f,
                new float3(-1, -1, 0) * 1.5f
        };
        var nativeRays = new NativeArray<float3>(length: 8, Allocator.TempJob);
        nativeRays.CopyFrom(rays);
        var roRays = nativeRays.AsReadOnly();
        Entities.ForEach((ref CellComponent cell) =>
        {
        int count = 0;
        
            for (int i = 0; i < 8; i++)
            {
                if (collisionWorldSystem.CastRay(new RaycastInput
                {
                    Start = new float3(cell.x * 1.5f, cell.y * 1.5f, 0f),
                    End = roRays[i],
                    Filter = filter
                })) count++;
            }
            //var hitEntity = Raycast(tran.position, to: Vector3.up * 5f);
            //var count = RaycastManager.NeighboorRaycast(collisionWorldSystem, new float3(cell.x * 1.5f, cell.y * 1.5f, 0f));
            cell.count = count;
        }).Schedule();
        generation++;
        Debug.Log("Generation: " + generation);
    }

    
    /*
    private Entity Raycast(Vector3 rayOrigin, Vector3 to)
    {
        var physicWorldSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        var collisionWorldSystem = physicWorldSystem.PhysicsWorld.CollisionWorld;

        var raycastInput = new RaycastInput
        {
            Start = rayOrigin,
            End = to,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            }
        };
        var raycastHit = new Unity.Physics.RaycastHit();
        RaycastManager.SingleRaycast(collisionWorldSystem, raycastInput, ref raycastHit);
        return raycastHit.Entity;
    }
    */
}
