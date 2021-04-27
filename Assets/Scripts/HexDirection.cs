public enum HexDirection {
	NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions {

    /// <summary>
    /// 相反方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public static HexDirection Opposite (this HexDirection direction) {
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}

    /// <summary>
    /// 输入一个方向得到这个方向前一个方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public static HexDirection Previous (this HexDirection direction) {
		return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
	}

    /// <summary>
    /// 输入一个方向得到这个方向的下一个方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
	public static HexDirection Next (this HexDirection direction) {
		return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
	}

    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}