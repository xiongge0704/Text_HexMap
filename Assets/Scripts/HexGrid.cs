using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 网格地图排序
/// </summary>
public class HexGrid : MonoBehaviour {

	int cellCountX;
	int cellCountZ;

    public int chunkCountX = 4,chunkCountZ = 3;

	public Color defaultColor = Color.white;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

    public HexGridChunk chunkPrefab;

	HexCell[] cells;

	// Canvas gridCanvas;
	// HexMesh hexMesh;

    public Texture2D noiseSource;

    HexGridChunk[] chunks;

    public int seed;

	void Awake () {

        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);

		// gridCanvas = GetComponentInChildren<Canvas>();
		// hexMesh = GetComponentInChildren<HexMesh>();

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
	}

    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        //按顺序创建六边行
		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for(int z = 0,i = 0;z < chunkCountZ;z++)
        {
            for(int x = 0;x<chunkCountX;x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void OnEnable() {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

	// void Start () {
    //     ///创建六边形三角面
	// 	hexMesh.Triangulate(cells);
	// }

	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
		return cells[index];
	}

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if(z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if(x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }

	// public void Refresh () {
	// 	hexMesh.Triangulate(cells);
	// }

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
		// cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Color = defaultColor;
        cell.name = x.ToString() + "," + z.ToString();

		if (x > 0) {
            //水平顺序，位置大于1的设置西边邻居为前一位六边形
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0) {
            //垂直顺序，位置大于1的位置
			if ((z & 1) == 0) {
                //垂直顺序，偶数位置的，设置东南邻居为前一行长度的六边形
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
                    //同时，水平位置大于1的设置西南邻居为前一行长度再加一个单位长度的六边形
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
			}
			else {
                //垂直顺序，奇数位置的，设置西南邻居为前一行长度的六边形
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
                    //同时，水平位置大于1的设置东南邻居为前一行长度再减一个单位长度的六边形
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
		// label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;

        //立即设置高度，触发高度的扰动
        cell.Elevation = 0;

        AddCellToChunk(x,z,cell);
	}

    void AddCellToChunk(int x,int z,HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX,cell);
    }

    public void ShowUI(bool visible)
    {
        for(int i = 0;i < chunks.Length;i++)
        {
            chunks[i].ShowUI(visible);
        }
    }
}
