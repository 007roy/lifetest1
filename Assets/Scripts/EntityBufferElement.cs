using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(8)]
public struct EntityBufferElement : IBufferElementData
{
    private Entity _entity;

    public static implicit operator Entity(EntityBufferElement e) //fancy ass c# magic
    {
        return e._entity;
    }

    public static implicit operator EntityBufferElement(Entity e)
    {
        return new EntityBufferElement {_entity = e};
    }
}