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

struct FBoxSphereBounds
{
    /** Holds the origin of the bounding box and sphere. */
    float3 Origin;
    /** Holds the extent of the bounding box, which is half the size of the box in 3D space */
    float3 BoxExtent;
    /** Holds the radius of the bounding sphere. */
    float SphereRadius;
};

#define LOD_LEVEL 3

#endif