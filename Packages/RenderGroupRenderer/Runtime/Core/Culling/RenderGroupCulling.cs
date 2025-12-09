using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RenderGroupRenderer
{
    [BurstCompile(CompileSynchronously = true)]
    public struct RenderGroupCulling : IJobParallelFor
    {
        public static RenderGroupCulling CreateJob(
            FFrustumCullingFlags Flags,
            FConvexVolume convexVolume,
            NativeArray<FBoxSphereBounds> bounds,
            NativeArray<int> groupIDs,
            NativeArray<bool> results
            )
        {
            RenderGroupCulling job = new RenderGroupCulling();
            job.Flags = Flags;
            job.ViewCullingFrustum = convexVolume;
            job.groupIDs = groupIDs;
            job.AllBounds = bounds;
            job.Results = results;
            return job;
        }
    
        [ReadOnly] public FFrustumCullingFlags Flags;
        [ReadOnly] public NativeArray<int> groupIDs;
        [ReadOnly] public NativeArray<FBoxSphereBounds> AllBounds; //全部物体的包围盒
        [ReadOnly] public FConvexVolume ViewCullingFrustum; //摄像机视锥平面
        
        // 剔除结果
        public NativeArray<bool> Results;
            
        
        public void Execute(int index)
        {
            var itemBounds = AllBounds[index];
            
            Results[index] = IsPrimitiveVisible(itemBounds);
        }

        bool IsPrimitiveVisible(FBoxSphereBounds Bounds)
        {
            if (Flags.bUseSphereTestFirst && !ViewCullingFrustum.IntersectSphere(Bounds.Origin, Bounds.SphereRadius))
            {
                return false;
            }
            
            return ViewCullingFrustum.IntersectBox(Bounds.Origin, Bounds.BoxExtent);
        }
    }
}