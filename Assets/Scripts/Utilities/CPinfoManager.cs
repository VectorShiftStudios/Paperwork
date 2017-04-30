using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPinfo
{
	public GameObject mGob;

	private float _lifeTime;
	private Vector3 _anchorPoint;
	private Vector3 _Velocity;
	private Text _text;

	public CPinfo(GameObject Prefab, string Text, Vector3 AnchorPoint, Color Colour)
	{
		mGob = GameObject.Instantiate(Prefab) as GameObject;
		_text = mGob.GetComponent<Text>();
		_text.text = Text;
		_text.color = Colour;

		//mGob.transform.SetParent(CGame.UIResources.UnitUIPanel.transform);
		_lifeTime = 1.0f;
		_anchorPoint = AnchorPoint;
		_Velocity = Vector3.up * 4.0f;

		// Offset X & Z		

		_Velocity.x += CGame.UniversalRandom.GetNextFloat() * 0.2f - 0.1f;
		_Velocity.z += CGame.UniversalRandom.GetNextFloat() * 0.2f - 0.1f;
	}

	public bool Update()
	{
		_lifeTime -= Time.deltaTime;

		if (_lifeTime <= 0.0f)
		{
			GameObject.Destroy(mGob);
			return false;
		}

		_anchorPoint += _Velocity * Time.deltaTime;
		Color c = _text.color;
		c.a = _lifeTime;
		_text.color = c;

		// Dampen Velocity
		//_Velocity *= 0.1f * Time.deltaTime;

		// Apply Accel
		_Velocity += Vector3.down * 5.0f * Time.deltaTime;

		Vector3 screenPos = Camera.main.WorldToScreenPoint(_anchorPoint);
		((RectTransform)mGob.transform).anchoredPosition = new Vector2((int)screenPos.x, (int)screenPos.y);				

		return true;
	}
}

public class CPinfoManager : MonoBehaviour 
{
	public GameObject BasicText;

	private List<CPinfo> _pinfos = new List<CPinfo>();

	public void CreateInfo(string Text, Vector3 AnchorPoint, Color Colour)
	{
		CPinfo pinfo = new CPinfo(BasicText, Text, AnchorPoint, Colour);
		_pinfos.Add(pinfo);
	}	 
		
	void Update () 
	{
		for (int i = 0; i < _pinfos.Count; ++i)
		{
			if (!_pinfos[i].Update())
			{
				_pinfos.RemoveAt(i);
				--i;
			}
		}
	}
}
