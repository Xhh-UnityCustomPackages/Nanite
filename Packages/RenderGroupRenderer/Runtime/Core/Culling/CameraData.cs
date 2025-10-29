using Unity.Collections;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class CameraData
    {
        public Camera camera;
        
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
        
        private bool m_IsFirst;
        private Plane[] m_CullingPlanes = new Plane[6];
        private NativeArray<Plane> m_CullingPlanesNativeArray;
        
        public Plane[] cullingPlanes => m_CullingPlanes;
        public NativeArray<Plane> cullingPlaneArray => m_CullingPlanesNativeArray;

        public void SetCamera(Camera camera)
        {
            this.camera = camera;
        }

        public void Dispose()
        {
            m_CullingPlanesNativeArray.Dispose();
        }
        
        public void CalculateCameraData()
        {
            if (camera == null)
                return;

            GeometryUtility.CalculateFrustumPlanes(camera, m_CullingPlanes);
            if (!m_CullingPlanesNativeArray.IsCreated)
                m_CullingPlanesNativeArray = new NativeArray<Plane>(m_CullingPlanes.Length, Allocator.Persistent);
            for (int i = 0; i < m_CullingPlanes.Length; i++)
            {
                m_CullingPlanesNativeArray[i] = m_CullingPlanes[i];
            }
        }
        
        public bool IsCameraDirty()
        {
            return IsCameraDirty(camera);
        }

        private bool Vector3Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        private bool QuaternionApproximately(Quaternion a, Quaternion b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z) &&
                   Mathf.Approximately(a.w, b.w);
        }
        
        private bool IsCameraDirty(Camera camera)
        {
            if (camera == null)
                return false;

            if (!m_IsFirst)
            {
                m_IsFirst = true;
                return true;
            }

            var cameraRotation = camera.transform.rotation;
            var cameraPosition = camera.transform.position;
            var cameraFOV = camera.fieldOfView;
            if (QuaternionApproximately(cameraRotation, rotation) &&
                Vector3Approximately(cameraPosition, position) &&
                Mathf.Approximately(cameraFOV, fov))
            {
                return false;
            }

            this.position = cameraPosition;
            this.rotation = cameraRotation;
            this.fov = cameraFOV;
            return true;
        }
    }
}