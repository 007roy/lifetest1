using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;

[DisableAutoCreation]
public class RaycastManager : JobComponentSystem
{
    [BurstCompile]
    public struct RayCastJob : IJobParallelFor
    {
        [ReadOnly] public CollisionWorld collisionWorld;
        [ReadOnly] public NativeArray<RaycastInput> raycastInputs;

        public NativeArray<RaycastHit> results;

        public void Execute(int index)
        {
            collisionWorld.CastRay(raycastInputs[index], out var hit);
            results[index] = hit;
        }
    }

    public static JobHandle ScheduleRaycast(CollisionWorld collisionWorld, NativeArray<RaycastInput> raycastInputs,
        NativeArray<RaycastHit> raycastHits)
    {
        var raycastJob = new RayCastJob()
        {
            collisionWorld = collisionWorld,
            raycastInputs = raycastInputs,
            results = raycastHits
        };
        JobHandle jobHandle = raycastJob.Schedule(raycastInputs.Length, 8);
        return jobHandle;
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        throw new System.NotImplementedException();
    }
    /*
    public static void SingleRaycast(CollisionWorld collisionWorld, RaycastInput input, ref RaycastHit hit)
    {
        var raycastInput = new NativeArray<RaycastInput>(length: 1, Allocator.TempJob);
        var raycastResult = new NativeArray<RaycastHit>(length: 1, Allocator.TempJob);

        raycastInput[0] = input;

        var jobHandle = ScheduleRaycast(collisionWorld, raycastInput, raycastResult);
        jobHandle.Complete();
        hit = raycastResult[0];
        raycastInput.Dispose();
        raycastResult.Dispose();
    }
    */
    public static int NeighboorRaycast(CollisionWorld collisionWorld, float3 from)
    {
        var raycastInput = new NativeArray<RaycastInput>(length: 8, Allocator.TempJob);
        var raycastResult = new NativeArray<RaycastHit>(length: 8, Allocator.TempJob);
        var filter = new CollisionFilter()
        {
            BelongsTo = ~0u,
            CollidesWith = ~0u,
            GroupIndex = 0
        };
        
        raycastInput[0] = new RaycastInput
        {
            Start = from,
            End = UnityEngine.Vector3.up * 1.5f,
            Filter = filter
        };
        raycastInput[1] = new RaycastInput
        {
            Start = from,
            End = UnityEngine.Vector3.down * 1.5f,
            Filter = filter
        };
        raycastInput[2] = new RaycastInput
        {
            Start = from,
            End = UnityEngine.Vector3.left * 1.5f,
            Filter = filter
        };
        raycastInput[3] = new RaycastInput
        {
            Start = from,
            End = UnityEngine.Vector3.right * 1.5f,
            Filter = filter
        };
        raycastInput[4] = new RaycastInput
        {
            Start = from,
            End = new float3(-1,1,0) * 1.5f, // up/left
            Filter = filter
        };
        raycastInput[5] = new RaycastInput
        {
            Start = from,
            End = new float3(1, 1, 0) * 1.5f, //up/right
            Filter = filter
        };
        raycastInput[6] = new RaycastInput
        {
            Start = from,
            End = new float3(-1, -1, 0) * 1.5f, //down/left
            Filter = filter
        };
        raycastInput[7] = new RaycastInput
        {
            Start = from,
            End = new float3(1, -1, 0) * 1.5f, //down/right
            Filter = filter
        };

        var jobHandle = ScheduleRaycast(collisionWorld, raycastInput, raycastResult);
        jobHandle.Complete();
        int count = 0;
        foreach (var hit in raycastResult)
        {
            if(hit.Entity != Entity.Null)
                count++;
        }
        
        raycastInput.Dispose();
        raycastResult.Dispose();

        return count;
    }


}
