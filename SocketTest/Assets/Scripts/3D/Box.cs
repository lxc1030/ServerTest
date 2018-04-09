using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour
{
    public BoxInfo myInfo;

    private MeshRenderer myMesh;
    public void Awake()
    {
        myMesh = GetComponentInChildren<MeshRenderer>();
    }
    public void Init(BoxInfo info)
    {
        myInfo = info;
        ChangeTexture(TeamType.Both);
    }
    /// <summary>
    /// 根据服务器的数值修改拥有者
    /// </summary>
    /// <param name="userIndex"></param>
    public void ChangeOwner(int userIndex)
    {
        myInfo.ownerIndex = userIndex;
        //转换拥有者成队伍颜色
        TeamType type = DataController.instance.ActorList[myInfo.ownerIndex].MyTeam;
        ChangeTexture(type);
        //Debug.LogError(myInfo.myIndex + "/改成" + type);
    }
    public void ChangeTexture(TeamType type)
    {
        Material m = DataController.instance.GetMaterialOfTeamType(type);
        myMesh.material = m;
    }
}
