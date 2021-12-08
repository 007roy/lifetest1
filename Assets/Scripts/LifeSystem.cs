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
using TMPro;

[DisableAutoCreation]
public class LifeSystem : SystemBase
{

    public int generation;
    private EntityQuery cellComponentQuery;
    private EntityQuery entityBufferQuery;

    protected override void OnCreate()
    {
        generation = 0;
;
        var q = new EntityQueryDesc()
        {
            All = new[] { ComponentType.ReadWrite<CellComponent>() }
        };
    
    }

    protected override void OnStartRunning()
    {
    }


    protected override void OnUpdate()
    {
        /*
        var bsx = MasterSystem.BoardSizeX;
        var bsy = MasterSystem.BoardSizeY;
        EntityManager entityManager = EntityManager;
        ComponentDataFromEntity<CellComponent> allCellComps = GetComponentDataFromEntity<CellComponent>(true);
        var b = MasterSystem.neighboors;
        Entities.WithReadOnly(allCellComps).WithReadOnly(b).ForEach((ref CellComponent cell) =>
        {
            int count = 0;

            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0) continue;
                    var x = cell.x + i;
                    var y = cell.y + j;

                    if (x < 0 || y < 0) continue;
                    if (x >= bsx || y >= bsy) continue;
                    
                    var index = y * bsx + x;
                    var neighboor = b[index];
                    if (allCellComps[neighboor].alive) { count++; }
                }
            cell.count = count;

        }).Run();
*/
        
        //Count the neighbors
        NeighborCountJob neighborCountJob = new NeighborCountJob()
        {
            CellComponentTypeHandle = GetComponentTypeHandle<CellComponent>(),
            BufferTypeHandle = GetBufferTypeHandle<EntityBufferElement>()
        };
        Dependency = neighborCountJob.ScheduleParallel(entityBufferQuery, 1, Dependency);
        


        if (generation < 2) return; //dont apply rules till after first 2 rounds TODO this is wonky figure out why

        var applyRulesJob = new ApplyRulesJob()
        {
            cellComponentHandle = this.GetComponentTypeHandle<CellComponent>(false)
        };
        applyRulesJob.ScheduleParallel(cellComponentQuery, 32, this.Dependency);
        CompleteDependency();
        generation++;
        GenerationDisplay.singleton.onCounterUpdate.Invoke(generation);
        
    }


}

//Job to check neighboor count and apply life rules
public struct ApplyRulesJob : IJobEntityBatch
{
    public ComponentTypeHandle<CellComponent> cellComponentHandle;
    [BurstCompatible]
    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    {
        NativeArray<CellComponent> cellComponents = batchInChunk.GetNativeArray(cellComponentHandle);
        for (int index = 0; index < cellComponents.Length; index++)
        {
            bool alive = cellComponents[index].alive;
            if (cellComponents[index].count < 2) alive = false;
            else if (cellComponents[index].count > 3) alive = false;
            else if (cellComponents[index].count == 3) alive = true;
            cellComponents[index] = new CellComponent()
            {
                x = cellComponents[index].x,
                y = cellComponents[index].y,
                alive = alive,
                count = 0
            };
        }
    }
}

public struct NeighborCountJob : IJobEntityBatch
{
    public ComponentTypeHandle<CellComponent> CellComponentTypeHandle;
    [ReadOnly]public BufferTypeHandle<EntityBufferElement> BufferTypeHandle;
    [BurstCompatible]
    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    {
        var buffers = batchInChunk.GetBufferAccessor(BufferTypeHandle);
        var cellComponents = batchInChunk.GetNativeArray(CellComponentTypeHandle);
        for (int index = 0; index < batchInChunk.Count; index++)
        {
            var count = 0;
            var buffer = buffers[index];
            foreach (var n in buffer)
            {
                //var neighboorAlive = 
               // if (neighboorAlive) count++;
            }

            cellComponents[index] = new CellComponent()
            {
                x = cellComponents[index].x,
                y = cellComponents[index].y,
                alive = cellComponents[index].alive,
                count = count
            };
        }
        
    }
}




