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
            FConvexVolume convexVolume,
            NativeArray<FBoxSphereBounds> bounds,
            NativeArray<int> groupIDs,
            NativeArray<bool> results
            )
        {
            RenderGroupCulling job = new RenderGroupCulling();
            job.convexVolume = convexVolume;
            job.groupIDs = groupIDs;
            job.AllBounds = bounds;
            job.Results = results;
            return job;
        }
    
        [ReadOnly] public NativeArray<int> groupIDs;
        [ReadOnly] public NativeArray<FBoxSphereBounds> AllBounds; //全部物体的包围盒
        [ReadOnly] public FConvexVolume convexVolume; //摄像机视锥平面
        
        // 剔除结果
        public NativeArray<bool> Results;
            
        
        public void Execute(int index)
        {
            var itemBounds = AllBounds[index];
            
            bool isFrustumCulled = IsFrustumCulled(itemBounds);
            
            bool show = !isFrustumCulled;
            
            Results[index] = show;
        }

        bool IsFrustumCulled(FBoxSphereBounds itemBounds)
        {
            return !convexVolume.IntersectBox(itemBounds.Origin, itemBounds.BoxExtent);
        }
    }
}