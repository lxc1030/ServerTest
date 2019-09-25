using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TxtOnekey : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public string split;
    public string all;

    public string tableName;
    public string tableIndex;

    public string[] listSp;
    public string[] tbox;


    private void Init()
    {
        all = all.Replace("\t", "");
        all = all.Replace(" ", "");

        listSp = all.Split(new string[] { split }, System.StringSplitOptions.RemoveEmptyEntries);

        //tbox
        tbox = new string[listSp.Length];
        for (int i = 0; i < listSp.Length; i++)
        {
            tbox[i] = "tbox_" + listSp[i];
        }

        //debug = "";
        ////+ ",RID=" + 
        //for (int i = 0; i < listSp.Length; i++)
        //{
        //    debug += "+ \"," + listSp[i] + "=" + "\" " + "+" + "\n";
        //}
        //Debug.LogError(debug);

    }

    string debug = "";




    [ContextMenu("View")]
    public void View()
    {
        Init();



        debug = "";
        // || $("#tbox_AchieveID").val() == ""
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "|| $(\"#" + tbox[i] + "\").val() == \"\"" + "\n";
        }
        debug = debug.Substring(2, debug.Length - 2);
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

    }






    [ContextMenu("UpdateT")]
    public void UpdateTXT()
    {
        if (!string.IsNullOrEmpty(all))
        {
            Init();
        }


        debug = "";
        //s 变量名 sAch_ShowLv,
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "s" + listSp[i] + ",\n";
        }
        debug = debug.Remove(debug.Length - 2);
        debug += ";\n";
        Debug.LogError(debug);


        debug = "";
        //sShowID = Request.Form["ShowID"];
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "s" + listSp[i] + " = Request.Form[\"" + listSp[i] + "\"];" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

        debug = "";
        //|| string.IsNullOrEmpty(sAch_ShowLv)
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "|| string.IsNullOrEmpty(s" + listSp[i] + ")" + "\n";
        }
        debug = debug.Substring(2, debug.Length - 2);
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);


        debug = "检查int值,需要删减\n";
        //  || !General.IsMatch(sAch_ShowLv, intRegex)
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = "|| ";
            debug += (i == 0 ? "" : temp) + "!General.IsMatch(s" + listSp[i] + ", intRegex)" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

        debug = "检查string值，需要删减\n";
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "DB.SQLFiltrate(ref s" + listSp[i] + ");" + "\n";
        }
        debug += "\n";
        Debug.LogError(debug);


        debug = "检查string值,需要增加符号'\n";
        //Ach_ShowLv,
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = ",";
            debug += "+ \"" + (i == 0 ? "" : temp) + listSp[i] + "\"\n";
        }
        Debug.LogError(debug);

        debug = "";
        //s 变量名 sAch_ShowLv,
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = "\",\" + ";
            debug += "+ " + (i == 0 ? "" : temp) + "s" + listSp[i] + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);


        debug = "检查string值，需要增加‘\n";
        // + "Ach_ShowID=" + sAch_ShowID
        //+",Ach_ShowLv=" + sAch_ShowLv
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = ",";
            debug += "+ \"" + (i == 0 ? "" : temp) + listSp[i] + "=\" + s" + listSp[i] + "\n";
        }
        Debug.LogError(debug);

    }


    [ContextMenu("Add")]
    public void AddNewTXT()
    {
        all = all.Replace("+ \"'\"", "");
        all = all.Replace("='\"", "\"");
        all = all.Replace("=\"", "\"");

        string first = "";
        string second = "";
        string[] adds = all.Split(new char[] { '+' });
        for (int i = 0; i < adds.Length; i++)
        {
            if (i % 2 == 0)
            {
                first += "+ \",\" +" + adds[i] + " \n";
            }
            else
            {
                second += "+" + adds[i] + " \n";
            }
        }
        Debug.LogError(first);
        Debug.LogError(second);
    }



    [ContextMenu("静态表_List")]
    public void XList()
    {
        Init();

        debug = "";
        //ShowLv: $("#tbox_Ach_ShowLv").val(),
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += listSp[i] + ": $(\"#" + tbox[i] + "\").val()," + "\n";
        }
        debug = debug.Remove(debug.Length - 2);
        debug += "\n";
        Debug.LogError(debug);

        debug = "";
        // || $("#tbox_AchieveID").val() == ""
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "|| $(\"#tbox_" + listSp[i] + "\").val() == \"\"\n";
        }
        debug = debug.Substring(2, debug.Length - 2);
        debug += "\n";
        Debug.LogError(debug);

        debug = "";
        //function ShowDetails
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = ",";
            debug += (i == 0 ? "" : temp) + listSp[i];
        }
        debug = "function ShowDetails(id, " + debug + ") {";
        debug += "\n";
        Debug.LogError(debug);


        debug = "";
        //tbox_.val
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "$(\"#tbox_" + listSp[i] + "\").val(" + listSp[i] + ");" + "\n";
        }
        debug += "$(\"#Btn_Addnew\").hide();" + "\n";
        debug += "$(\"#Btn_Update\").show();" + "\n";
        debug += "$(\"#Btn_Delete\").show();" + "\n";
        Debug.LogError(debug);


        debug = "";
        //tbox_.val
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "$(\"#tbox_" + listSp[i] + "\").val(\"\");" + "\n";
        }
        Debug.LogError(debug);

    }

    [ContextMenu("静态表_List_div")]
    public void ListDIV()
    {
        Init();

        debug = "";
        //<label for="tbox_ShowID" class="col-md-2 control-label">ShowID</label>
        //<div class="col-md-2">
        //    <input type="text" id="tbox_ShowID" class="form-control" placeholder="--------" maxlength="10" />
        //</div>
        for (int i = 0; i < tbox.Length; i++)
        {
            debug = "<label for=\"" + tbox[i] + "\" class=\"col-md-2 control-label\">" + listSp[i] + "</label>\n";
            debug += "<div class=\"col-md-2\">\n";
            debug += "    <input type=\"text\" id=\"" + tbox[i] + "\" class=\"form-control\" placeholder=\"--------\" maxlength=\"10\" />\n";
            debug += "</div>\n";
            Debug.LogError(debug);
        }

    }

    [ContextMenu("静态表_GetData")]
    public void GetData()
    {
        Init();

        debug = "";
        //<td style="width: 5%">Ach_Reward_Para1</td>
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "<td style=\"width: 5%\">" + listSp[i] + "</td>" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

        debug = "";
        //<td><%=Dt.Rows[i][0] %></td>
        for (int i = 0; i < listSp.Length + 1; i++)
        {
            debug += "<td><%=Dt.Rows[i][" + i + "] %></td>" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);
    }


    [ContextMenu("静态表_Update")]
    public void JTBUpdate()
    {
        Init();

        debug = "";
        //s + 变量名
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "s" + listSp[i] + ",\n";
        }
        debug = debug.Remove(debug.Length - 2);
        debug += ";\n";
        Debug.LogError(debug);


        debug = "";
        //sShowID = Request.Form["ShowID"];
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "s" + listSp[i] + " = Request.Form[\"" + listSp[i] + "\"];" + "\n";
        }
        debug += "\n";
        Debug.LogError(debug);

        debug = "";
        //|| string.IsNullOrEmpty(sAch_ShowLv)
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "|| string.IsNullOrEmpty(s" + listSp[i] + ")" + "\n";
        }
        debug = debug.Substring(2, debug.Length - 2);
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);


        debug = "检查int值,需要删减\n";
        //  || !General.IsMatch(sAch_ShowLv, intRegex)
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = "|| ";
            debug += (i == 0 ? "" : temp) + "!General.IsMatch(s" + listSp[i] + ", intRegex)" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

        debug = "检查string值，需要删减\n";
        //DB.SQLFiltrate();
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "DB.SQLFiltrate(ref s" + listSp[i] + ");" + "\n";
        }
        debug += "\n";
        Debug.LogError(debug);


        //sql insert1
        debug = "";
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = ",";
            debug += "+ \"" + (i == 0 ? "" : temp) + listSp[i] + "\"\n";
        }
        debug = "SQL = \"INSERT INTO \" + TableName + \" (\n" + debug + "+ \") VALUES(\"";
        debug += "\n";
        Debug.LogError(debug);

        //sql insert2
        debug = "";
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = ",";
            debug += "+ \"" + (i == 0 ? "" : temp) + "\" + s" + listSp[i] + "\n";
        }
        debug += "+ \")\";";
        Debug.LogError(debug);


        //sql update
        debug = "SQL = \"UPDATE \" + TableName + \" SET \"\n";
        for (int i = 0; i < listSp.Length; i++)
        {
            string temp = ",";
            debug += "+ \"" + (i == 0 ? "" : temp) + listSp[i] + "=\"" + " + s" + listSp[i] + "\n";
        }
        debug += "+ \" WHERE \" + OrderFiled + \"=\" + sId" + ";";
        Debug.LogError(debug);


        //sql delete
        debug = "SQL = \"DELETE FROM " + tableName + " WHERE " + tableIndex + " = \" + s" + tableIndex + ";";
        Debug.LogError(debug);
    }


    [ContextMenu("Get-----List")]
    public void GetxxxList()
    {
        Init();

        debug = "SQL = \"SELECT TOP \" + PageSize * PageId + \" * FROM " + tableName + " ORDER BY " + tableIndex + "\";";
        debug += "\n";
        debug += "SQL2 = \"SELECT COUNT(*) FROM " + tableName + "\";";
        Debug.LogError(debug);

        debug = "";
        //<td style="width: 5%">Ach_Reward_Para1</td>
        for (int i = 0; i < listSp.Length; i++)
        {
            debug += "<td style=\"width: 5%\">" + listSp[i] + "</td>" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

        debug = "";
        //<td><%=Dt.Rows[i][0] %></td>
        for (int i = 0; i < listSp.Length + 1; i++)
        {
            debug += "<td><%=Dt.Rows[i][" + i + "] %></td>" + "\n";
        }
        debug = debug.Remove(debug.Length - 1);
        debug += "\n";
        Debug.LogError(debug);

        debug = "两个%%中间内容\n";
        for (int i = 0; i < listSp.Length + 1; i++)
        {
            string temp = "+\",\"+";
            debug += (i == 0 ? "=" : temp) + "Dt.Rows[i][" + i + "]";
        }
        debug += "\n";
        Debug.LogError(debug);
    }
}