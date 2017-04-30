using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// UI Interface for contract paper stacks.
/// </summary>
public class CUIContractStackIconPaper : MonoBehaviour
{
	public RectTransform IconTransform;
	public Image PaperBackgroundImage;
	public Image PaperFrameImage;

	public void SetColor(Color32 BackgroundColor, Color32 FrameColor)
	{
		PaperBackgroundImage.color = BackgroundColor;
		PaperFrameImage.color = FrameColor;
	}

	public void SetTransform(RectTransform Parent, Vector2 Position, float Scale)
	{
		IconTransform.SetParent(Parent);
		IconTransform.localScale = new Vector3(Scale, Scale, Scale);
		IconTransform.localPosition = Position;
	}
}
