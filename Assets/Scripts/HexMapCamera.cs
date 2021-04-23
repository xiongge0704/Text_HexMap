using UnityEngine;

/// <summary>
/// 摄像机控制
/// </summary>
public class HexMapCamera:MonoBehaviour
{
    Transform swivel,stick;

    float zoom = 1f;

    public float stickMinZoom,stickMaxZoom;

    public float swivelMinZoom,swivelMaxZoom;

    // public float moveSpeed;
    public float moveSpeedMinZoom,moveSpeedMaxZoom;

    public float rotationSpeed;

    public HexGrid grid;

    private void Awake() {
        swivel = this.transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    private void Update() {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if(rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if(xDelta != 0f || zDelta != 0f)
        {
            adjustPosition(xDelta,zDelta);
        }
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom,stickMaxZoom,zoom);
        stick.localPosition = new Vector3(0f,0f,distance);

        float angle = Mathf.Lerp(swivelMinZoom,swivelMaxZoom,zoom);
        swivel.localRotation = Quaternion.Euler(angle,0f,0f);
    }

    void adjustPosition(float xDelta,float zDelta)
    {
        //这个方向向量归一化后，大小是一样的，所以会导致松开按键后还会有移动效果
        Vector3 direction = transform.localRotation * new Vector3(xDelta,0f,zDelta).normalized;
        ///设置阻尼来抵消松开按键时还会移动的效果
        float damping = Mathf.Max(Mathf.Abs(xDelta),Mathf.Abs(zDelta));
        float distance = Mathf.Lerp(moveSpeedMinZoom,moveSpeedMaxZoom,zoom) * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.chunkCountX * HexMetrics.chunkSizeX - 0.5f) * (2f * HexMetrics.innerRadius);
        position.x = Mathf.Clamp(position.x,0f,xMax);

        float zMax = (grid.chunkCountZ * HexMetrics.chunkSizeZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z,0f,zMax);

        return position;
    }

    float rotationAngle;
    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if(rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if(rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }
        transform.localRotation = Quaternion.Euler(0f,rotationAngle,0f);
    }
}
