using UnityEngine;

///边缘顶点结构体
///用于两个点计算出边缘上多个点的分布
///意思是边缘抽象成一条线，线上的两个端点是确定的，通过差值计算，得到将这条线分成若干断
public struct EdgeVertices
{
    public Vector3 v1,v2,v3,v4,v5;

    /// <summary>
    /// 构造函数，通过差值计算两点之间的多点位置
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    public EdgeVertices(Vector3 corner1,Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1,corner2,0.25f);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1,corner2,0.75f);
        v5 = corner2;
    }

    /// <summary>
    /// 通过参数来划分两点之间的线
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    /// <param name="outerStep"></param>
    public EdgeVertices(Vector3 corner1,Vector3 corner2,float outerStep)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        v5 = corner2;
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
        result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
        result.v4 = HexMetrics.TerraceLerp(a.v4,b.v4,step);
        result.v5 = HexMetrics.TerraceLerp(a.v5,b.v5,step);
        return result;
    }
}
