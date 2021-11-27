using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct LifeComponent : IComponentData
{
    public bool alive;
}
