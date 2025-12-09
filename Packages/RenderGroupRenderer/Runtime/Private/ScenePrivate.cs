using System.Runtime.CompilerServices;
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

        public FBoxSphereBounds(Bounds bounds)
        {
            this.Origin = bounds.center;
            this.BoxExtent = bounds.extents;
            this.SphereRadius = BoxExtent.magnitude * 2;
        }

        public FBoxSphereBounds(Vector3 Origin, Vector3 BoxExtent)
        {
            this.Origin = Origin;
            this.BoxExtent = BoxExtent;
            this.SphereRadius = BoxExtent.magnitude * 2;
        }
        
        public Vector3 min
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this.Origin - this.BoxExtent;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => this.SetMinMax(value, this.max);
        }

        public Vector3 max
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this.Origin + this.BoxExtent;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => this.SetMinMax(this.min, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMinMax(Vector3 min, Vector3 max)
        {
            this.BoxExtent = (max - min) * 0.5f;
            this.Origin = min + this.BoxExtent;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(Vector3 point)
        {
            this.SetMinMax(Vector3.Min(this.min, point), Vector3.Max(this.max, point));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(FBoxSphereBounds bounds)
        {
            this.Encapsulate(bounds.Origin - bounds.BoxExtent);
            this.Encapsulate(bounds.Origin + bounds.BoxExtent);
            this.SphereRadius = BoxExtent.magnitude * 2;
        }
    }
}