using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    enum OptionalToggle
    {
        ignore,
        Yes,
        No
    }

	public Color[] colors;

	public HexGrid hexGrid;

	int activeElevation;
    int activeWaterLevel;
    int activeUrbanLevel;

	Color activeColor;

    bool applyColor;

    bool applyElevation = true;
    bool applyWaterLevel = true;
    bool applyUrbanLevel;

    OptionalToggle riverMode,roadMode;

    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

	public void SelectColor (int index) {
        applyColor = index >= 0;
        if(applyColor)
        {
	        activeColor = colors[index];
        }
	}

	public void SetElevation (float elevation) {
		activeElevation = (int)elevation;
	}

	void Awake () {
		SelectColor(0);
	}

	void Update () {
		if (
			Input.GetMouseButton(0) &&
			!EventSystem.current.IsPointerOverGameObject()
		) {
			HandleInput();
		}
        else
        {
            previousCell = null;
        }
	}

	void HandleInput () {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) {
            //缓存当前点击到的细胞，以便下次更新时做处理
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if(previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }
            EditCells(currentCell);
            previousCell = currentCell;
			//EditCells(hexGrid.GetCell(hit.point));
		}
        else
        {
            previousCell = null;
        }
	}

    /// <summary>
    /// 检查当前细胞是否是前一个细胞的邻居
    /// </summary>
    /// <param name="currentCell"></param>
    void ValidateDrag(HexCell currentCell)
    {
        for(dragDirection = HexDirection.NE;dragDirection <= HexDirection.NW;dragDirection++)
        {
            if(previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
    }

    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for(int r = 0,z = centerZ - brushSize;z <= centerZ;z++,r++)
        {
            for(int x = centerX - r;x <= centerX + brushSize;x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x,z)));
            }
        }
        for(int r = 0, z = centerZ + brushSize;z > centerZ;z--,r++)
        {
            for(int x = centerX - brushSize;x <= centerX + r;x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x,z)));
            }
        }
    }

	void EditCell (HexCell cell) {
        if(cell)
        {
            if(applyColor)
            {
                cell.Color = activeColor;
            }

            if(applyElevation)
            {
                cell.Elevation = activeElevation;
            }

            if(applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }

            if(applyUrbanLevel)
            {
                cell.UrbanLevel = activeUrbanLevel;
            }

            if(riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }

            if(roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }

            if(isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if(otherCell)
                {
                    if(riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    
                    if(roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }                
            }
        }
		// hexGrid.Refresh();
	}

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    int brushSize;

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
    }

    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int)level;
    }
}
