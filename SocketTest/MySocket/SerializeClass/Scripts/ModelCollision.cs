using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ModelCollision
{
    public static bool IsCollider(ModelInfo info1, ModelInfo info2)
    {
        bool result = false;
        switch (info1.type)
        {
            case Model3DType.Ball://球和球碰撞
                result = BallVsThing(info1, info2);
                break;
            case Model3DType.Squareness://球和矩形
                result = SquarenessVsThing(info1, info2);
                break;
            case Model3DType.Cylinder://球和圆柱体
                result = CylinderVsThing(info1, info2);
                break;
            default:
                break;
        }
        return result;
    }


    private static bool BallVsThing(ModelInfo ball, ModelInfo thing)
    {
        bool result = false;
        switch (thing.type)
        {
            case Model3DType.Ball://球和球碰撞

                break;
            case Model3DType.Squareness://球和矩形---->和矩形角判断

                break;
            case Model3DType.Cylinder://球和圆柱体

                break;
            default:
                break;
        }
        return result;
    }


    private static bool SquarenessVsThing(ModelInfo squa, ModelInfo thing)
    {
        bool result = false;
        switch (thing.type)
        {
            case Model3DType.Ball://矩形和球
                result = BallVsThing(thing, squa);
                break;
            case Model3DType.Squareness://矩形和矩形

                break;
            case Model3DType.Cylinder://矩形和圆柱体

                break;
            default:
                break;
        }
        return result;
    }

    private static bool CylinderVsThing(ModelInfo cylinder, ModelInfo thing)
    {
        bool result = false;
        switch (thing.type)
        {
            case Model3DType.Ball://圆柱体和球
                result = BallVsThing(thing, cylinder);
                break;
            case Model3DType.Squareness://圆柱体和矩形

                break;
            case Model3DType.Cylinder://圆柱体和圆柱体

                break;
            default:
                break;
        }
        return result;
    }
}

public enum Model3DType
{
    Ball,//球形
    Squareness,//矩形
    Cylinder,//圆柱体
}
public class ModelInfo
{
    public Model3DType type;
    /// <summary>
    /// 模型坐标
    /// </summary>
    public NetVector3 position;
    /// <summary>
    /// 模型大小（矩形，圆柱体）
    /// </summary>
    public NetVector3 scale;
    /// <summary>
    /// 模型半径（球形）
    /// </summary>
    public float radius;

}