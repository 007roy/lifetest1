using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;


public class LifeSystem : SystemBase
{
    protected override void OnCreate()
    {
    }
    protected override void OnStartRunning()
    {
        Random rnd = new Random(1211);
        Entities.ForEach((ref CellComponent cell) =>
        {
            cell.Alive = rnd.NextInt(100) > 50;
        }).WithoutBurst().Run();
    }
    
    protected override void OnUpdate()
    {

        Entities.ForEach((ref CellComponent cell) =>
        {

        }).Schedule();
    }
}
