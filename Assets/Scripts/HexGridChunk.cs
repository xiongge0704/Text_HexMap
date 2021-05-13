using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 网格块，用于大地图
/// HexMap6-6后，将HexMesh中原本测量三角面的相关方法移到此类，此类用于专门地形的测量
/// </summary>
public class HexGridChunk:MonoBehaviour
{
    HexCell[] cells;

    //HexMesh hexMesh;
    public HexMesh terrain,rivers,roads,water,waterShore,estuaries;
    Canvas gridCanvas;

    public HexFeatureManager features;

    private void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>();
        //hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];

        ShowUI(false);
    }

    // private void Start() {
    //     hexMesh.Triangulate(cells);
    // }

    public void AddCell(int index,HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform,false);
        cell.uiRect.SetParent(gridCanvas.transform,false);
    }

    /// <summary>
    /// 更新块
    /// </summary>
    public void Refresh()
    {
        // hexMesh.Triangulate(cells);
        enabled = true;
    }

    private void LateUpdate() {
        Triangulate();
        //hexMesh.Triangulate(cells);
        enabled = false;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 通用创建三角面
    /// </summary>
    /// <param name="cells"></param>
	public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();

        //按六边形个数创建对应的六边形三角面
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    /// <summary>
    /// 创建单个六边形需要的三角面
    /// </summary>
    /// <param name="cell"></param>
	void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }

        if (!cell.IsUnderwater && !cell.HasRiver && !cell.HasRoads)
        {
            features.AddFeature(cell,cell.Position);
        }
    }

    /// <summary>
    /// 创建六边形的三角面
    /// 包含于邻居相连的中间部分的网格
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
	void Triangulate(HexDirection direction, HexCell cell)
    {
        //得到扰动后的坐标
        Vector3 center = cell.Position;//cell.transform.localPosition;

        #region 原始的六边形构建方式
        // //通过第一个邻居索引，得到三角形其中一个顶点位置
        // Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        // //通过第二个邻居索引，得到三角形其中一个顶点位置
        // Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        // //通过六边形两相邻的顶点中间加点，实现六边形有更多的三角面，能够实现更多的变化
        // Vector3 e1 = Vector3.Lerp(v1,v2,1f / 3f);
        // Vector3 e2 = Vector3.Lerp(v1,v2,2f / 3f);

        // //添加六边形的三角面数据，用于统一创建三角面
        // AddTriangle(center, v1, e1);
        // AddTriangleColor(cell.color);

        // //新增加的六边形三角面
        // AddTriangle(center,e1,e2);
        // AddTriangleColor(cell.color);
        // AddTriangle(center,e2,v2);
        // AddTriangleColor(cell.color);
        #endregion

        #region HexMap4中新的六边形构建方式
        EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction), center + HexMetrics.GetSecondSolidCorner(direction));

        //区分细胞中间是否有河道，并用对应的方法绘制
        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            //TriangulateEdgeFan(center, e, cell.Color);
            TriangulateWithoutRiver(direction, cell, center, e);

            if(!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
            {
                features.AddFeature(cell,(center + e.v1 + e.v5) * (1f / 3f));
            }
        }


        #endregion

        ///固定东北方向为初始方向，按顺时针顺序，选取前三个邻居关联,创建连接处所需的三角面
        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }

        if(cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
        }
    }

    /// <summary>
    /// 细胞中的水
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    void TriangulateWater(HexDirection direction,HexCell cell,Vector3 center)
    {
        center.y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if(neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    /// <summary>
    /// 开放性水域，即相邻细胞中有水，细胞中的水连城一片
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="neighbor"></param>
    /// <param name="center"></param>
    void TriangulateOpenWater(HexDirection direction,HexCell cell,HexCell neighbor,Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        water.AddTriangle(center, c1, c2);

        if (direction <= HexDirection.SE && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                {
                    return;
                }
                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
            }
        }
    }

    /// <summary>
    /// 封闭水域，即相邻细胞中没有水，连接细胞边，如沙滩一样
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="neighbor"></param>
    /// <param name="center"></param>
    void TriangulateWaterShore(HexDirection direction,HexCell cell,HexCell neighbor,Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(center + HexMetrics.GetFirstWaterCorner(direction), center + HexMetrics.GetSecondWaterCorner(direction));
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        //Vector3 bridge = HexMetrics.GetWaterBridge(direction);
        //EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);
        Vector3 center2 = neighbor.Position;
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()), center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite()));

        if (cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2,cell.IncomingRiver == direction);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if(nextNeighbor != null)
        {
            //Vector3 center3 = nextNeighbor.Position;
            //center3.y = center.y;
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ? HexMetrics.GetFirstWaterCorner(direction.Previous()) : HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.y = center.y;
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            //waterShore.AddTriangle(e1.v5, e2.v5, e1.v5 + HexMetrics.GetWaterBridge(direction.Next()));
            waterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
        }
    }

    void TriangulateEstuary(EdgeVertices e1,EdgeVertices e2,bool incomingRiver)
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        //estuaries.AddQuadUV(0f, 0f, 0f, 1f);

        if (incomingRiver)
        {
            estuaries.AddQuadUV2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            estuaries.AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
            estuaries.AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            estuaries.AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            estuaries.AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
        }
    }

    void TriangulateWaterfallInWater(Vector3 v1,Vector3 v2,Vector3 v3,Vector3 v4,float y1,float y2,float waterY)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }

    /// <summary>
    /// 细胞中没有河流的时候
    /// 因为是特指没有河流的情况
    /// 在河流和道路共存的时候
    /// 将无法在细胞中绘制道路
    /// 所以需要在另外的函数中处理这种情况
    /// 即TriangulateAdjacentToRiver中
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithoutRiver(HexDirection direction,HexCell cell,Vector3 center,EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cell.Color);

        if(cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(center, Vector3.Lerp(center, e.v1, interpolators.x), Vector3.Lerp(center, e.v5, interpolators.y), e,cell.HasRoadThroughEdge(direction));
        }
    }

    /// <summary>
    /// 细胞中心河流河道的绘制方法
    /// 有流入和有流出的情况
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;

        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            //通过前一个方向得到中心点往左偏移的位置
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            //通过下一个方向得到中心点往右偏移的位置
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            //同一个细胞中，如果当前方向相邻1个单位方向上有河道，那么相连处河道拓宽，让河流能通过
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            //同一个细胞中，如果当前方向相邻1个单位方向上有河道，那么相连处河道拓宽，让河流能通过
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            //同一个细胞中，如果当前方向相邻2个单位方向上有河道
            centerL = center;
            centerR = center + HexMetrics.GetSoliEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else
        {
            //同一个细胞中，如果当前方向相邻2个单位方向上有河道
            centerL = center + HexMetrics.GetSoliEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }

        center = Vector3.Lerp(centerL, centerR, 0.5f);

        //得到偏移后的左右两点与细胞的边界顶点做差值，得到中间位置的点
        EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
        //将细胞顶面中的河道中心线的高度设为一样
        m.v3.y = center.y = e.v3.y;

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.Color);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    /// <summary>
    /// 河道开始和结束的绘制
    /// 只有流入或流出的情况
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));
        m.v3.y = e.v3.y;
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        //开始或结束的河流绘制-----------
        if (!cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);

            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else
            {
                rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0.6f, 1f), new Vector2(1f, 0.6f));
            }
        }
        //----------------------------
    }

    /// <summary>
    /// 填充河道细胞中其他没有河道的顶面部分
    /// 在此函数中处理河流和道路共存的情况
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if(cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }

        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                //当前方向的前一个方向和后一个方向上都有河道
                //就是河道的流入和流出之间相差一个方向单位
                center += HexMetrics.GetSoliEdgeMiddle(direction) * (HexMetrics.innerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                //当前方向的前一个方向和后两个方向上都有河道
                //就是笔直的河道，这是靠近前一个方向上的
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            //当前方向的后一个方向和前两个方向上都有河道
            //就是笔直的河道，这是靠近后一个方向上的
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        if(!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            features.AddFeature(cell,(center + e.v1 + e.v5) * (1f / 3f));
        }
    }

    /// <summary>
    /// 道路河流共存的时候
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        if(cell.HasRiverBeginOrEnd)
        {
            //细胞中有河流的开始或结束
            //中心点道路往河流的相反方向偏移1/3位置
            roadCenter += HexMetrics.GetSoliEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if(cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            //直线河流经过细胞中心
            //将细胞中心的道路截成两段
            Vector3 corner;
            if(previousHasRiver)
            {
                if(!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                {
                    //河流截断道路后，如果一段没有连接的道路，将返回不在绘制
                    //即此中心如果是道路开头或结尾
                    //河流截断后一遍将不绘制对应面片
                    return;
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                if(!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                {
                    //河流截断道路后，如果一段没有连接的道路，将返回不在绘制
                    //即此中心如果是道路开头或结尾
                    //河流截断后一遍将不绘制对应面片
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            center += corner * 0.25f;
        }
        else if(cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            //之字型河流，即出和入河流是相邻的情况
            //道路在河流汇合处偏移
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        else if(cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            //之字型河流，即出和入河流是相邻的情况
            //道路在河流汇合处偏移
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        else if(previousHasRiver && nextHasRiver)
        {
            //河流出入间隔一个单位的方向，即呈现弧形的河流，弧形内部的情况
            if(!hasRoadThroughEdge)
            {
                //头尾部分，截断后多余部分返回不绘制
                return;
            }
            Vector3 offset = HexMetrics.GetSoliEdgeMiddle(direction) * HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            //剩下的情况，即弧形河流的弧形外部情况
            HexDirection middle;
            if(previousHasRiver)
            {
                middle = direction.Next();
            }
            else if(nextHasRiver)
            {
                middle = direction.Previous();
            }
            else
            {
                middle = direction;
            }

            if(!cell.HasRoadThroughEdge(middle) && !cell.HasRoadThroughEdge(middle.Previous()) && !cell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }

            roadCenter += HexMetrics.GetSoliEdgeMiddle(middle) * 0.25f;
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        //填充中心位置道路偏移后的空白位置
        if(previousHasRiver)
        {
            //当前方向的前一位方向存在河流时
            //在道路中心、单元格中心和中间左点之间添加一个三角形
            TriangulateRoadEdge(roadCenter, center, mL);
            //Debug.Log("Direction:" + direction);
        }
        if(nextHasRiver)
        {
            //当前方向的下一位方向存在河流时
            //道路中心，中间的右点，和单元格中心之间添加一个三角形
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    /// <summary>
    /// 六边形连接处的面片
    /// 连接处为矩形面片，定义为桥 - HexMap3
    /// 修改参数，传入六边形的结构体 - HexMap4
    /// </summary>
	void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        //得到桥向量，为当前邻居方向
        Vector3 bridge = HexMetrics.GetBridge(direction);

        #region 新方法，通过结构体计算
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighbor.StreamBedY;

            if (!cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f, cell.HasIncomingRiver && cell.IncomingRiver == direction);
                }
                else if(cell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY);
                }
            }
            else if(!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel)
            {
                TriangulateWaterfallInWater(e2.v4, e2.v2, e1.v4, e1.v2, neighbor.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
            }
        }
        #endregion

        #region 原始方法
        // //六边形的当前邻居边的两顶点加上桥向量，得到对应矩形桥的四个顶点
        // Vector3 v3 = v1 + bridge;
        // Vector3 v4 = v2 + bridge;

        // //设置高度,替换成扰动后的
        // v3.y = v4.y = neighbor.Position.y;//neighbor.Elevation * HexMetrics.elevationStep;

        // //新增顶点后，对应的邻居之间的桥也要有对应的顶点做关联
        // Vector3 e3 = Vector3.Lerp(v3,v4,1f / 3f);
        // Vector3 e4 = Vector3.Lerp(v3,v4,2f / 3f);
        #endregion

        //与邻居间的桥--------
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            //与邻居的高度差为坡，作为楼梯型的样子
            TriangulateEdgeTerraces(e1, cell, e2, neighbor,cell.HasRoadThroughEdge(direction));
        }
        else
        {
            //于邻居间的桥做成斜的平面
            //新增顶点后也要增加对应的关联桥
            // AddQuad(v1, e1, v3, e3);
            // AddQuadColor(cell.color,neighbor.color);
            // AddQuad(e1,e2,e3,e4);
            // AddQuadColor(cell.color,neighbor.color);
            // AddQuad(e2,v2,e4,v4);
            // AddQuadColor(cell.color, neighbor.color);

            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color,cell.HasRoadThroughEdge(direction));
        }
        //-----------------


        //当前六边形、当前邻居、下一顺位邻居，三个六边形围成的角落--------
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());

        //选取前两东邻居索引前的邻居围成角落
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            //通过下一邻居得到的桥向量，与v2点计算得到角落三角面的最后一个顶点
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            //替换成扰动后的高度
            v5.y = nextNeighbor.Position.y;//nextNeighbor.Elevation * HexMetrics.elevationStep;

            //找到最低点，顺时针排序，即当前-邻居-下一顺位邻居
            //当前六边形比邻居低
            if (cell.Elevation <= neighbor.Elevation)
            {
                //当前比下一顺位的邻居低，即当前最低
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    //角落三角形，当前顶点为底部，邻居为左边顶点，下一顺位邻居为右边顶点
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                //当前比下一顺位的邻居高，即下一顺位最低
                else
                {
                    //角落三角形，下一顺位邻居为底部顶点，当前为左边顶点，邻居为右边顶点
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            //当前最高，邻居小于等于下一顺位邻居高度，即邻居为最矮
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                //角落三角形，邻居为底部顶点，下一顺位邻居为左顶点，当前为右顶点
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            //当前最高，邻居大于下一顺位邻居高度，即下一顺位邻居为最矮
            else
            {
                //下一顺位
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }
        //------------------------------------------------------
    }

    /// <summary>
    /// 角落三角面
    /// 固定顺时针顺序，即底-左-右
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
	void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                //三角形样式的梯田，一个点在下，两个点在上的梯田样式
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                //三角形样式的梯田，一个点在上，两个点在下的梯田样式
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
            else
            {
                //梯田悬崖是角落
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                //悬崖梯田是角落
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                //悬崖梯田是角落
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        }
        else
        {
            //高度全相同，即一个三角面完成
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    /// <summary>
    /// 将于邻居间的桥制作成楼梯型的坡
    /// 修改参数，改用顶点结构作为入参 -HexMap4
    /// </summary>
	void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad
    )
    {
        #region 原始方法
        // Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        // Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        // Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        // AddQuad(beginLeft, beginRight, v3, v4);
        // AddQuadColor(beginCell.color, c2);

        // for (int i = 2; i < HexMetrics.terraceSteps; i++) {
        // 	Vector3 v1 = v3;
        // 	Vector3 v2 = v4;
        // 	Color c1 = c2;
        // 	v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
        // 	v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
        // 	c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
        // 	AddQuad(v1, v2, v3, v4);
        // 	AddQuadColor(c1, c2);
        // }

        // AddQuad(v3, v4, endLeft, endRight);
        // AddQuadColor(c2, endCell.color);
        #endregion

        #region 新方式
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2,hasRoad);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2,hasRoad);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color,hasRoad);
        #endregion
    }

    /// <summary>
    /// 制作角落的梯田坡
    /// 三个六边形组成的角落，只有一种高度变化，形成的梯田型的角落
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
	void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        //角落梯田第一步数，是个三角面---------
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);
        //----------------------------------

        //中间步数梯田，是矩形面片----------------------
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }
        //-------------------------------------------

        //最后一步数矩形梯田-------------------------
        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
        //-------------------------------------
    }

    /// <summary>
    /// 先梯田
    /// 后悬崖
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
	void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        //梯田开始位置，就是梯田在下，悬崖在上
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);

        //取反，就是梯田在上，悬崖载下
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    /// <summary>
    /// 先悬崖
    /// 后梯田
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
	void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    //合围的角落三角面化
    void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        // AddTriangle(begin, v2, boundary);
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            // AddTriangle(v1, v2, boundary);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        // AddTriangle(v2, left, boundary);
        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    /// <summary>
    /// 拓展后的六边形构建方式
    /// 将原来的两个顶点之间的三角面增加至多个
    /// </summary>
    /// <param name="center"></param>
    /// <param name="edge"></param>
    /// <param name="color"></param>
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    /// <summary>
    /// 拓展后的于邻居间桥的构建
    /// </summary>
    /// <param name="e1"></param>
    /// <param name="c1"></param>
    /// <param name="e2"></param>
    /// <param name="c2"></param>
    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2,bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);


        //平面的道路
        if(hasRoad)
        {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
        }
    }

    /// <summary>
    /// 河流绘制
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="y"></param>
    void TriangulateRiverQuad(Vector3 v1,Vector3 v2,Vector3 v3,Vector3 v4,float y1,float y2,float v, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);

        //河流方向
        if(reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            rivers.AddQuadUV(0f, 1f, v, v+0.2f);
        }
        
    }

    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y,v, reversed);
    }

    /// <summary>
    /// 细胞与邻居间的连接处道路绘制
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="v5"></param>
    /// <param name="v6"></param>
    void TriangulateRoadSegment(Vector3 v1,Vector3 v2,Vector3 v3,Vector3 v4,Vector3 v5,Vector3 v6)
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    /// <summary>
    /// 细胞中间部分的道路绘制
    /// </summary>
    /// <param name="center"></param>
    /// <param name="mL"></param>
    /// <param name="mR"></param>
    /// <param name="e"></param>
    void TriangulateRoad(Vector3 center,Vector3 mL,Vector3 mR,EdgeVertices e,bool hasRoadThroughCellEdge)
    {
        if(hasRoadThroughCellEdge)
        {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    /// <summary>
    /// 道路的边缘
    /// 即道路的末端
    /// </summary>
    /// <param name="center"></param>
    /// <param name="mL"></param>
    /// <param name="mR"></param>
    void TriangulateRoadEdge(Vector3 center,Vector3 mL,Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }

    /// <summary>
    /// 平滑道路末端
    /// 即细胞中心位置道路绘制
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
    Vector2 GetRoadInterpolators(HexDirection direction,HexCell cell)
    {
        Vector2 interpolators;

        if(cell.HasRoadThroughEdge(direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            //对应方向上没有道路时
            //缩小中心位置的绘制范围，达到整体道路更加的平滑好看
            interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }    
}
