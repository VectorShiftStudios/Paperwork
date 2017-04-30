using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUIWindowBase : MonoBehaviour, IDragHandler, IPointerDownHandler
{
	public GameObject RootObject;
	
	public void OnDrag(PointerEventData EventData)
	{
		RootObject.GetComponent<RectTransform>().position += new Vector3(EventData.delta.x, EventData.delta.y);		
	}

	public void OnPointerDown(PointerEventData EventData)
	{
		RootObject.GetComponent<RectTransform>().SetParent(RootObject.GetComponent<RectTransform>().parent);
	}
}