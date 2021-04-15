public enum HexEdgeType {
    /// <summary>
    /// 平
    /// 两个相连六边形的高度一样
    /// </summary>
	Flat,
    /// <summary>
    /// 坡
    /// 两个相连六边形的高度相差1个单位高度
    /// </summary>
    Slope,
    /// <summary>
    /// 悬崖
    /// 两个相连六边形的高度相差大于1个单位的高度
    /// </summary>
    Cliff
}