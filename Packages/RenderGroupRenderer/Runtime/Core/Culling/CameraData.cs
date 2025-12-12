using Unity.Collections;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class CullingCameraData
    {
        public Camera camera;
        
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
        
        private bool m_IsFirst;
        private Plane[] m_CullingPlanes = new Plane[6];
        
        public Plane[] cullingPlanes => m_CullingPlanes;

        public void SetCamera(Camera camera)
        {
            this.camera = camera;
        }

        public void Dispose()
        {
        }
        
        public void CalculateCameraData()
        {
            if (camera == null)
                return;

            GeometryUtility.CalculateFrustumPlanes(camera, m_CullingPlanes);
        }

        public FConvexVolume GetCullingFrustum()
        {
            FConvexVolume volume = new FConvexVolume();
            volume.Init();
            return volume;
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