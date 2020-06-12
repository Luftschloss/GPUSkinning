#if UNITY_ANDROID && !UNITY_EDITOR
#define ANDROID
#endif


#if UNITY_IPHONE && !UNITY_EDITOR
#define IPHONE
#endif

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 移动端相机控制器
/// </summary>
public class CameraCtr : MonoBehaviour
{
    #region PublicMember公有成员
    // 主角  
    public Transform target;
    #endregion

    #region PrivateMember 私有成员
    // 缩放系数  
    private float distance = 10f;

    // 左右滑动移动速度  
    private float xSpeed = 90.0f;
    private float ySpeed = 40.0f;

    // 缩放限制系数  
    private float yMinLimit = -10;
    private float yMaxLimit = 60;

    // 摄像头的位置  
    private float x = 0.0f;
    private float y = 1.5f;

    // 记录上一次手机触摸位置判断用户是在做放大还是缩小手势  
    private Vector2 oldPosition1 = new Vector2(0, 0);
    private Vector2 oldPosition2 = new Vector2(0, 0);

    #endregion

    #region MonoBehaviour脚本生命周期函数

    #region Start
    //初始化游戏信息设置  
    void Start()
    {
        //Debug.Log(oldPosition1);
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }
    #endregion

    #region Update
    private void Update()
    {
#if IPHONE || ANDROID
            if (EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
#else
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
#endif
        {
        }
        else
        {
            if (Input.touchCount == 1)
            {
                // 触摸类型为移动触摸  
                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    //根据触摸点计算X与Y位置  
                    x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                }
            }
            // 判断触摸数量为多点触摸  
            if (Input.touchCount > 1)
            {
                // 前两只手指触摸类型都为移动触摸  
                if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
                {
                    // 计算出当前两点触摸点的位置  
                    var tempPosition1 = Input.GetTouch(0).position;
                    var tempPosition2 = Input.GetTouch(1).position;
                    // 函数返回真为放大，返回假为缩小  
                    if (isEnlarge(oldPosition1, oldPosition2, tempPosition1, tempPosition2))
                    {
                        // 放大系数超过2以后不允许继续放大  
                        if (distance > 2)
                        {
                            distance -= 5f * Time.deltaTime;
                        }
                    }
                    else
                    {
                        // 缩小系数返回7.5后不允许继续缩小   
                        if (distance < 7.5f)
                        {
                            distance += 5f * Time.deltaTime;
                        }
                    }
                    // 备份上一次触摸点的位置，用于对比  
                    oldPosition1 = tempPosition1;
                    oldPosition2 = tempPosition2;

                }
            }

        }


    }
    #endregion

    #region LateUpdate
    // Update方法一旦调用结束以后进入这里算出重置摄像机的位置  
    void LateUpdate()
    {
        // target为主角，缩放旋转的参照物  
        if (target)
        {
            // 重置摄像机的位置  
            y = ClampAngle(y, yMinLimit, yMaxLimit);
            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 position = rotation * new Vector3(0.0f, 1.5f, -distance) + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }
    #endregion

    #endregion

    #region PrivateMethod 私有方法

    #region 限制相机的角度 ClampAngle
    /// <summary>
        /// 限制相机的角度
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    #endregion

    #region 判断放大缩小
    // 函数返回真为放大，返回假为缩小  
    private bool isEnlarge(Vector2 oP1, Vector2 oP2, Vector2 nP1, Vector2 nP2)
    {
        // 函数传入上一次触摸两点的位置与本次触摸两点的位置计算出用户的手势  
        float leng1 = Mathf.Sqrt((oP1.x - oP2.x) * (oP1.x - oP2.x) + (oP1.y - oP2.y) * (oP1.y - oP2.y));
        float leng2 = Mathf.Sqrt((nP1.x - nP2.x) * (nP1.x - nP2.x) + (nP1.y - nP2.y) * (nP1.y - nP2.y));

        if (leng1 < leng2)
        {
            // 放大手势  
            return true;
        }
        else
        {
            // 缩小手势  
            return false;
        }
    }
    #endregion

    #endregion
}