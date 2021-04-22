using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 网格地图排序
/// </summary>
public class HexGrid : MonoBehaviour {

	public int width = 6;
	public int height = 6;

	public Color defaultColor = Color.white;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	HexCell[] cells;

	Canvas gridCanvas;
	HexMesh hexMesh;

	void Awake () {
		gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();

		cells = new HexCell[height * width];

        //按顺序创建六边行
		for (int z = 0, i = 0; z < height; z++) {
			for (int x = 0; x < width; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void Start () {
        ///创建六边形三角面
		hexMesh.Triangulate(cells);
	}

	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
		return cells[index]; 
	}

	public void Refresh () {
		hexMesh.Triangulate(cells);
	}

    /// <summary>
    /// 创建六边形细胞
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="i"></param>
	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.color = defaultColor;
        cell.name = x.ToString() + "," + z.ToString();

		if (x > 0) {
            //水平顺序，位置大于1的设置西边邻居为前一位六边形
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0) {
            //垂直顺序，位置大于1的位置
			if ((z & 1) == 0) {
                //垂直顺序，偶数位置的，设置东南邻居为前一行长度的六边形
				cell.SetNeighbor(HexDirection.SE, cells[i - width]);
				if (x > 0) {
                    //同时，水平位置大于1的设置西南邻居为前一行长度再加一个单位长度的六边形
					cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
				}
			}
			else {
                //垂直顺序，奇数位置的，设置西南邻居为前一行长度的六边形
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
				if (x < width - 1) {
                    //同时，水平位置大于1的设置东南邻居为前一行长度再减一个单位长度的六边形
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;
	}
}