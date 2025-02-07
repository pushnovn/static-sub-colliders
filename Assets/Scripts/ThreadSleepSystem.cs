using System.Threading;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct ThreadSleepSystem : ISystem
{
    public void OnUpdate (ref SystemState state)
    {
        // Simple simulation of long frame to let FixedStep performs several times per frame
        Thread.Sleep(30);
        Debug.Log($"ThreadSleep: {SystemAPI.Time.ElapsedTime:F3}");
    }
}