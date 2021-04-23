using UnityEngine;

///顶点结构体
///用于顶点之间的计算
public struct EdgeVertices
{
    public Vector3 v1,v2,v3,v4;

    /// <summary>
    /// 构造函数，通过差值计算两点之间的多点位置
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    public EdgeVertices(Vector3 corner1,Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1,corner2,1f / 3f);
        v3 = Vector3.Lerp(corner1,corner2,2f / 3f);
        v4 = corner2;
    }

    /// <summary>
    /// 约束在两结构体点之间
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static EdgeVertices TerraceLerp(EdgeVertices a,EdgeVertices b,int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(a.v1,b.v1,step);
        result.v2 = HexMetrics.TerraceLerp(a.v2,b.v2,step);
        result.v3 = HexMetrics.TerraceLerp(a.v3,b.v3,step);
        result.v4 = HexMetrics.TerraceLerp(a.v4,b.v4,step);
        return result;
    }
}
