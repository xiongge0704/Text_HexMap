using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

    Color color;
	public Color Color
    {
        get
        {
            return color;
        }
        set
        {
            if(color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
    }

	public RectTransform uiRect;

    public HexGridChunk chunk;

	public int Elevation {
		get {
			return elevation;
		}
		set {
            if(elevation == value)
            {
                return;
            }

			elevation = value;
			Vector3 position = transform.localPosition;
			position.y = value * HexMetrics.elevationStep;
            //对整个高度进行扰动
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;

			transform.localPosition = position;

			Vector3 uiPosition = uiRect.localPosition;
			uiPosition.z =  -position.y;//elevation * -HexMetrics.elevationStep;
			uiRect.localPosition = uiPosition;

            ///改变高度时，要删除对应的异常河流
            ///比如河流只能从高往低流
            ///不满足的要删除
            if(hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation)
            {
                RemoveOutgoingRiver();
            }
            if(hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
            {
                RemoveIncomingRiver();
            }

            Refresh();
		}
	}

	int elevation = int.MinValue;

    bool hasIncomingRiver,hasOutgoingRiver;
    HexDirection incomingRiver,outgoingRiver;

	[SerializeField]
	HexCell[] neighbors;

    /// <summary>
    /// 河床垂直位置
    /// </summary>
    public float StreamBedY
    {
        get
        {
            return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

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

    void Refresh()
    {
        if(chunk)
        {
            chunk.Refresh();
            for(int i = 0;i<neighbors.Length;i++)
            {
                HexCell neighbor = neighbors[i];
                if(neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    public bool HasIncomingRiver
    {
        get{
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver
    {
        get{
            return hasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver
    {
        get{
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver
    {
        get{
            return outgoingRiver;
        }
    }

    public bool HasRiver
    {
        get{
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    /// <summary>
    /// 当前细胞中河道是否有开始和结束
    /// </summary>
    public bool HasRiverBeginOrEnd
    {
        get{
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    /// <summary>
    /// 河流流过边界是否有效
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return hasIncomingRiver && incomingRiver == direction || hasOutgoingRiver && outgoingRiver == direction;
    }

    /// <summary>
    /// 删除细胞中的流出河流
    /// 与之相连的细胞就没有流入的河流了
    /// 所以也要做对应的处理
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if(!hasOutgoingRiver)
        {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// 删除细胞中的流入的河流
    /// 因为没有流入的河流
    /// 所以对应关联的细胞中也没有流出的河流
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if(!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// 移除整体河流
    /// 意味着删除流入和流出的河流
    /// </summary>
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    /// <summary>
    /// 设置流出河流
    /// </summary>
    /// <param name="direction"></param>
    public void SetOutgoingRiver(HexDirection direction)
    {
        ///判断对应方向是否已存在河流，存在就不做处理
        if(hasOutgoingRiver && outgoingRiver == direction)
        {
            return;
        }

        ///判断对应方向的细胞是否高于自身，高过自身也不做处理
        HexCell neighbor = GetNeighbor(direction);
        if(!neighbor || elevation < neighbor.elevation)
        {
            return;
        }

        //删除细胞本身存在的流出河流，并删除对应方向如果存在流入的河流
        RemoveOutgoingRiver();
        if(hasIncomingRiver && incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        ///设置对应方向的流出河流
        hasOutgoingRiver = true;
        outgoingRiver = direction;
        RefreshSelfOnly();

        ///同时设置对应的邻居细胞中的流入河流
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// 只刷新自身单独的块
    /// 不涉及颜色、高度等变化
    /// </summary>
    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }
}
