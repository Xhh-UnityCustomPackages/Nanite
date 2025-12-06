using UnityEngine;

namespace RenderGroupRenderer
{
    /**
    * Bounding information used to cull primitives in the scene.
    */
    public struct FPrimitiveBounds
    {
        public FBoxSphereBounds BoxSphereBounds;

        /** Square of the minimum draw distance for the primitive. */
        float MinDrawDistance;

        /** Maximum draw distance for the primitive. */
        float MaxDrawDistance;
    }

    /**
    * A bounding box and bounding sphere with the same origin.
    * @note The full C++ class is located here : Engine\Source\Runtime\Core\Public\Math\BoxSphereBounds.h
    */
    public struct FBoxSphereBounds
    {
        /** Holds the origin of the bounding box and sphere. */
        public Vector3 Origin;
        
        /** Holds the extent of the bounding box, which is half the size of the box in 3D space */
        public Vector3 BoxExtent;
        
        /** Holds the radius of the bounding sphere. */
        public float SphereRadius;
    }
}