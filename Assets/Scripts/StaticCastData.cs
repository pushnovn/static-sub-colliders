using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct StaticCastData : IComponentData
{
    public float3 Velocity;
    public float3 RayStartLocal;
    public float3 RayEndLocal;
}