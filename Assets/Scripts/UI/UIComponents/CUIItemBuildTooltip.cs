using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CUIItemBuildTooltip : MonoBehaviour 
{
	public Text TitleText;
	public Text DetailHeadings;
	public Text DetailValues;
	public Text Description;

	public void Show(Vector3 Position, int ItemGUID)
	{
		//TitleText.text = CGame.Datastore.mItems[ItemGUID].mName;

		gameObject.GetComponent<RectTransform>().position = Position;
		gameObject.GetComponent<RectTransform>().localPosition += new Vector3(0.0f, +64.0f, 0.0f);
		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}
}
