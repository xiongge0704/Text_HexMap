using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 构建三角面网格
/// 加入高度后，先检测高矮，找到最矮的
/// 然后按照固定顺时针顺序，底-左-右
/// 依次传入构建角落
/// 构建角落时，按照上面同样顺序
/// 分为先悬崖后梯田
/// 或先梯田后悬崖
/// 或纯梯田角落
/// 构建角落样式
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
	List<Vector3> vertices;
	List<Color> colors;
	List<int> triangles;

	MeshCollider meshCollider;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";
		vertices = new List<Vector3>();
		colors = new List<Color>();
		triangles = new List<int>();
	}

    /// <summary>
    /// 通用创建三角面
    /// </summary>
    /// <param name="cells"></param>
	public void Triangulate (HexCell[] cells) {
		hexMesh.Clear();
		vertices.Clear();
		colors.Clear();
		triangles.Clear();

        //按六边形个数创建对应的六边形三角面
		for (int i = 0; i < cells.Length; i++) {
			Triangulate(cells[i]);
		}
		hexMesh.vertices = vertices.ToArray();
		hexMesh.colors = colors.ToArray();
		hexMesh.triangles = triangles.ToArray();
		hexMesh.RecalculateNormals();
		meshCollider.sharedMesh = hexMesh;
	}

    /// <summary>
    /// 创建单个六边形需要的三角面
    /// </summary>
    /// <param name="cell"></param>
	void Triangulate (HexCell cell) {
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			Triangulate(d, cell);
		}
	}

    /// <summary>
    /// 创建六边形的三角面
    /// 包含于邻居相连的中间部分的网格
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
	void Triangulate (HexDirection direction, HexCell cell) {
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
        EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction),center + HexMetrics.GetSecondSolidCorner(direction));
        TriangulateEdgeFan(center,e,cell.color);
        #endregion

        ///固定东北方向为初始方向，按顺时针顺序，选取前三个邻居关联,创建连接处所需的三角面
		if (direction <= HexDirection.SE) {
			TriangulateConnection(direction, cell,e);
		}
	}

    /// <summary>
    /// 六边形连接处的面片
    /// 连接处为矩形面片，定义为桥 - HexMap3
    /// 修改参数，传入六边形的结构体 - HexMap4
    /// </summary>
	void TriangulateConnection (HexDirection direction, HexCell cell, EdgeVertices e1)
    {
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) {
			return;
		}

        //得到桥向量，为当前邻居方向
		Vector3 bridge = HexMetrics.GetBridge(direction);

        #region 新方法，通过结构体计算
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge,e1.v4 + bridge);
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
		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            //与邻居的高度差为坡，作为楼梯型的样子
			TriangulateEdgeTerraces(e1, cell, e2, neighbor);
		}
		else {
            //于邻居间的桥做成斜的平面
            //新增顶点后也要增加对应的关联桥
			// AddQuad(v1, e1, v3, e3);
            // AddQuadColor(cell.color,neighbor.color);
            // AddQuad(e1,e2,e3,e4);
            // AddQuadColor(cell.color,neighbor.color);
            // AddQuad(e2,v2,e4,v4);
			// AddQuadColor(cell.color, neighbor.color);

            TriangulateEdgeStrip(e1,cell.color,e2,neighbor.color);
		}
        //-----------------


        //当前六边形、当前邻居、下一顺位邻居，三个六边形围成的角落--------
		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());

        //选取前两东邻居索引前的邻居围成角落
		if (direction <= HexDirection.E && nextNeighbor != null) {
            //通过下一邻居得到的桥向量，与v2点计算得到角落三角面的最后一个顶点
			Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
            //替换成扰动后的高度
			v5.y = nextNeighbor.Position.y;//nextNeighbor.Elevation * HexMetrics.elevationStep;

            //找到最低点，顺时针排序，即当前-邻居-下一顺位邻居
            //当前六边形比邻居低
			if (cell.Elevation <= neighbor.Elevation) {
                //当前比下一顺位的邻居低，即当前最低
				if (cell.Elevation <= nextNeighbor.Elevation) {
                    //角落三角形，当前顶点为底部，邻居为左边顶点，下一顺位邻居为右边顶点
					TriangulateCorner(e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor);
				}
                //当前比下一顺位的邻居高，即下一顺位最低
				else {
                    //角落三角形，下一顺位邻居为底部顶点，当前为左边顶点，邻居为右边顶点
					TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
				}
			}
            //当前最高，邻居小于等于下一顺位邻居高度，即邻居为最矮
			else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                //角落三角形，邻居为底部顶点，下一顺位邻居为左顶点，当前为右顶点
				TriangulateCorner(e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell);
			}
            //当前最高，邻居大于下一顺位邻居高度，即下一顺位邻居为最矮
			else {
                //下一顺位
				TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
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
	void TriangulateCorner (
		Vector3 bottom, HexCell bottomCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope) {
			if (rightEdgeType == HexEdgeType.Slope) {
                //三角形样式的梯田，一个点在下，两个点在上的梯田样式
				TriangulateCornerTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
			else if (rightEdgeType == HexEdgeType.Flat) {
                //三角形样式的梯田，一个点在上，两个点在下的梯田样式
                TriangulateCornerTerraces(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
			else {
                //梯田悬崖是角落
				TriangulateCornerTerracesCliff(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope) {
			if (leftEdgeType == HexEdgeType.Flat) {
				TriangulateCornerTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else {
                //悬崖梯田是角落
				TriangulateCornerCliffTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			if (leftCell.Elevation < rightCell.Elevation) {
                //悬崖梯田是角落
                TriangulateCornerCliffTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else {
				TriangulateCornerTerracesCliff(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
		}
		else {
            //高度全相同，即一个三角面完成
			AddTriangle(bottom, left, right);
			AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
		}
	}

    /// <summary>
    /// 将于邻居间的桥制作成楼梯型的坡
    /// 修改参数，改用顶点结构作为入参 -HexMap4
    /// </summary>
	void TriangulateEdgeTerraces (
		EdgeVertices begin, HexCell beginCell,
		EdgeVertices end, HexCell endCell
	) {
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
            EdgeVertices e2 = EdgeVertices.TerraceLerp(begin,end,1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.color,endCell.color,1);

            TriangulateEdgeStrip(begin,beginCell.color,e2,c2);

            for(int i = 2;i<HexMetrics.terraceSteps;i++)
            {
                EdgeVertices e1 = e2;
                Color c1 = c2;
                e2 = EdgeVertices.TerraceLerp(begin,end,i);
                c2 = HexMetrics.TerraceLerp(beginCell.color,endCell.color,i);
                TriangulateEdgeStrip(e1,c1,e2,c2);
            }

            TriangulateEdgeStrip(e2,c2,end,endCell.color);
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
	void TriangulateCornerTerraces (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
        //角落梯田第一步数，是个三角面---------
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
		Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

		AddTriangle(begin, v3, v4);
		AddTriangleColor(beginCell.color, c3, c4);
        //----------------------------------

        //中间步数梯田，是矩形面片----------------------
		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(c1, c2, c3, c4);
		}
        //-------------------------------------------

        //最后一步数矩形梯田-------------------------
		AddQuad(v3, v4, left, right);
		AddQuadColor(c3, c4, leftCell.color, rightCell.color);
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
	void TriangulateCornerTerracesCliff (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
        //梯田开始位置，就是梯田在下，悬崖在上
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);

        //取反，就是梯田在上，悬崖载下
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
		Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

		TriangulateBoundaryTriangle(
			begin, beginCell, left, leftCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else {
			AddTriangleUnperturbed(Perturb(left),Perturb(right), boundary);
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
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
	void TriangulateCornerCliffTerraces (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(Perturb(begin),Perturb(left), b);
		Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

		TriangulateBoundaryTriangle(
			right, rightCell, begin, beginCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else {
			AddTriangleUnperturbed(Perturb(left),Perturb(right), boundary);
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

//合围的角落三角面化
	void TriangulateBoundaryTriangle (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 boundary, Color boundaryColor
	) {
		Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

		// AddTriangle(begin, v2, boundary);
        AddTriangleUnperturbed(Perturb(begin),v2,boundary);
		AddTriangleColor(beginCell.color, c2, boundaryColor);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			// AddTriangle(v1, v2, boundary);
            AddTriangleUnperturbed(v1,v2,boundary);
			AddTriangleColor(c1, c2, boundaryColor);
		}

		// AddTriangle(v2, left, boundary);
        AddTriangleUnperturbed(v2,Perturb(left),boundary);
		AddTriangleColor(c2, leftCell.color, boundaryColor);
	}

    ///添加网格构建所需的顶点数据和顶点索引
	void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	void AddTriangleColor (Color color) {
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
	}

	void AddTriangleColor (Color c1, Color c2, Color c3) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
	}

	void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		vertices.Add(Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	void AddQuadColor (Color c1, Color c2) {
		colors.Add(c1);
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c2);
	}

	void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

    ///通过噪音图的数据来扰动坐标点
    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);

        //将扰动范围从0-1更改到-1-1范围
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;

        //去掉Y方向即高度上的扰动，保持细胞的高度一致性，避免高度的扰动导致裂缝的出现
        // position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;

        return position;
    }


    /// <summary>
    /// 拓展后的六边形构建方式
    /// 将原来的两个顶点之间的三角面增加至多个
    /// </summary>
    /// <param name="center"></param>
    /// <param name="edge"></param>
    /// <param name="color"></param>
    void TriangulateEdgeFan(Vector3 center,EdgeVertices edge,Color color)
    {
        AddTriangle(center,edge.v1,edge.v2);
        AddTriangleColor(color);
        AddTriangle(center,edge.v2,edge.v3);
        AddTriangleColor(color);
        AddTriangle(center,edge.v3,edge.v4);
        AddTriangleColor(color);
    }

    /// <summary>
    /// 拓展后的于邻居间桥的构建
    /// </summary>
    /// <param name="e1"></param>
    /// <param name="c1"></param>
    /// <param name="e2"></param>
    /// <param name="c2"></param>
    void TriangulateEdgeStrip(EdgeVertices e1,Color c1,EdgeVertices e2,Color c2)
    {
        AddQuad(e1.v1,e1.v2,e2.v1,e2.v2);
        AddQuadColor(c1,c2);
        AddQuad(e1.v2,e1.v3,e2.v2,e2.v3);
        AddQuadColor(c1,c2);
        AddQuad(e1.v3,e1.v4,e2.v3,e2.v4);
        AddQuadColor(c1,c2);
    }

/// <summary>
///不扰动边界点的三角面创建方法
/// </summary>
/// <param name="v1"></param>
/// <param name="v2"></param>
/// <param name="v3"></param>
    void AddTriangleUnperturbed(Vector3 v1,Vector3 v2,Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
}
