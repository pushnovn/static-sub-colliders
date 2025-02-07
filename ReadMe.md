If a static Entity (without a physics body) contains a non-root collider (in a child Entity) and is shifted several times per frame (within a FixedStep), then CollisionWorld does not update the position of this child collider inside itself between FixedSteps until the next frame. And if several FixedSteps occur per frame, then the correct position of the child collider will only be in the first FixedStep (and even then, if you shift this collider after starting a test raycast). Regardless of the number of FixedSteps, the position of the child collider will "freeze" at the start of the frame (this can be checked by casting a long ray that "reaches" the previous position of the collider). In this case, if the static collider is located at the root of the moving Entity or there is a CompountCollider, then the raycast will hit such a static collider every FixedStep even without any additional tricks (PhysicsStep.SynchronizeCollisionWorld, CollisionWorld.UpdateStaticTree, etc.).


What I tried to do:
- I tried to call state.Dependency.Complete() after ExportPhysicsWorld in order to wait for updated data from the physics simulation
- I tried to check PhysicsStep.SynchronizeCollisionWorld
- I tried to manually synchronize CollisionWorld before raycast via UpdateStaticTree/UpdateDynamicTree


How the test project works:
- The test cube with a collider (StaticCastData) is shifted every FixedStep by a fixed distance. Before the next cube shift (within FixedStep), we send a raycast to the center of the cube in the hope of hitting this cube.
- The problem starts when several PhysicsSystemGroups have time to process in the SimulationSystemGroup: if we shift the cube and on the next FixedStep (within the same frame) send a raycast to the cube again, we will miss, because from the point of view of CollisionWorld, the cube collider will remain in the same place before the shift.
- Even if you make several shifts, this collider will still be in the original location at the beginning of the frame (you can verify this by sending a very long raycast, which will also affect the "old" position of the cube collider, this will be visible by hit.Position).
- The whole problem only works for cubes whose collider is not in the root, but in the child Entity.
- Both the test raycast and the cube shift are performed in the StaticCastSystem. There is also a ThreadSleepSystem, but it simply "delays" Update so that FixedStep is executed several times per frame (in a real project, the situation is exactly the same even with delays due to natural causes).
- Added several test cubes to SubScene: NoneChild, WithChild, WithChildLongRay. NoneChild works correctly and as expected. WithChild stops being defined by the raycast after the first shift. WithChildLongRay is similar to WithChild, but with a longer raycast that reaches the old WithChildLongRay position (the position before the shift), this can be seen in hit.Position (although hit.Entity WorldTransform is already updated).


? Why not just use root CompoundCollider?
> Because in our project this static Entity is a complex object with a bunch of child entities and colliders (doors, colliders for interacting with the player, etc.), and CompoundCollider will simply "bake" all the colliders together into one single collider.

? Why not split this static object into many root and small ones with one collider per object and drag the whole group along where we need to go?
> Because our static object supports nesting, i.e. other similar objects can be nested in it, and this whole mass moves together as a single object. And even if we somehow manage to split everything into a huge train of simple entity-colliders, we get hundreds of different train carriage (of varying degrees of complexity and different tasks), each of which must be monitored individually. And the nesting hierarchy of such static objects complicates the task even more and leads to complete chaos in situations more complex than a simple flying cube.

? Is there a way to get around this problem in your particular situation?
> Theoretically, it is possible if we send all the raycasts we need once per regular frame (Update), and not once per FixedStep. But this will immediately cause a bunch of problems with physical raycasts (checking bullet hits, hovering the character's capsule above the surface, etc.), such raycasts should be sent every FixedStep, and not every Update.