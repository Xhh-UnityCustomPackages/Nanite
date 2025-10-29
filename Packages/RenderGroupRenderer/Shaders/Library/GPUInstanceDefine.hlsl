#ifndef GPUINSTANCE_DEFINE_INCLUDED
#define GPUINSTANCE_DEFINE_INCLUDED

struct IndirectArgs
{
    uint numVerticesPerInstance;    //Mesh 顶点数量
    uint numInstances;              //绘制数量
    uint startVertexIndex;
    uint startInstanceIndex;
    uint startLocation;
};

struct Bounds
{
    float3 position;
    float3 extents;
};

#endif