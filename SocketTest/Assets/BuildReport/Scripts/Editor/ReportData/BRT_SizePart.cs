/*
http://www.cgsoso.com/forum-211-1.html

CG搜搜 Unity3d 每日Unity3d插件免费更新 更有VIP资源！

CGSOSO 主打游戏开发，影视设计等CG资源素材。

插件如若商用，请务必官网购买！

daily assets update for try.

U should buy the asset from home store if u use it in your project!
*/


namespace BuildReportTool
{

// represents one entry in an asset list
// really poorly named, unfortunately, the serialized XML files already use "SizePart"
// an updater would be needed if the class name here is changed.
[System.Serializable]
public class SizePart
{
	// file with path, but relative to project's Assets folder
	public string Name;


	// how much the asset takes up space in the final build, in percentage
	// (gotten from editor log). obviously, only used when this SizePart is in the Used Assets list.
	public double Percentage = 0;


	public string Size; // the raw file size as existing in the assets folder, expressed in human-readable format
	public long SizeBytes = -1; // the raw file size in bytes


	// in cases where we don't have exact values of file size (we just got it from
	// editor log as string, which was converted to readable format already).
	// expressed in bytes (but with fractions because of the inaccuracies).
	//
	// this applies to the "Used Assets" list
	//
	public double DerivedSize = 0;


	// helper function to get the proper raw file size
	public double UsableSize
	{
		get
		{
			if (DerivedSize > 0)
				return DerivedSize;
			return SizeBytes;
		}
	}




	// same as getting the `Size` but since we now have two size types,
	// for consistency, we now refer to the size as either RawSize and ImportedSize
	public string RawSize { get{ return Size; } set{ Size = value; } }
	public long RawSizeBytes { get{ return SizeBytes; } set{ SizeBytes = value; } }


	// the file size as imported into Unity, expressed in human-readable format.
	// if this SizePart is for an asset that has no imported size (e.g. built-in asset)
	// this will be empty
	public string ImportedSize;

	public long ImportedSizeBytes; // the imported file size, expressed in bytes




	public bool IsTotal { get{ return Name == "Complete size"; } }
}

} // namespace BuildReportTool
