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
            NativeArray<float4> frustumPlanes,
            NativeArray<Bounds> bounds,
            NativeArray<int> groupIDs,
            NativeArray<bool> results
            )
        {
            RenderGroupCulling job = new RenderGroupCulling();
            job.FrustumPlanes = frustumPlanes;
            job.groupIDs = groupIDs;
            job.AllBounds = bounds;
            job.Results = results;
            return job;
        }
    
        [ReadOnly] public NativeArray<int> groupIDs;
        [ReadOnly] public NativeArray<Bounds> AllBounds; //全部物体的包围盒
        [ReadOnly] public NativeArray<float4> FrustumPlanes; //摄像机视锥平面
        
        // 剔除结果
        public NativeArray<bool> Results;
            
        
        public void Execute(int index)
        {
            var itemBounds = AllBounds[index];
            
            bool isFrustumCulled = IsFrustumCulled(itemBounds);
            
            bool show = !isFrustumCulled;
            
            Results[index] = show;
        }

        bool IsFrustumCulled(Bounds itemBounds)
        {
            float3 center = itemBounds.center;
            float3 extents = itemBounds.extents;

            // 检查包围盒是否与视锥的每个平面相交
            for (int i = 0; i < 6; i++)
            {
                float3 planeNormal = FrustumPlanes[i].xyz;
                float planeDistance = FrustumPlanes[i].w;

                // 计算包围盒在平面法线方向上的投影半径
                float projectedRadius = math.dot(extents, math.abs(planeNormal));

                // 计算包围盒中心到平面的距离
                float distanceToPlane = math.dot(planeNormal, center) + planeDistance;

                // 如果包围盒完全在平面的负侧，则被剔除
                if (distanceToPlane < -projectedRadius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}