using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CColorThumbnail : MonoBehaviour 
{
	public Image ColorThumbnail;
	public Text ColorRText;
	public Text ColorGText;
	public Text ColorBText;
	
	void Update () 
	{
		int iR = 0;
		int iG = 0;
		int iB = 0;

		int.TryParse(ColorRText.text, out iR);
		int.TryParse(ColorGText.text, out iG);
		int.TryParse(ColorBText.text, out iB);

		float r = (float)iR / 255.0f;
		float g = (float)iG / 255.0f;
		float b = (float)iB / 255.0f;

		ColorThumbnail.color = new Color(r, g, b, 1.0f);
	}
}
