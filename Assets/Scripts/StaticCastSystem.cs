using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct StaticCastSystem : ISystem
{
    private int _frame;
    private int _fixed;

    public void OnCreate (ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<StaticCastData>();
    }

    public void OnUpdate (ref SystemState state)
    {
        // Try to use PhysicsStep.SynchronizeCollisionWorld, but it doesn't help
        // PhysicsStepAuthoring located in sub-scene with SynchronizeCollisionWorld checked

        // Try to await ExportPhysicsWorld, but it doesn't help either
        state.Dependency.Complete();

        // FixedStep during frame Counter
        if (_frame < Time.frameCount)
        {
            _frame = Time.frameCount;
            _fixed = default;
        }
        _fixed++;

        // I add this after daniel-holz suggestion, doesn't help
        {
            // Retrieve the LocalToWorldSystem from the World
            var localToWorldSystem = state.World.GetOrCreateSystem<LocalToWorldSystem>();

            // Call Update on the system
            localToWorldSystem.Update(state.World.Unmanaged);

            // Wait LocalToWorldSystem to complete
            state.Dependency.Complete();
        }

        // I add this after daniel-holz suggestion, doesn't help
        {
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            var parentLookup = SystemAPI.GetComponentLookup<Parent>();
            var scaleLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>();

            foreach (var (localToWorld, entity) in SystemAPI.Query<RefRW<LocalToWorld>>().WithEntityAccess())
            {
                TransformHelpers.ComputeWorldTransformMatrix(
                    entity, out var worldMatrix,
                    ref localTransformLookup, ref parentLookup, ref scaleLookup);
                localToWorld.ValueRW.Value = worldMatrix;
            }
        }

        // Try to manually sync CollisionWorld, but it doesn't help either
        ref var physicsWorld = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW;
        physicsWorld.CollisionWorld.UpdateStaticTree(ref physicsWorld.PhysicsWorld);
        physicsWorld.CollisionWorld.UpdateDynamicTree(ref physicsWorld.PhysicsWorld, default, default);

        // And, to be honest, I would prefer to avoid unnecessary SynchronizeCollisionWorld and UpdateTree calls

        var hits = new NativeList<RaycastHit>(Allocator.TempJob);

        foreach (var (transform, staticCast) in SystemAPI.Query<LocalTransform, StaticCastData>())
        {
            hits.Clear();
            var worldMatrix = transform.ToMatrix();

            // Aiming ray to the center of our cube (according to inspector settings)
            // By default it set to send from forward (Z+1) to back (Z-1), but also works for X±1 and Y±1
            physicsWorld.CastRay(new RaycastInput
            {
                Start = worldMatrix.TransformPoint(staticCast.RayStartLocal),
                End = worldMatrix.TransformPoint(staticCast.RayEndLocal),
                Filter = CollisionFilter.Default,
            }, ref hits);

            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    var name = state.EntityManager.GetName(hit.Entity);
                    // Distance from cube (according to entity LocalTransform) to hit.Position,
                    // After the first shift with long ray hit.Position will remain in the old coords
                    var distance = math.distance(worldMatrix.Translation(), hit.Position);
                    Debug.Log($"FixedStep#{_fixed}: {name}, cube2hit dist = {distance:F3}m, hit pos = {hit.Position}");
                }
            }
            // After first shift during same frame we will start miss raycast, if it is short enough
            else
            {
                Debug.Log($"FixedStep#{_fixed}: NO HIT, ray start = {worldMatrix.TransformPoint(staticCast.RayStartLocal)}");
            }
        }

        // After raycast we move our test cubes according to their Velocity param
        foreach (var (transformRW, staticCast) in SystemAPI.Query<RefRW<LocalTransform>, StaticCastData>())
        {
            // By default, velocity is (0,0,180) to move cube 3 meters along Z-axis (60 physics fps)
            transformRW.ValueRW.Position += staticCast.Velocity * SystemAPI.Time.DeltaTime;
        }

        hits.Dispose();
    }
}