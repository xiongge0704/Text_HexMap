using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public Color color;

	public RectTransform uiRect;

	public int Elevation {
		get {
			return elevation;
		}
		set {
			elevation = value;
			Vector3 position = transform.localPosition;
			position.y = value * HexMetrics.elevationStep;
            //对整个高度进行扰动
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;

			transform.localPosition = position;

			Vector3 uiPosition = uiRect.localPosition;
			uiPosition.z =  -position.y;//elevation * -HexMetrics.elevationStep;
			uiRect.localPosition = uiPosition;
		}
	}

	int elevation;

	[SerializeField]
	HexCell[] neighbors;

    /// <summary>
    /// 通过邻居索引值获得对应邻居的对象
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public HexCell GetNeighbor (HexDirection direction) {
		return neighbors[(int)direction];
	}

	public void SetNeighbor (HexDirection direction, HexCell cell) {
		neighbors[(int)direction] = cell;
		cell.neighbors[(int)direction.Opposite()] = this;
	}

    /// <summary>
    /// 获得与指定邻居的高度差枚举
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public HexEdgeType GetEdgeType (HexDirection direction) {
		return HexMetrics.GetEdgeType(
			elevation, neighbors[(int)direction].elevation
		);
	}

	public HexEdgeType GetEdgeType (HexCell otherCell) {
		return HexMetrics.GetEdgeType(
			elevation, otherCell.elevation
		);
	}

    public Vector3 Position
    {
        get{
            return transform.localPosition;
        }
    }
}
