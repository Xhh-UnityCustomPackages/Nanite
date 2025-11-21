using UnityEngine;

namespace RenderGroupRenderer
{
    /// <summary>
    /// 剔除系统操控类
    /// </summary>
    public interface ICullingable
    {
        public enum CullState
        {
            
        }

        
        Bounds Bounds { get; }
        void FrustumCullShow();//视锥剔除结果为显示
        void FrustumCullHide();//视锥剔除结果为隐藏
    }
}