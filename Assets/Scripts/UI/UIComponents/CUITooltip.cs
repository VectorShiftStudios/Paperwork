using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CUITooltip : MonoBehaviour 
{
	public Text TitleText;	
	public Text DetailText;
	public Text DescriptionText;
	public GameObject DetailsGOB;

	public void SetDetails(string Title, string Details, string Description)
	{
		TitleText.text = Title;

		if (Details == "")
		{
			DetailsGOB.SetActive(false);
		}
		else
		{
			DetailsGOB.SetActive(true);
			DetailText.text = Details;
		}

		if (Description == "")
		{
			DescriptionText.gameObject.SetActive(false);
		}
		else
		{
			DescriptionText.text = Description;
			DescriptionText.gameObject.SetActive(true);
		}
	}

	public void Show(Vector2 UISpacePos, Vector2 Pivot)
	{
		gameObject.GetComponent<RectTransform>().anchoredPosition = UISpacePos;
		gameObject.GetComponent<RectTransform>().pivot = Pivot;
		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}
}
