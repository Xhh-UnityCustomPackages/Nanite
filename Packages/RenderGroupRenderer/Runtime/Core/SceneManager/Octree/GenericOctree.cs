using System;
using System.Collections.Generic;
using UnityEngine;

namespace RenderGroupRenderer
{
    // 中心点和范围表示的包围盒
    public struct FBoxCenterAndExtent
    {
        public Vector3 Center;
        public Vector3 Extent;

        public FBoxCenterAndExtent(Vector3 center, Vector3 extent)
        {
            Center = center;
            Extent = extent;
        }

        public FBoxCenterAndExtent(Bounds bounds)
        {
            Center = bounds.center;
            Extent = bounds.extents;
        }

        public Bounds GetBounds()
        {
            return new Bounds(Center, Extent * 2);
        }

        public static bool Intersect(FBoxCenterAndExtent a, FBoxCenterAndExtent b)
        {
            Vector3 centerDiff = new Vector3(
                Mathf.Abs(a.Center.x - b.Center.x),
                Mathf.Abs(a.Center.y - b.Center.y),
                Mathf.Abs(a.Center.z - b.Center.z)
            );

            Vector3 compositeExtent = a.Extent + b.Extent;

            return centerDiff.x <= compositeExtent.x &&
                   centerDiff.y <= compositeExtent.y &&
                   centerDiff.z <= compositeExtent.z;
        }

        public static bool Intersect(Bounds a, FBoxCenterAndExtent b)
        {
            return Intersect(new FBoxCenterAndExtent(a.center, a.extents), b);
        }
    }

    // 子节点引用
    public struct FOctreeChildNodeRef
    {
        public byte Index;

        public FOctreeChildNodeRef(byte index)
        {
            Index = index;
        }

        public FOctreeChildNodeRef(int x, int y, int z)
        {
            Index = (byte)((x & 1) | ((y & 1) << 1) | ((z & 1) << 2));
        }

        public void Advance()
        {
            Index++;
        }

        public bool IsNULL()
        {
            return Index >= 8;
        }

        public void SetNULL()
        {
            Index = 8;
        }

        public int X() => (Index >> 0) & 1;
        public int Y() => (Index >> 1) & 1;
        public int Z() => (Index >> 2) & 1;
    }

// 子节点子集
    public struct FOctreeChildNodeSubset
    {
        private uint allBits;

        public FOctreeChildNodeSubset(FOctreeChildNodeRef childRef)
        {
            allBits = 0;
            PositiveChildBits = childRef.Index;
            NegativeChildBits = (byte)~childRef.Index;
        }

        public bool Contains(FOctreeChildNodeRef childRef)
        {
            uint positiveBits = PositiveChildBits;
            uint negativeBits = NegativeChildBits;
            uint childBit = 1u << childRef.Index;

            return ((positiveBits & childBit) != 0) || ((negativeBits & childBit) != 0);
        }

        private uint PositiveChildBits
        {
            get => (allBits >> 0) & 0x7u;
            set => allBits = (allBits & ~0x7u) | ((value & 0x7u) << 0);
        }

        private uint NegativeChildBits
        {
            get => (allBits >> 3) & 0x7u;
            set => allBits = (allBits & ~(0x7u << 3)) | ((value & 0x7u) << 3);
        }
    }

    // 八叉树节点上下文
    public struct FOctreeNodeContext
    {
        public const int LoosenessDenominator = 16;

        public FBoxCenterAndExtent Bounds;
        public float ChildExtent;
        public float ChildCenterOffset;
        public uint InCullBits;
        public uint OutCullBits;

        public FOctreeNodeContext(FBoxCenterAndExtent bounds)
        {
            Bounds = bounds;
            float tightChildExtent = Bounds.Extent.x * 0.5f;
            float looseChildExtent = tightChildExtent * (1.0f + 1.0f / LoosenessDenominator);

            ChildExtent = looseChildExtent;
            ChildCenterOffset = Bounds.Extent.x - looseChildExtent;
            InCullBits = 0;
            OutCullBits = 0;
        }

        public FOctreeNodeContext(FBoxCenterAndExtent bounds, uint inCullBits, uint outCullBits)
            : this(bounds)
        {
            InCullBits = inCullBits;
            OutCullBits = outCullBits;
        }

        public FOctreeNodeContext GetChildContext(FOctreeChildNodeRef childRef)
        {
            Vector3 offset = new Vector3(
                childRef.X() == 1 ? ChildCenterOffset : -ChildCenterOffset,
                childRef.Y() == 1 ? ChildCenterOffset : -ChildCenterOffset,
                childRef.Z() == 1 ? ChildCenterOffset : -ChildCenterOffset
            );

            Vector3 childCenter = Bounds.Center + offset;
            Vector3 childExtent = new Vector3(ChildExtent, ChildExtent, ChildExtent);

            return new FOctreeNodeContext(new FBoxCenterAndExtent(childCenter, childExtent));
        }

        public FOctreeChildNodeSubset GetIntersectingChildren(FBoxCenterAndExtent boundingBox)
        {
            FOctreeChildNodeSubset subset = new FOctreeChildNodeSubset();

            for (byte i = 0; i < 8; i++)
            {
                FOctreeChildNodeRef childRef = new FOctreeChildNodeRef(i);
                FOctreeNodeContext childContext = GetChildContext(childRef);

                if (FBoxCenterAndExtent.Intersect(childContext.Bounds, boundingBox))
                {
                    // 这里简化处理，实际需要更精确的子集计算
                    subset = new FOctreeChildNodeSubset(childRef);
                }
            }

            return subset;
        }

        public FOctreeChildNodeRef GetContainingChild(FBoxCenterAndExtent boundingBox)
        {
            for (byte i = 0; i < 8; i++)
            {
                FOctreeChildNodeRef childRef = new FOctreeChildNodeRef(i);
                FOctreeNodeContext childContext = GetChildContext(childRef);

                // 检查边界是否完全包含在子节点中
                Vector3 min = boundingBox.Center - boundingBox.Extent;
                Vector3 max = boundingBox.Center + boundingBox.Extent;

                Vector3 childMin = childContext.Bounds.Center - childContext.Bounds.Extent;
                Vector3 childMax = childContext.Bounds.Center + childContext.Bounds.Extent;

                if (min.x >= childMin.x && max.x <= childMax.x &&
                    min.y >= childMin.y && max.y <= childMax.y &&
                    min.z >= childMin.z && max.z <= childMax.z)
                {
                    return childRef;
                }
            }

            return new FOctreeChildNodeRef(8); // NULL ref
        }
    }

    // 八叉树语义接口
    public interface IOctreeSemantics<T>
    {
        FBoxCenterAndExtent GetBoundingBox(T element);
        void SetElementId(T element, FOctreeElementId2 id);
        void ApplyOffset(T element, Vector3 offset);
        int MaxElementsPerLeaf { get; }
        int MinInclusiveElementsPerNode { get; }
        int MaxNodeDepth { get; }
    }

    // 默认的八叉树语义实现
    public abstract class DefaultOctreeSemantics<T> : IOctreeSemantics<T>
    {
        public abstract FBoxCenterAndExtent GetBoundingBox(T element);
        public abstract void SetElementId(T element, FOctreeElementId2 id);

        public virtual void ApplyOffset(T element, Vector3 offset)
        {
            // 默认实现为空，派生类需要重写
        }

        public virtual int MaxElementsPerLeaf => 8;
        public virtual int MinInclusiveElementsPerNode => 4;
        public virtual int MaxNodeDepth => 10;
    }

    // 通用八叉树
    public class TOctree2<T> where T : class
    {
        private class FNode
        {
            public uint ChildNodes = FOctreeElementId2.INDEX_NONE;
            public uint InclusiveNumElements = 0;

            public bool IsLeaf()
            {
                return ChildNodes == FOctreeElementId2.INDEX_NONE;
            }
        }

        private FOctreeNodeContext rootNodeContext;
        private List<FNode> treeNodes = new List<FNode>();
        private List<uint> parentLinks = new List<uint>();
        private List<List<T>> treeElements = new List<List<T>>();
        private Stack<uint> freeList = new Stack<uint>();

        private float minLeafExtent;
        private IOctreeSemantics<T> semantics;

        public TOctree2(Vector3 origin, float extent, IOctreeSemantics<T> semantics)
        {
            this.semantics = semantics;

            Vector3 extentVec = new Vector3(extent, extent, extent);
            rootNodeContext = new FOctreeNodeContext(new FBoxCenterAndExtent(origin, extentVec));

            minLeafExtent = extent * Mathf.Pow(
                (1.0f + 1.0f / FOctreeNodeContext.LoosenessDenominator) / 2.0f,
                semantics.MaxNodeDepth
            );

            // 添加根节点
            treeNodes.Add(new FNode());
            treeElements.Add(new List<T>());
        }

        private uint AllocateEightNodes()
        {
            if (freeList.Count > 0)
            {
                uint blockIndex = freeList.Pop();
                return blockIndex * 8 + 1;
            }
            else
            {
                uint startIndex = (uint)treeNodes.Count;

                // 添加8个节点
                for (int i = 0; i < 8; i++)
                {
                    treeNodes.Add(new FNode());
                    treeElements.Add(new List<T>());
                }

                // 添加父链接
                uint blockIndex = (startIndex - 1) / 8;
                if (parentLinks.Count <= blockIndex)
                {
                    parentLinks.AddRange(new uint[blockIndex - parentLinks.Count + 1]);
                }

                return startIndex;
            }
        }

        private void FreeEightNodes(uint index)
        {
            uint blockIndex = (index - 1) / 8;
            freeList.Push(blockIndex);

            // 清除节点数据
            for (int i = 0; i < 8; i++)
            {
                treeNodes[(int)(index + i)] = new FNode();
                treeElements[(int)(index + i)].Clear();
            }

            parentLinks[(int)blockIndex] = FOctreeElementId2.INDEX_NONE;
        }

        public void AddElement(T element)
        {
            List<T> tempElementStorage = new List<T>();
            FBoxCenterAndExtent elementBounds = semantics.GetBoundingBox(element);
            AddElementInternal(0, rootNodeContext, elementBounds, element, tempElementStorage);
        }

        private void AddElementInternal(
            uint currentNodeIndex,
            FOctreeNodeContext nodeContext,
            FBoxCenterAndExtent elementBounds,
            T element,
            List<T> tempElementStorage)
        {
            FNode node = treeNodes[(int)currentNodeIndex];
            node.InclusiveNumElements++;

            if (node.IsLeaf())
            {
                List<T> elements = treeElements[(int)currentNodeIndex];

                if (elements.Count + 1 > semantics.MaxElementsPerLeaf &&
                    nodeContext.Bounds.Extent.x > minLeafExtent)
                {
                    // 保存当前元素并清空节点
                    tempElementStorage.Clear();
                    tempElementStorage.AddRange(elements);
                    elements.Clear();

                    // 分配子节点
                    uint childStartIndex = AllocateEightNodes();
                    uint blockIndex = (childStartIndex - 1) / 8;
                    parentLinks[(int)blockIndex] = currentNodeIndex;

                    node.ChildNodes = childStartIndex;
                    node.InclusiveNumElements = 0;

                    // 重新添加原有元素
                    foreach (T childElement in tempElementStorage)
                    {
                        FBoxCenterAndExtent childElementBounds = semantics.GetBoundingBox(childElement);
                        AddElementInternal(currentNodeIndex, nodeContext, childElementBounds, childElement, tempElementStorage);
                    }

                    tempElementStorage.Clear();
                    AddElementInternal(currentNodeIndex, nodeContext, elementBounds, element, tempElementStorage);
                    return;
                }
                else
                {
                    int elementIndex = elements.Count;
                    elements.Add(element);
                    semantics.SetElementId(element, new FOctreeElementId2(currentNodeIndex, elementIndex));
                    return;
                }
            }
            else
            {
                FOctreeChildNodeRef childRef = nodeContext.GetContainingChild(elementBounds);

                if (childRef.IsNULL())
                {
                    int elementIndex = treeElements[(int)currentNodeIndex].Count;
                    treeElements[(int)currentNodeIndex].Add(element);
                    semantics.SetElementId(element, new FOctreeElementId2(currentNodeIndex, elementIndex));
                    return;
                }
                else
                {
                    uint childNodeIndex = node.ChildNodes + childRef.Index;
                    FOctreeNodeContext childNodeContext = nodeContext.GetChildContext(childRef);
                    AddElementInternal(childNodeIndex, childNodeContext, elementBounds, element, tempElementStorage);
                    return;
                }
            }
        }

        public void RemoveElement(FOctreeElementId2 elementId)
        {
            if (!elementId.IsValidId())
                return;

            uint nodeIndex = elementId.GetNodeIndex();
            int elementIndex = (int)elementId;

            List<T> elements = treeElements[(int)nodeIndex];

            // 移除元素
            int lastIndex = elements.Count - 1;
            elements[elementIndex] = elements[lastIndex];
            elements.RemoveAt(lastIndex);

            // 更新被交换元素的ID
            if (elementIndex < elements.Count)
            {
                semantics.SetElementId(elements[elementIndex], elementId);
            }

            // 更新节点计数并查找需要折叠的节点
            uint collapseNodeIndex = FOctreeElementId2.INDEX_NONE;
            uint currentNode = nodeIndex;

            while (true)
            {
                FNode node = treeNodes[(int)currentNode];
                node.InclusiveNumElements--;

                if (node.InclusiveNumElements < semantics.MinInclusiveElementsPerNode)
                {
                    collapseNodeIndex = currentNode;
                }

                if (currentNode == 0)
                    break;

                uint blockIndex = (currentNode - 1) / 8;
                currentNode = parentLinks[(int)blockIndex];
            }

            // 折叠节点
            if (collapseNodeIndex != FOctreeElementId2.INDEX_NONE)
            {
                FNode collapseNode = treeNodes[(int)collapseNodeIndex];

                if (!collapseNode.IsLeaf() &&
                    treeElements[(int)collapseNodeIndex].Count < collapseNode.InclusiveNumElements)
                {
                    List<T> collapsedElements = new List<T>();
                    CollapseNodesInternal(collapseNodeIndex, collapsedElements);

                    treeElements[(int)collapseNodeIndex] = collapsedElements;

                    // 更新元素的ID
                    for (int i = 0; i < collapsedElements.Count; i++)
                    {
                        semantics.SetElementId(
                            collapsedElements[i],
                            new FOctreeElementId2(collapseNodeIndex, i)
                        );
                    }
                }
            }
        }

        private void CollapseNodesInternal(uint nodeIndex, List<T> collapsedNodeElements)
        {
            List<T> elements = treeElements[(int)nodeIndex];
            collapsedNodeElements.AddRange(elements);
            elements.Clear();

            FNode node = treeNodes[(int)nodeIndex];

            if (!node.IsLeaf())
            {
                uint childStartIndex = node.ChildNodes;

                for (byte i = 0; i < 8; i++)
                {
                    CollapseNodesInternal(childStartIndex + i, collapsedNodeElements);
                }

                // 标记为叶子节点
                node.ChildNodes = FOctreeElementId2.INDEX_NONE;

                FreeEightNodes(childStartIndex);
            }
        }

        // 查找满足条件的节点
        public void FindNodesWithPredicate(
            Func<uint, uint, FOctreeNodeContext, bool> predicate,
            Action<uint, FOctreeNodeContext> func)
        {
            FindNodesWithPredicateInternal(
                FOctreeElementId2.INDEX_NONE,
                0,
                rootNodeContext,
                predicate,
                func
            );
        }

        private void FindNodesWithPredicateInternal(
            uint parentNodeIndex,
            uint currentNodeIndex,
            FOctreeNodeContext nodeContext,
            Func<uint, uint, FOctreeNodeContext, bool> predicate,
            Action<uint, FOctreeNodeContext> func)
        {
            if (treeNodes[(int)currentNodeIndex].InclusiveNumElements > 0)
            {
                if (predicate(parentNodeIndex, currentNodeIndex, nodeContext))
                {
                    func(currentNodeIndex, nodeContext);

                    if (!treeNodes[(int)currentNodeIndex].IsLeaf())
                    {
                        uint childStartIndex = treeNodes[(int)currentNodeIndex].ChildNodes;
                        for (byte i = 0; i < 8; i++)
                        {
                            FindNodesWithPredicateInternal(
                                currentNodeIndex,
                                childStartIndex + i,
                                nodeContext.GetChildContext(new FOctreeChildNodeRef(i)),
                                predicate,
                                func
                            );
                        }
                    }
                }
            }
        }

        // 查找满足条件的元素
        public void FindElementsWithPredicate(
            Func<uint, uint, FOctreeNodeContext, bool> predicate,
            Action<uint, T> func)
        {
            FindNodesWithPredicateInternal(
                FOctreeElementId2.INDEX_NONE,
                0,
                rootNodeContext,
                predicate,
                (nodeIndex, nodeContext) =>
                {
                    foreach (T element in treeElements[(int)nodeIndex])
                    {
                        func(nodeIndex, element);
                    }
                }
            );
        }

        // 查询方法
        public void FindElementsWithBoundsTest(FBoxCenterAndExtent boxBounds, Action<T> func)
        {
            FindElementsWithBoundsTestInternal(0, rootNodeContext, boxBounds, func);
        }

        private void FindElementsWithBoundsTestInternal(
            uint currentNodeIndex,
            FOctreeNodeContext nodeContext,
            FBoxCenterAndExtent boxBounds,
            Action<T> func)
        {
            FNode node = treeNodes[(int)currentNodeIndex];

            if (node.InclusiveNumElements > 0)
            {
                // 检查当前节点的元素
                foreach (T element in treeElements[(int)currentNodeIndex])
                {
                    if (FBoxCenterAndExtent.Intersect(semantics.GetBoundingBox(element), boxBounds))
                    {
                        func(element);
                    }
                }

                // 检查子节点
                if (!node.IsLeaf())
                {
                    FOctreeChildNodeSubset intersectingChildren = nodeContext.GetIntersectingChildren(boxBounds);
                    uint childStartIndex = node.ChildNodes;

                    for (byte i = 0; i < 8; i++)
                    {
                        if (intersectingChildren.Contains(new FOctreeChildNodeRef(i)))
                        {
                            FindElementsWithBoundsTestInternal(
                                childStartIndex + i,
                                nodeContext.GetChildContext(new FOctreeChildNodeRef(i)),
                                boxBounds,
                                func
                            );
                        }
                    }
                }
            }
        }

        public FOctreeElementId2 FindFirstElementWithBoundsTest(FBoxCenterAndExtent boxBounds, Func<T, bool> func)
        {
            return FindFirstElementWithBoundsTestInternal(0, rootNodeContext, boxBounds, func);
        }

        private FOctreeElementId2 FindFirstElementWithBoundsTestInternal(
            uint currentNodeIndex,
            FOctreeNodeContext nodeContext,
            FBoxCenterAndExtent boxBounds,
            Func<T, bool> func)
        {
            FNode node = treeNodes[(int)currentNodeIndex];

            if (node.InclusiveNumElements > 0)
            {
                List<T> elements = treeElements[(int)currentNodeIndex];

                for (int i = 0; i < elements.Count; i++)
                {
                    T element = elements[i];
                    if (FBoxCenterAndExtent.Intersect(semantics.GetBoundingBox(element), boxBounds))
                    {
                        if (!func(element))
                        {
                            return new FOctreeElementId2(currentNodeIndex, i);
                        }
                    }
                }

                if (!node.IsLeaf())
                {
                    FOctreeChildNodeSubset intersectingChildren = nodeContext.GetIntersectingChildren(boxBounds);
                    uint childStartIndex = node.ChildNodes;

                    for (byte i = 0; i < 8; i++)
                    {
                        if (intersectingChildren.Contains(new FOctreeChildNodeRef(i)))
                        {
                            FOctreeElementId2 foundId = FindFirstElementWithBoundsTestInternal(
                                childStartIndex + i,
                                nodeContext.GetChildContext(new FOctreeChildNodeRef(i)),
                                boxBounds,
                                func
                            );

                            if (foundId.IsValidId())
                            {
                                return foundId;
                            }
                        }
                    }
                }
            }

            return new FOctreeElementId2(FOctreeElementId2.INDEX_NONE, FOctreeElementId2.INDEX_NONE_INT);
        }

        public void FindAllElements(Action<T> func)
        {
            foreach (List<T> elements in treeElements)
            {
                foreach (T element in elements)
                {
                    func(element);
                }
            }
        }

        public void FindNearbyElements(Vector3 position, Action<T> func)
        {
            FindNearbyElementsInternal(
                0,
                rootNodeContext,
                new FBoxCenterAndExtent(position, Vector3.zero),
                func
            );
        }

        private void FindNearbyElementsInternal(
            uint currentNodeIndex,
            FOctreeNodeContext nodeContext,
            FBoxCenterAndExtent boxBounds,
            Action<T> func)
        {
            FNode node = treeNodes[(int)currentNodeIndex];

            if (node.InclusiveNumElements > 0)
            {
                // 处理当前节点的所有元素
                foreach (T element in treeElements[(int)currentNodeIndex])
                {
                    func(element);
                }

                if (!node.IsLeaf())
                {
                    FOctreeChildNodeRef childRef = nodeContext.GetContainingChild(boxBounds);

                    if (!childRef.IsNULL())
                    {
                        uint childStartIndex = node.ChildNodes;
                        uint childNodeIndex = childStartIndex + childRef.Index;

                        if (treeNodes[(int)childNodeIndex].InclusiveNumElements > 0)
                        {
                            FindNearbyElementsInternal(
                                childNodeIndex,
                                nodeContext.GetChildContext(childRef),
                                boxBounds,
                                func
                            );
                        }
                        else
                        {
                            // 检查所有子节点
                            for (byte i = 0; i < 8; i++)
                            {
                                FindNearbyElementsInternal(
                                    childStartIndex + i,
                                    nodeContext.GetChildContext(new FOctreeChildNodeRef(i)),
                                    boxBounds,
                                    func
                                );
                            }
                        }
                    }
                }
            }
        }

        public T GetElementById(FOctreeElementId2 elementId)
        {
            if (!elementId.IsValidId())
                return null;

            return treeElements[(int)elementId.GetNodeIndex()][(int)elementId];
        }

        public void Destroy()
        {
            treeNodes.Clear();
            treeElements.Clear();
            freeList.Clear();
            parentLinks.Clear();

            // 重新添加根节点
            treeNodes.Add(new FNode());
            treeElements.Add(new List<T>());
        }

        public void ApplyOffset(Vector3 offset, bool bGlobalOctree = false)
        {
            List<T> tempElementStorage = new List<T>();

            // 收集所有元素
            CollapseNodesInternal(0, tempElementStorage);
            Destroy();

            if (!bGlobalOctree)
            {
                rootNodeContext.Bounds.Center += offset;
            }

            // 偏移并重新添加所有元素
            foreach (T element in tempElementStorage)
            {
                semantics.ApplyOffset(element, offset);
                AddElement(element);
            }
        }

        public FBoxCenterAndExtent GetRootBounds()
        {
            return rootNodeContext.Bounds;
        }

        public int GetNumNodes()
        {
            return treeNodes.Count - (int)freeList.Count * 8;
        }
        
        
        public void ForEachNodeWithDepth(Action<uint, FOctreeNodeContext, int> action)
        {
            ForEachNodeWithDepthInternal(0, rootNodeContext, 0, action);
        }

        private void ForEachNodeWithDepthInternal(uint nodeIndex, FOctreeNodeContext nodeContext, int depth, Action<uint, FOctreeNodeContext, int> action)
        {
            action(nodeIndex, nodeContext, depth);

            FNode node = treeNodes[(int)nodeIndex];
            if (!node.IsLeaf())
            {
                uint childStartIndex = node.ChildNodes;
                for (byte i = 0; i < 8; i++)
                {
                    FOctreeNodeContext childContext = nodeContext.GetChildContext(new FOctreeChildNodeRef(i));
                    ForEachNodeWithDepthInternal(childStartIndex + i, childContext, depth + 1, action);
                }
            }
        }
    }
}