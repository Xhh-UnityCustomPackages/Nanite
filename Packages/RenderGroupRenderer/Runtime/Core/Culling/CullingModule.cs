using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class CullingModule
    {
        private Camera m_CullingCamera;
        private CameraData m_CameraData;

        private NativeArray<Bounds> m_CullingBoundsNativeArray;
        private NativeArray<bool> m_CullingResultNativeArray;

        public CameraData CameraData => m_CameraData;
        public NativeArray<bool> CullingResultNativeArray => m_CullingResultNativeArray;

        public CullingModule()
        {
            m_CameraData = new CameraData();
        }

        public void SetCullingCamera(Camera camera)
        {
            m_CullingCamera = camera;
            m_CameraData.SetCamera(camera);
        }

        public void Init(RenderGroup[] renderGroups)
        {
            int groupCount = renderGroups.Length;
            m_CullingBoundsNativeArray = new NativeArray<Bounds>(groupCount, Allocator.Persistent);
            m_CullingResultNativeArray = new NativeArray<bool>(groupCount, Allocator.Persistent);
            for (int i = 0; i < groupCount; i++)
            {
                m_CullingBoundsNativeArray[i] = renderGroups[i].bounds;
                m_CullingResultNativeArray[i] = false;
            }
        }

        public void Dispose()
        {
            m_CullingBoundsNativeArray.Dispose();
            m_CullingResultNativeArray.Dispose();
            m_CameraData.Dispose();
        }

        public void OnUpdate()
        {
            if (m_CameraData.IsCameraDirty())
            {
                m_CameraData.CalculateCameraData();
                CPUCulling();
            }
        }

        void CPUCulling()
        {
            int length = m_CullingBoundsNativeArray.Length;

            var cullJobs = RenderGroupCulling.CreateJob(m_CameraData.cullingPlaneArray, m_CullingBoundsNativeArray, m_CullingResultNativeArray);
            var job = cullJobs.Schedule(length, length);
            job.Complete();
        }

        public void OnDrawGizmos()
        {
            // Gizmos.DrawFrustum(c);
            // for (int i = 0; i < m_CullingBoundsNativeArray.Length; i++)
            // {
            //     var bounds = m_CullingBoundsNativeArray[i];
            //     var result = m_CullingResultNativeArray[i];
            //     Gizmos.color = result ? Color.green : Color.red;
            //     Gizmos.DrawWireCube(bounds.center, bounds.size);
            // }
        }
    }
}