﻿using UnityEngine;

public static class HexMetrics {

	public const float outerRadius = 10f;

	public const float innerRadius = outerRadius * 0.866025404f;

	public const float solidFactor = 0.75f;

	public const float blendFactor = 1f - solidFactor;

	public const float elevationStep = 5f;

    /// <summary>
    /// 应该是梯田的个数
    /// </summary>
	public const int terracesPerSlope = 2;

    /// <summary>
    /// 应该是构成梯田的步数，简单的是说，就是2个梯田需要5步来构成，从效果来说就是需要5个矩形面片来组成
    /// </summary>
	public const int terraceSteps = terracesPerSlope * 2 + 1;

    /// <summary>
    /// 水平方向步数大小，应该是俯视看梯田，能看到几个面，2个梯田，能看到5个面
    /// </summary>
	public const float horizontalTerraceStepSize = 1f / terraceSteps;

    /// <summary>
    /// 垂直方向步数大小,应该是面向梯田看，能看到几个面，2个梯田，能看到三个垂直方向的面
    /// </summary>
	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    public static Texture2D noiseSource;

	static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};

	public static Vector3 GetFirstCorner (HexDirection direction) {
		return corners[(int)direction];
	}

	public static Vector3 GetSecondCorner (HexDirection direction) {
		return corners[(int)direction + 1];
	}

    /// <summary>
    /// 通过邻居索引得到对应的向量
    /// 索引值为当前邻居
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}

    /// <summary>
    /// 通过邻居索引得到对应的向量
    /// 索引值为当前邻居+1
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return corners[(int)direction + 1] * solidFactor;
	}

    /// <summary>
    /// 获得桥向量
    /// 通过当前邻居索引向量和下一邻居索引向量相加，再乘混合系数
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public static Vector3 GetBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			blendFactor;
	}

    /// <summary>
    /// 通过差值计算梯田步数对应点的位置
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
	public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
        //(1-t)a+tb = a+t(b-a)
        //通过公式在a和b中间差值取值------------
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
        //-----------------------------------

        //(step+1)/2
        //将1,2,3,4 转换成 1,1,2,2----------
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        //-------------------

		a.y += (b.y - a.y) * v;
		return a;
	}

	public static Color TerraceLerp (Color a, Color b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

    /// <summary>
    /// 计算返回两个高度差的枚举
    /// </summary>
    /// <param name="elevation1"></param>
    /// <param name="elevation2"></param>
    /// <returns></returns>
	public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

    ///通过空间坐标得到噪音图中的4维数据
    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x,position.z);
    }
}
