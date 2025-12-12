using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RenderGroupRenderer
{
    public struct OccluderDerivedData
    {
        /// <summary></summary>
        public Matrix4x4 viewProjMatrix; // from view-centered world space
        /// <summary></summary>
        public Vector4 viewOriginWorldSpace;
        /// <summary></summary>
        public Vector4 facingDirWorldSpace;

        public static OccluderDerivedData FromParameters(in OccluderSubviewUpdate occluderSubviewUpdate)
        {
            var origin = occluderSubviewUpdate.viewOffsetWorldSpace + (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(3); // view origin in world space
            var xViewVec = (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(0); // positive x axis in world space
            var yViewVec = (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(1); // positive y axis in world space
            var towardsVec = (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(2); // positive z axis in world space
            
            var viewMatrixNoTranslation = occluderSubviewUpdate.viewMatrix;
            viewMatrixNoTranslation.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            
            return new OccluderDerivedData
            {
                viewOriginWorldSpace = origin,
                facingDirWorldSpace = towardsVec.normalized,
                // radialDirWorldSpace = (xViewVec + yViewVec).normalized,
                viewProjMatrix = occluderSubviewUpdate.gpuProjMatrix * viewMatrixNoTranslation
            };
        }
    }
}