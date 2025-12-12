using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RenderGroupRenderer
{
    public struct OccluderSubviewUpdate
    {
        /// <summary>The transform from world space to view space when rendering the depth buffer.</summary>
        public Matrix4x4 viewMatrix;
        /// <summary>The transform from view space to world space when rendering the depth buffer.</summary>
        public Matrix4x4 invViewMatrix;
        /// <summary>The GPU projection matrix when rendering the depth buffer.</summary>
        public Matrix4x4 gpuProjMatrix;
        /// <summary>An additional world space offset to apply when moving between world space and view space.</summary>
        public Vector3 viewOffsetWorldSpace;
        
        public OccluderSubviewUpdate(int subviewIndex)
        {
            // this.subviewIndex = subviewIndex;
            //
            // this.depthSliceIndex = 0;
            // this.depthOffset = Vector2Int.zero;

            this.viewMatrix = Matrix4x4.identity;
            this.invViewMatrix = Matrix4x4.identity;
            this.gpuProjMatrix = Matrix4x4.identity;
            this.viewOffsetWorldSpace = Vector3.zero;
        }

        public OccluderSubviewUpdate(CameraData cameraData)
        {
            var viewMatrix = cameraData.GetViewMatrix(0);
            var projMatrix = cameraData.GetProjectionMatrix(0);
            
            this.viewMatrix = viewMatrix;
            this.invViewMatrix = viewMatrix.inverse;
            this.gpuProjMatrix = GL.GetGPUProjectionMatrix(projMatrix, true);
            this.viewOffsetWorldSpace = Vector3.zero;
        }

        public OccluderSubviewUpdate(UniversalCameraData cameraData)
        {
            var viewMatrix = cameraData.GetViewMatrix(0);
            var projMatrix = cameraData.GetProjectionMatrix(0);
            
            this.viewMatrix = viewMatrix;
            this.invViewMatrix = viewMatrix.inverse;
            this.gpuProjMatrix = GL.GetGPUProjectionMatrix(projMatrix, true);
            this.viewOffsetWorldSpace = Vector3.zero;
        }
    }
}