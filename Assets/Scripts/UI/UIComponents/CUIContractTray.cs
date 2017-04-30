using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CUIContractTray : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public class CTrayEntry
	{
		public GameObject mGOB;
		public Vector2 mTargetPosition;
		public float mHeight;

		private RectTransform _rect;

		public void Spawn(RectTransform Parent, GameObject Prefab)
		{
			GameObject entryGOB = GameObject.Instantiate(Prefab);
			_rect = entryGOB.GetComponent<RectTransform>();
			_rect.SetParent(Parent);
			_rect.localScale = Vector3.one;
			_rect.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
			mHeight = 60.0f;
		}

		public void Update(bool Expanded)
		{
			float speed = 10.0f;

			// TODO: Check if lerp differences are greater than possible.
			_rect.anchoredPosition = (mTargetPosition - _rect.anchoredPosition) * speed * Time.deltaTime + _rect.anchoredPosition;

			Vector2 targetSize = new Vector2(60.0f, 10.0f);

			//if (Expanded)
				targetSize.y = mHeight;

			_rect.sizeDelta = (targetSize - _rect.sizeDelta) * speed * Time.deltaTime + _rect.sizeDelta;
		}
	}

	private bool _expanded = false;
	private float _lerpTarget = 0.0f;
	private float _lerp = 0.0f;

	private RectTransform _rect;

	public GameObject ContractTrayEntryPrefab;

	private List<CTrayEntry> _entries;

	public void Init()
	{
		_rect = GetComponent<RectTransform>();
		_entries = new List<CTrayEntry>();

		AddEntry();
		AddEntry();
		AddEntry();		
	}

	public void AddEntry()
	{
		CTrayEntry entry = new CTrayEntry();
		entry.Spawn(_rect, ContractTrayEntryPrefab);
		_entries.Add(entry);

		//CUIContractStackIcon stackIcon = trayIcon.GetComponent<CUIContractStackIcon>();
		//stackIcon.Init(3, 1.0f, Color.white, Color.white, Color.white);
	}
	
	void Update()
	{
		_lerp = (_lerpTarget - _lerp) * 20.0f * Time.deltaTime + _lerp;
		// TODO: Epsilon
		_lerp = Mathf.Clamp(_lerp,  0.0f, 1.0f);

		_rect.sizeDelta = Vector2.Lerp(new Vector2(60.0f, 60.0f), new Vector2(60.0f, 180.0f), _lerp);
		
		float yPos = 10.0f;
		for (int i = 0; i < _entries.Count; ++i)
		{
			if (_expanded)
				yPos += _entries[i].mHeight + 5.0f;
			else
				yPos += 15.0f;

			_entries[i].mTargetPosition = new Vector2(0.0f, yPos);

			_entries[i].Update(_expanded);
		}
	}
	
	void IPointerEnterHandler.OnPointerEnter(PointerEventData EventData)
	{
		//Debug.Log("Showing");
		_expanded = true;
		_lerpTarget = 1.0f;
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData EventData)
	{
		//Debug.Log("Hiding");
		_expanded = false;
		_lerpTarget = 0.0f;
	}
}