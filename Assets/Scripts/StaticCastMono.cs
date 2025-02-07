using Unity.Entities;
using UnityEngine;

public class StaticCastMono : MonoBehaviour
{
    public StaticCastData Data;
    class Baker : Baker<StaticCastMono>
    {
        public override void Bake (StaticCastMono mono)
        {
            var entity = GetEntity(mono, TransformUsageFlags.Dynamic);
            AddComponent(entity, mono.Data);
        }
    }
}
