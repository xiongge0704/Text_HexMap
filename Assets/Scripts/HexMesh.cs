﻿using UnityEngine;
using System.Collections.Generic;
using System;

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
/// HexMap6-6后，移除了三角面测量的相关方法，放到了HexGridChunk中，此类改为专门绘制三角面，不做测量工作
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
    //static List<Vector3> vertices = new List<Vector3>();
    //static List<Color> colors = new List<Color>();
    //static List<int> triangles = new List<int>();
    [NonSerialized]
    List<Vector3> vertices;
    [NonSerialized]
    List<Color> colors;
    [NonSerialized]
    List<int> triangles;

	MeshCollider meshCollider;

    public bool useCollider,useColors,useUVCoordinates;

    [NonSerialized]
    List<Vector2> uvs;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        if (useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
		hexMesh.name = "Hex Mesh";
		// vertices = new List<Vector3>();
		// colors = new List<Color>();
		// triangles = new List<int>();
	}

    ///添加网格构建所需的顶点数据和顶点索引
	public void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(HexMetrics.Perturb(v1));
		vertices.Add(HexMetrics.Perturb(v2));
		vertices.Add(HexMetrics.Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	public void AddTriangleColor (Color color) {
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
	}

	public void AddTriangleColor (Color c1, Color c2, Color c3) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
	}

	public void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = vertices.Count;
		vertices.Add(HexMetrics.Perturb(v1));
		vertices.Add(HexMetrics.Perturb(v2));
		vertices.Add(HexMetrics.Perturb(v3));
		vertices.Add(HexMetrics.Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	public void AddQuadColor (Color c1, Color c2) {
		colors.Add(c1);
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c2);
	}

    public void AddQuadColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }


    public void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

    /// <summary>
    ///不扰动边界点的三角面创建方法
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddTriangleUnperturbed(Vector3 v1,Vector3 v2,Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUV(Vector2 uv1,Vector2 uv2,Vector2 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1,Vector2 uv2,Vector2 uv3,Vector2 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    public void AddQuadUV(float uMin,float uMax,float vMin,float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }

    public void Clear()
    {
        hexMesh.Clear();
        //vertices.Clear();
        //colors.Clear();
        //triangles.Clear();
        vertices = ListPool<Vector3>.Get();
        if (useColors)
        {
            colors = ListPool<Color>.Get();
        }
        if(useUVCoordinates)
        {
            uvs = ListPool<Vector2>.Get();
        }
        triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        hexMesh.SetColors(colors);
        ListPool<Color>.Add(colors);

        if (useColors)
        {
            hexMesh.SetTriangles(triangles, 0);
            ListPool<int>.Add(triangles);
        }

        if(useUVCoordinates)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }

        hexMesh.RecalculateNormals();

        if (useCollider)
        {
            meshCollider.sharedMesh = hexMesh;
        }
    }
}
