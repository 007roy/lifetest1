using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class NeighborSystem : SystemBase
{
    private EntityQuery _entityBufferQuery;
    private EntityQuery _entityLifeQuery;
    private int _generation;
    protected override void OnCreate()
    {
        _generation = 0;
        
        var q = new EntityQueryDesc() {
            All = new[] { ComponentType.ReadWrite<EntityBufferElement>() }
        };
        _entityBufferQuery = GetEntityQuery(q);
        q = new EntityQueryDesc
        {
            All = new[] {ComponentType.ReadWrite<LifeComponent>()}
        };
        _entityLifeQuery = GetEntityQuery(q);
    }

    protected override void OnStartRunning()
    {
        //Assign neighbors to each entities' EntityBufferElement
        var neighborJob = new NeighborDataJob
        {
            BufferTypeHandle = GetBufferTypeHandle<EntityBufferElement>(),
            GridLocationTypeHandle = GetComponentTypeHandle<GridLocationComponent>(),
            BoardSize = BuildGridSystem.BoardSize,
            Neighbors = BuildGridSystem.NeighborEntities
        };
        Dependency = neighborJob.ScheduleParallel(_entityBufferQuery, 64, Dependency);
        Dependency.Complete();
    }

    protected override void OnUpdate()
    {
        var neighborJob = new NeighborCountJob()
        {
            BufferTypeHandle = GetBufferTypeHandle<EntityBufferElement>(),
            LifeDataFromEntity = GetComponentDataFromEntity<LifeComponent>(),
            CountComponentTypeHandle = GetComponentTypeHandle<NeighborCountComponent>()
        };
        var rulesJob = new RulesJob()
        {
            LifeTypeHandle = GetComponentTypeHandle<LifeComponent>(),
            CountComponentTypeHandle = GetComponentTypeHandle<NeighborCountComponent>(true)
        };
        var translationJob = new TranslationJob
        {
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(),
            LifeTypeHandle = GetComponentTypeHandle<LifeComponent>(),
            LocationTypeHandle = GetComponentTypeHandle<GridLocationComponent>()
        };
        
        var neighborJobHandle = neighborJob.ScheduleParallel(_entityBufferQuery, 64, Dependency);
        var rulesJobHandle = rulesJob.ScheduleParallel(_entityLifeQuery, 64, neighborJobHandle);
        Dependency = translationJob.ScheduleParallel(_entityLifeQuery, 64, rulesJobHandle);
        Dependency.Complete();
        _generation++;
        GenerationDisplay.singleton.onCounterUpdate.Invoke(_generation);
    }

    private struct NeighborDataJob : IJobEntityBatch
    {
        public BufferTypeHandle<EntityBufferElement> BufferTypeHandle;
        [ReadOnly]public ComponentTypeHandle<GridLocationComponent> GridLocationTypeHandle;
        [ReadOnly]public int2 BoardSize;
        [ReadOnly]public NativeArray<Entity> Neighbors;
        [BurstCompatible]
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var buffers = batchInChunk.GetBufferAccessor(BufferTypeHandle);
            var cells = batchInChunk.GetNativeArray(GridLocationTypeHandle);
            for (var index = 0; index < batchInChunk.Count; index++)
            {
                var buffer = buffers[index];
                for(var j = -1; j<=1 ; j++)
                for(var i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0) continue;
                    var x = cells[index].location.x + i;
                    var y = cells[index].location.y + j;
                    if (x < 0 || y < 0) continue;
                    if (x >= BoardSize.x || y >= BoardSize.y) continue;
                    buffer.Add(Neighbors[y*BoardSize.x + x]);
                }
            }
        }
    }

    private struct NeighborCountJob : IJobEntityBatch
    {
        [ReadOnly] public BufferTypeHandle<EntityBufferElement> BufferTypeHandle;
        [ReadOnly] public ComponentDataFromEntity<LifeComponent> LifeDataFromEntity;
        public ComponentTypeHandle<NeighborCountComponent> CountComponentTypeHandle;
        [BurstCompatible]
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var buffers = batchInChunk.GetBufferAccessor(BufferTypeHandle);
            var countComponents = batchInChunk.GetNativeArray(CountComponentTypeHandle);
            
            for (var index = 0; index < batchInChunk.Count; index++)
            {
                var count = 0;
                foreach (var neighbor in buffers[index])
                {
                    if (LifeDataFromEntity[neighbor].alive) count++;
                }
                countComponents[index] = new NeighborCountComponent {count = count};
            }

        }
    }

    private struct RulesJob : IJobEntityBatch
    {
        public ComponentTypeHandle<LifeComponent> LifeTypeHandle;
        [ReadOnly]public ComponentTypeHandle<NeighborCountComponent> CountComponentTypeHandle;
        [BurstCompatible]
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var countComponents = batchInChunk.GetNativeArray(CountComponentTypeHandle);
            var lifeComponents = batchInChunk.GetNativeArray(LifeTypeHandle);
            for (var index = 0; index < batchInChunk.Count; index++)
            {
                var alive = lifeComponents[index].alive;
                if (countComponents[index].count < 2) alive = false;
                else if (countComponents[index].count > 3) alive = false;
                else if (countComponents[index].count == 3) alive = true;
                lifeComponents[index] = new LifeComponent
                {
                    alive = alive
                };
            }
        }
    }

    private struct TranslationJob : IJobEntityBatch
    {
        //Move dead cells out of render range
        //TODO try pooling to bulk enable disable?
        public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<LifeComponent> LifeTypeHandle;
        [ReadOnly] public ComponentTypeHandle<GridLocationComponent> LocationTypeHandle;
        [BurstCompatible]
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var translations = batchInChunk.GetNativeArray(TranslationTypeHandle);
            var lifes = batchInChunk.GetNativeArray(LifeTypeHandle);
            var gridLocations = batchInChunk.GetNativeArray(LocationTypeHandle);
            for (var index = 0; index < batchInChunk.Count; index++)
            {
                translations[index] = new Translation()
                {
                    Value = lifes[index].alive
                        ? new float3(gridLocations[index].location.x * 1.5f, gridLocations[index].location.y * 1.5f, 0f)
                        : new float3(-10000, -10000, -10000)
                };
                
            }
        }
    }
}
