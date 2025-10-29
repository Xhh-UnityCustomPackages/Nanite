#ifndef GPUINSTANCE_INCLUDED
#define GPUINSTANCE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "GPUInstanceDefine.hlsl"

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
uint _ArgIndex;
StructuredBuffer<float4x4>          _TransformBuffer;
StructuredBuffer<uint>              _GroupIDBuffer;
StructuredBuffer<IndirectArgs>      _IndirectArgsBuffer;
StructuredBuffer<uint>              _SortIDBuffer;
#endif

void SetupGPUInstance()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    
    uint offsetIndex = _IndirectArgsBuffer[_ArgIndex].startLocation;
    uint itemIndex = _SortIDBuffer[offsetIndex + unity_InstanceID];
    unity_ObjectToWorld = _TransformBuffer[itemIndex];

    // unity_WorldToObject = unity_ObjectToWorld;

    #endif //UNITY_PROCEDURAL_INSTANCING_ENABLED

}

#endif
