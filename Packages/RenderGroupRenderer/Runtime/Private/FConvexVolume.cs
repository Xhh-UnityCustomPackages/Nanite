using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static RenderGroupRenderer.UnrealMathFPU;

namespace RenderGroupRenderer
{
    /**
    * Builds the permuted planes for SIMD fast clipping
    */
    public struct FConvexVolume
    {
        private NativeArray<float4> Planes;
        private NativeArray<float4> PermutedPlanes;
        
        public NativeArray<float4> cullingPlaneArray => Planes;

        public void Init()
        {
            if (!Planes.IsCreated) Planes = new NativeArray<float4>(6, Allocator.Persistent);
            if (!PermutedPlanes.IsCreated) PermutedPlanes = new NativeArray<float4>(8, Allocator.Persistent);
            
            int NumToAdd = Planes.Length / 4;
            int NumRemaining = Planes.Length % 4;
                
            for (int Count = 0, Offset = 0; Count < NumToAdd; Count++, Offset += 4)
            {
                var planes0 = Planes[Offset + 0];
                var planes1 = Planes[Offset + 1];
                var planes2 = Planes[Offset + 2];
                var planes3 = Planes[Offset + 3];

                // Add them in SSE ready form
                PermutedPlanes[Count + 0] = new float4(planes0.x, planes1.x, planes2.x, planes3.x);
                PermutedPlanes[Count + 1] = new float4(planes0.y, planes1.y, planes2.y, planes3.y);
                PermutedPlanes[Count + 2] = new float4(planes0.z, planes1.z, planes2.z, planes3.z);
                PermutedPlanes[Count + 3] = new float4(planes0.w, planes1.w, planes2.w, planes3.w);
            }

            if (NumRemaining>0)
            {
                float4 Last1, Last2, Last3, Last4;
                switch (NumRemaining)
                {
                    case 3:
                    {
                        Last1 = Planes[NumToAdd * 4 + 0];
                        Last2 = Planes[NumToAdd * 4 + 1];
                        Last3 = Planes[NumToAdd * 4 + 2];
                        Last4 = Last1;
                        break;
                    }
                    case 2:
                    {
                        Last1 = Planes[NumToAdd * 4 + 0];
                        Last2 = Planes[NumToAdd * 4 + 1];
                        Last3 = Last4 = Last1;
                        break;
                    }
                    case 1:
                    {
                        Last1 = Planes[NumToAdd * 4 + 0];
                        Last2 = Last3 = Last4 = Last1;
                        break;
                    }
                    default:
                    {
                        Last1 = float4.zero;
                        Last2 = Last3 = Last4 = Last1;
                        break;
                    }
                }
                
                PermutedPlanes[4] = new float4(Last1.x, Last2.x, Last3.x, Last4.x);
                PermutedPlanes[5] = new float4(Last1.y, Last2.y, Last3.y, Last4.y);
                PermutedPlanes[6] = new float4(Last1.z, Last2.z, Last3.z, Last4.z);
                PermutedPlanes[7] = new float4(Last1.w, Last2.w, Last3.w, Last4.w);
            }
        }

        public void Update(Plane[] cullingPlanes)
        {
            for (int i = 0; i < cullingPlanes.Length; i++)
            {
                Planes[i] = new float4(cullingPlanes[i].normal.x, cullingPlanes[i].normal.y, cullingPlanes[i].normal.z, cullingPlanes[i].distance);
            }
            Init();
        }

        public void Dispose()
        {
            if (Planes.IsCreated)
                Planes.Dispose();
        }
        

        /// <summary>
        /// ConvexVolume.cpp 160
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IntersectBoxWithPermutedPlanes(NativeArray<float4> PermutedPlanes, float3 BoxOrigin, float3 BoxExtent)
        {
            // Splat origin into 3 vectors
            float4 OrigX = VectorReplicate(BoxOrigin, 0);
            float4 OrigY = VectorReplicate(BoxOrigin, 1);
            float4 OrigZ = VectorReplicate(BoxOrigin, 2);
            // Splat extent into 3 vectors
            float4 ExtentX = VectorReplicate(BoxExtent, 0);
            float4 ExtentY = VectorReplicate(BoxExtent, 1);
            float4 ExtentZ = VectorReplicate(BoxExtent, 2);
            // Splat the abs for the pushout calculation
            var AbsExt = VectorAbs(BoxExtent);
            float4 AbsExtentX = VectorReplicate(AbsExt, 0);
            float4 AbsExtentY = VectorReplicate(AbsExt, 1);
            float4 AbsExtentZ = VectorReplicate(AbsExt, 2);


            for (int Count = 0, Num = PermutedPlanes.Length; Count < Num; Count += 4)
            {
                var PlanesX = PermutedPlanes[Count + 0];
                var PlanesY = PermutedPlanes[Count + 1];
                var PlanesZ = PermutedPlanes[Count + 2];
                var PlanesW = PermutedPlanes[Count + 3];

                // Calculate the distance (x * x) + (y * y) + (z * z) - w
                float4 DistX = VectorMultiply(OrigX, PlanesX);
                float4 DistY = VectorMultiplyAdd(OrigY, PlanesY, DistX);
                float4 DistZ = VectorMultiplyAdd(OrigZ, PlanesZ, DistY);
                float4 Distance = VectorSubtract(DistZ, PlanesW);

                // Now do the push out FMath::Abs(x * x) + FMath::Abs(y * y) + FMath::Abs(z * z)
                float4 PushX = VectorMultiply(AbsExtentX, VectorAbs(PlanesX));
                float4 PushY = VectorMultiplyAdd(AbsExtentY, VectorAbs(PlanesY), PushX);
                float4 PushOut = VectorMultiplyAdd(AbsExtentZ, VectorAbs(PlanesZ), PushY);

                // Check for completely outside
                if (VectorAnyGreaterThan(Distance, PushOut))
                {
                    return false;
                }
            }

            return false;
        }

        public bool IntersectBox(float3 Origin, float3 Extent)
        {
            return IntersectBoxWithPermutedPlanes(PermutedPlanes, Origin, Extent);
        }

        [BurstCompile]
        public bool IntersectSphere(float3 Origin, float Radius)
        {
            float4 VRadius = new float4(Radius, Radius, Radius, Radius);
            // Splat origin into 3 vectors
            float4 OrigX = VectorReplicate(Origin, 0);
            float4 OrigY = VectorReplicate(Origin, 1);
            float4 OrigZ = VectorReplicate(Origin, 2);

            for (int Count = 0; Count < PermutedPlanes.Length; Count += 4)
            {
                var PlanesX = PermutedPlanes[Count + 0];
                var PlanesY = PermutedPlanes[Count + 1];
                var PlanesZ = PermutedPlanes[Count + 2];
                var PlanesW = PermutedPlanes[Count + 3];

                // Calculate the distance (x * x) + (y * y) + (z * z) - w
                float4 DistX = VectorMultiply(OrigX, PlanesX);
                float4 DistY = VectorMultiplyAdd(OrigY, PlanesY, DistX);
                float4 DistZ = VectorMultiplyAdd(OrigZ, PlanesZ, DistY);
                float4 Distance = VectorSubtract(DistZ, PlanesW);

                // Check for completely outside
                if (VectorAnyGreaterThan(Distance, VRadius))
                {
                    return false;
                }
            }

            return true;
        }
    }
}