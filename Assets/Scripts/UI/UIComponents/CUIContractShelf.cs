using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUIContractShelf : MonoBehaviour
{
	public GameObject ContractTrayPrefab;

	public void Init(int TrayCount)
	{
		RectTransform rect = GetComponent<RectTransform>();
		rect.sizeDelta = new Vector2(TrayCount * 60.0f, 60.0f);

		for (int i = 0; i < TrayCount; ++i)
		{
			GameObject tray = GameObject.Instantiate(ContractTrayPrefab);

			tray.GetComponent<RectTransform>().SetParent(rect);
			tray.GetComponent<RectTransform>().localScale = Vector3.one;
			tray.transform.GetChild(0).GetComponent<CUIContractTray>().Init();
		}
	}
}