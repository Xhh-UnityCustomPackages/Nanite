namespace RenderGroupRenderer
{
    // 八叉树元素ID
    public struct FOctreeElementId2
    {
        private uint nodeIndex;
        private int elementIndex;

        public static readonly uint INDEX_NONE = uint.MaxValue;
        public static readonly int INDEX_NONE_INT = -1;

        public FOctreeElementId2(uint nodeIndex, int elementIndex)
        {
            this.nodeIndex = nodeIndex;
            this.elementIndex = elementIndex;
        }

        public bool IsValidId()
        {
            return nodeIndex != INDEX_NONE;
        }

        public uint GetNodeIndex() => nodeIndex;

        public static implicit operator int(FOctreeElementId2 id)
        {
            return id.elementIndex;
        }

        public override string ToString()
        {
            return $"[{nodeIndex}:{elementIndex}]";
        }
    }
}