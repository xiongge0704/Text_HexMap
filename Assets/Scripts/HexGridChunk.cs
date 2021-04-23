using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 网格块，用于大地图
/// </summary>
public class HexGridChunk:MonoBehaviour
{
    HexCell[] cells;

    HexMesh hexMesh;
    Canvas gridCanvas;

    private void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
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
        hexMesh.Triangulate(cells);
        enabled = false;
    }
}
