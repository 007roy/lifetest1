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
    BuildPhysicsWorld buildPhysicsWorld;
    ExportPhysicsWorld exportPhysicsWorld;
    EndFramePhysicsSystem endFramePhysics;

    private EntityQuery cellComponentQuery;

protected override void OnCreate()
    {
        generation = 0;
        buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        
        exportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
        endFramePhysics = World.GetOrCreateSystem<EndFramePhysicsSystem>();
        var q = new EntityQueryDesc()
        {
            All = new ComponentType[] { ComponentType.ReadWrite<CellComponent>() }
        };
        cellComponentQuery = this.GetEntityQuery(q);
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
        ref PhysicsWorld pw = ref World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
        var updateRayCastJob = new RayCastJob()
        {
            cellComponentHandle = this.GetComponentTypeHandle<CellComponent>(false),
            world = pw
        };
        this.Dependency = JobHandle.CombineDependencies(this.Dependency, exportPhysicsWorld.GetOutputDependency());
        this.Dependency = updateRayCastJob.ScheduleParallel(cellComponentQuery, 32, this.Dependency);
        
        
        generation++;
        if (generation < 2) return;
        // Debug.Log("Generation: " + generation);

        var applyRulesJob = new ApplyRulesJob()
        {
            cellComponentHandle = this.GetComponentTypeHandle<CellComponent>(false)
        };
        applyRulesJob.ScheduleParallel(cellComponentQuery, 32, this.Dependency).Complete();
        //endFramePhysics.AddInputDependency(this.Dependency);
    }


}
public struct ApplyRulesJob : IJobEntityBatch
{
    public ComponentTypeHandle<CellComponent> cellComponentHandle;
    [BurstCompatible]
    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    {
        NativeArray<CellComponent> cellComponents = batchInChunk.GetNativeArray(cellComponentHandle);
        for (int index = 0; index < cellComponents.Length; index++)
        {
            bool alive = cellComponents[index].Alive;
            if (cellComponents[index].count < 2) alive = false;
            else if (cellComponents[index].count > 3) alive = false;
            else if (cellComponents[index].count == 3) alive = true;
            cellComponents[index] = new CellComponent()
            {
                x = cellComponents[index].x,
                y = cellComponents[index].y,
                Alive = alive,
                count = 0
            };
        }
    }
}
public struct RayCastJob : IJobEntityBatch
{
    public ComponentTypeHandle<CellComponent> cellComponentHandle;
    [ReadOnly] public PhysicsWorld world;
    [BurstCompile]
    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    {
        NativeArray<CellComponent> cellComponents = batchInChunk.GetNativeArray(cellComponentHandle);
        for (int index = 0; index < cellComponents.Length; index++)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    var start = new float3(cellComponents[index].x * 1.5f, cellComponents[index].y * 1.5f, 0f)+new float3(i,j, 0)*0.6f;
                    var end = start + new float3(i, j, 0) * 1f;
                    var hit = world.CastRay(new RaycastInput
                    {
                        Start = start,
                        End = end,
                        Filter = CollisionFilter.Default
                    },out Unity.Physics.RaycastHit closestHit);
                    if (hit) count++;
                }
            cellComponents[index] = new CellComponent()
            {
                x = cellComponents[index].x,
                y = cellComponents[index].y,
                Alive = cellComponents[index].Alive,
                count = count
            };
        }
    }
}