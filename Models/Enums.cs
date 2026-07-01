using System;

namespace FileDeduper.Models
{
    /// <summary>
    /// 智能标记时保留哪个副本作为原件的策略。
    /// </summary>
    public enum KeepStrategy
    {
        /// <summary>保留修改时间最旧的副本。</summary>
        Oldest = 0,

        /// <summary>保留修改时间最新的副本。</summary>
        Newest = 1,

        /// <summary>保留路径最短（最靠近根）的副本。</summary>
        ShortestPath = 2
    }

    /// <summary>
    /// 删除被标记文件时采用的方式。
    /// </summary>
    public enum DeleteMode
    {
        /// <summary>移入回收站，可恢复。</summary>
        Recycle = 0,

        /// <summary>从磁盘永久删除，不可恢复。</summary>
        Permanent = 1
    }

    /// <summary>
    /// 哈希计算加速模式。GPU 只作为可选能力，不能影响基础功能。
    /// </summary>
    public enum HardwareAccelerationMode
    {
        /// <summary>自动选择可用且安全的实现。</summary>
        Auto = 0,

        /// <summary>只使用 CPU 流式全量哈希。</summary>
        CpuOnly = 1,

        /// <summary>实验性 GPU 模式；没有可用 provider 时回退 CPU。</summary>
        GpuExperimental = 2
    }

    /// <summary>
    /// 单个文件在查重中的置信度等级。
    /// </summary>
    public enum DuplicateConfidence
    {
        /// <summary>尚未参与查重。</summary>
        None = 0,

        /// <summary>仅大小相等，名称/时间不同，疑似改名副本。</summary>
        Suspected = 1,

        /// <summary>大小+名称+时间三项全等，高置信度重复。</summary>
        Likely = 2,

        /// <summary>已通过 MD5 哈希精确确认的重复。</summary>
        Verified = 3
    }
}
