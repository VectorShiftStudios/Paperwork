using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CBetterScrollRect : ScrollRect
{
	public override void OnBeginDrag(PointerEventData eventData) { }
	public override void OnDrag(PointerEventData eventData) { }
	public override void OnEndDrag(PointerEventData eventData) { }

	/*
	private Vector2 scrollTarget;

	public override void OnScroll(PointerEventData data)
	{
		if (!IsActive())
			return;

		//base.OnScroll(data);

		Debug.Log("Scroll: " + data.scrollDelta);

		//velocity = data.scrollDelta * 100.0f;
		//normalizedPosition = normalizedPosition + data.scrollDelta;

		//var pointerDelta = localCursor - m_PointerStartLocalCursor;
		//Vector2 position = content.anchoredPosition;// m_ContentStartPosition + pointerDelta;

		//SetContentAnchoredPosition(content.anchoredPosition + data.scrollDelta);

		//scrollTarget += data.scrollDelta * 10.0f;
	}

	public void Update()
	{
		//SetContentAnchoredPosition(Vector2.Lerp(content.anchoredPosition, scrollTarget, Time.deltaTime * 1.0f));
	}
	*/
}
