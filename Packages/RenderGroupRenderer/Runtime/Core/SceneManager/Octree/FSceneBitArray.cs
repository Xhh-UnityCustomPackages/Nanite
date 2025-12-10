using System.Collections.Generic;

namespace RenderGroupRenderer
{
    public class FSceneBitArray
    {
        private bool[] m_Bits;
        public void Init(bool s, int length)
        {
            if (m_Bits == null || m_Bits.Length != length)
                m_Bits = new bool[length];
        }

        // Define the indexer to allow client code to use [] notation.
        public bool this[uint i]
        {
            get => m_Bits[i];
            set => m_Bits[i] = value;
        }
    }
}