using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace RenderGroupRenderer
{
    [BurstCompile]
    public static class UnrealMathFPU
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]//内联函数 相当于UE 的 FORCEINLINE
        public static float4 VectorReplicate(in float3 Vec, int ElementIndex)
        {
            float value = Vec[ElementIndex];
            return new float4(value, value, value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 VectorAbs(in float3 Vec)
        {
            float3 Vec2;
            Vec2.x = Math.Abs(Vec.x);
            Vec2.y = Math.Abs(Vec.y);
            Vec2.z = Math.Abs(Vec.z);
            return Vec2;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 VectorAbs(in float4 Vec)
        {
            float4 Vec2;
            Vec2.x = Math.Abs(Vec.x);
            Vec2.y = Math.Abs(Vec.y);
            Vec2.z = Math.Abs(Vec.z);
            Vec2.w = Math.Abs(Vec.w);
            return Vec2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 VectorMultiply(in float4 Vec1, in float4 Vec2)
        {
            float4 Vec;
            Vec.x = Vec1.x * Vec2.x;
            Vec.y = Vec1.y * Vec2.y;
            Vec.z = Vec1.z * Vec2.z;
            Vec.w = Vec1.w * Vec2.w;
            return Vec;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 VectorMultiplyAdd(in float4 Vec1, in float4 Vec2, in float4 Vec3)
        {
            float4 Vec;
            Vec.x = Vec1.x * Vec2.x + Vec3.x;
            Vec.y = Vec1.y * Vec2.y + Vec3.y;
            Vec.z = Vec1.z * Vec2.z + Vec3.z;
            Vec.w = Vec1.w * Vec2.w + Vec3.w;
            return Vec;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 VectorSubtract(in float4 Vec1, in float4 Vec2)
        {
            float4 Vec;
            Vec.x = Vec1.x - Vec2.x;
            Vec.y = Vec1.y - Vec2.y;
            Vec.z = Vec1.z - Vec2.z;
            Vec.w = Vec1.w - Vec2.w;
            return Vec;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VectorAnyGreaterThan(in float4 Vec1, in float4 Vec2)
        {
            // Note: Bitwise OR:ing all results together to avoid branching.
            return (Vec1.x > Vec2.x) | (Vec1.y > Vec2.y) | (Vec1.z > Vec2.z) | (Vec1.w > Vec2.w);
        }
    }
}