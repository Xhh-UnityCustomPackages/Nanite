using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RenderGroupRenderer
{
    [BurstCompile(CompileSynchronously = true)]
    public struct PrepareRenderGroupCulling : IJobParallelFor
    {
        public static PrepareRenderGroupCulling CreateJob(
            NativeList<int> visibleNodes,
            NativeArray<FBoxSphereBounds> allBounds,
            NativeArray<int> groupIDs,
            NativeArray<FBoxSphereBounds> bounds,
            NativeArray<bool> results
        )
        {
            PrepareRenderGroupCulling job = new ();
            job.groupIndexes = visibleNodes;
            job.groupIDs = groupIDs;
            job.allGroupBounds = allBounds;
            job.bounds = bounds;
            job.results = results;
            return job;
        }
        
        [ReadOnly] NativeList<int> groupIndexes;
        [ReadOnly] NativeArray<FBoxSphereBounds> allGroupBounds;
        
        public NativeArray<int> groupIDs; //全部物体的包围盒
        public NativeArray<FBoxSphereBounds> bounds; //全部物体的包围盒
        public NativeArray<bool> results; //全部物体的包围盒

        public void Execute(int index)
        {
            var groupIndex = groupIndexes[index];
            groupIDs[index] = groupIndex;
            bounds[index] = allGroupBounds[groupIndex];
            results[index] = false;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct RenderGroupCulling : IJobParallelFor
    {
        public static RenderGroupCulling CreateJob(
            FFrustumCullingFlags Flags,
            FConvexVolume convexVolume,
            NativeArray<FBoxSphereBounds> bounds,
            NativeArray<bool> results
            )
        {
            RenderGroupCulling job = new RenderGroupCulling();
            job.Flags = Flags;
            job.ViewCullingFrustum = convexVolume;
            job.AllBounds = bounds;
            job.Results = results;
            return job;
        }
    
        [ReadOnly] public FFrustumCullingFlags Flags;
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

    [BurstCompile(CompileSynchronously = true)]
    public struct AfterRenderGroupCulling : IJobParallelFor
    {
        public static AfterRenderGroupCulling CreateJob(
            NativeArray<int> groupIDs,
            NativeArray<bool> cullResults,
            NativeArray<RenderGroup> renderGroups,
            NativeArray<RenderGroupItem> renderItems,
            NativeArray<uint> totalCullResult
        )
        {
            AfterRenderGroupCulling job = new AfterRenderGroupCulling();
            job.groupIDs = groupIDs;
            job.cullResults = cullResults;
            job.renderGroups = renderGroups;
            job.renderItems = renderItems;
            job.totalCullResult = totalCullResult;
            return job;
        }
        
        [ReadOnly] public NativeArray<int> groupIDs;
        [ReadOnly] public NativeArray<bool> cullResults;
        [ReadOnly] public NativeArray<RenderGroupItem> renderItems;
        
        public NativeArray<RenderGroup> renderGroups;
        
        // 允许跨工作块访问
        [NativeDisableParallelForRestriction] 
        public NativeArray<uint> totalCullResult;

        public void Execute(int index)
        {
            bool result = cullResults[index];
            var groupID = groupIDs[index];

            var renderGroup = renderGroups[groupID];
            if (result)
                renderGroup.SetCPUCullingResult(RenderGroup.ShowState.PassBVHCulling);
            renderGroups[groupID] = renderGroup;
            
            for (int j = 0; j < renderGroup.itemCount; j++)
            {
                var itemID = renderItems[renderGroup.itemStartIndex + j].itemID;
                totalCullResult[itemID] = (uint)(result ? 1 : 0);
            }
        }
    }
}