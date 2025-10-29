#ifndef GPUINSTANCE_INCLUDED
#define GPUINSTANCE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

struct IndirectArgs
{
    uint numVerticesPerInstance;    //Mesh 顶点数量
    uint numInstances;              //绘制数量
    uint startVertexIndex;
    uint startInstanceIndex;
    uint startLocation;
};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
StructuredBuffer<float4x4>          _TransformBuffer;
StructuredBuffer<uint>              _GroupIDBuffer;
#endif

void SetupGPUInstance()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

    // uint offsetIndeex = _IndirectArgsBuffer[_ArgIndex].startLocation;
    // InstanceData instanceData = _DrawTriangles[offsetIndeex + unity_InstanceID];
    //
    // //响应缩放
    // float distanceFromCamera = distance(instanceData.Position, _CameraPositionWS);
    // float distanceFade = 1 - saturate((distanceFromCamera - _DistanceFadeMin) / (_DistanceFadeMax - _DistanceFadeMin));
    //
    // // 随机旋转
    // float randomisedPos = rand(instanceData.Position);
    // float4x4 facingRotationMatrix = AngleAxis4x4(randomisedPos * TWO_PI, float3(0, 1, 0));
    //
    // unity_ObjectToWorld = 0;
    // unity_ObjectToWorld = facingRotationMatrix;
    // unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.Position, 1);
    // unity_ObjectToWorld._m00_m11_m22 = instanceData.Scale * distanceFade;//scale

    uint offsetIndeex = 0;
    unity_ObjectToWorld = _TransformBuffer[offsetIndeex + unity_InstanceID];

    // unity_WorldToObject = unity_ObjectToWorld;

    #endif //UNITY_PROCEDURAL_INSTANCING_ENABLED

}

#endif
