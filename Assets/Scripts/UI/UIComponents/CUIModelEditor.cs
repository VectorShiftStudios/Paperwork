using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/*
public class CUIModelEditor : MonoBehaviour 
{
	public GameObject ResourceEntryPrefab;
	public GameObject ModelsListView;
	public GameObject PrimitvesListView;
	public GameObject BrushesListView;
	
	public GameObject StringFieldPrefab;
	public GameObject ToggleFieldPrefab;
	public GameObject ButtonFieldPrefab;
	public GameObject Vec3FieldPrefab;

	public GameObject BrushFieldList;
	public GameObject ModelFieldList;
	public GameObject PrimitiveFieldList;
	public GameObject GeneralFieldList;

	private bool _active;

	private int _selectedModelID = -1;
	private int _selectedPrimitiveID = -1;
	private int _selectedBrushID = -1;

	private GameObject _currentModelGOB;

	public void DrawGrid(int Width)
	{
		float x = -0.5f;
		float z = -0.5f;

		Color color = new Color(0.3f, 0.3f, 0.3f, 1.0f);

		for (int i = 0; i < Width + 1; ++i)
			CDebug.DrawLine(new Vector3(i + x, 0.0f, z), new Vector3(i + x, 0.0f, Width + z), color);

		for (int i = 0; i < Width + 1; ++i)
			CDebug.DrawLine(new Vector3(x, 0.0f, i + z), new Vector3(Width + x, 0.0f, i + z), color);
	}
	
	void UpdateNO() 
	{
		DrawGrid(10);

		// Draw selected primitive outline
		if (_selectedPrimitiveID != -1)
		{
			CPrimitiveDefinition prim =  CGame.Datastore.mPrimitives[_selectedPrimitiveID];
			CModelDefinition.CQuad q = CModelDefinition.GetQuad(prim);

			if (Input.GetKey(KeyCode.LeftArrow))
			{
				prim.mRotation -= 0.1f;
				_UpdateModel();
			}
			else if (Input.GetKey(KeyCode.RightArrow))
			{
				prim.mWidth += 0.1f;
				_UpdateModel();
			}

			CDebug.DrawLine(q.v1, q.v2, Color.green);
			CDebug.DrawLine(q.v2, q.v3, Color.green);
			CDebug.DrawLine(q.v3, q.v4, Color.green);
			CDebug.DrawLine(q.v4, q.v1, Color.green);
		}		
	}

	private void _ClearResourceTree(GameObject Tree)
	{
		CUtility.DestroyChildren(Tree);
	}

	private GameObject _AddResourceEntry(GameObject Tree, string Name)
	{
		GameObject entry = GameObject.Instantiate(ResourceEntryPrefab) as GameObject;
		entry.GetComponent<RectTransform>().SetParent(Tree.transform);
		entry.GetComponent<RectTransform>().localScale = Vector3.one;
		entry.transform.GetChild(0).GetComponent<Text>().text = Name;
		return entry;
	}

	private void _AddModelResourceEntry(string Name, int ID)
	{
		GameObject gob = _AddResourceEntry(ModelsListView, Name);
		gob.GetComponent<Button>().onClick.AddListener(() => _SelectModel(ID));
	}

	private void _AddPrimitiveResourceEntry(string Name, int ID)
	{
		GameObject gob = _AddResourceEntry(PrimitvesListView, Name);
		gob.GetComponent<Button>().onClick.AddListener(() => _SelectPrimitive(ID));
	}

	private void _AddBrushResourceEntry(string Name, int ID)
	{
		GameObject gob = _AddResourceEntry(BrushesListView, Name);
		gob.GetComponent<Button>().onClick.AddListener(() => _SelectBrush(ID));
	}

	private void _SelectModel(int ID)
	{
		_SelectPrimitive(-1);
		CUtility.DestroyChildren(ModelFieldList);
		_selectedModelID = ID;

		if (ID == -1)
			return;

		CModelDefinition def = CGame.Datastore.mModels[ID];
		_AddStringField(ModelFieldList, "Name", def.mName);

		CUtility.DestroyChildren(PrimitvesListView);
		foreach (CPrimitiveDefinition prim in def.mPrimitives)
		{
			_AddPrimitiveResourceEntry("(" + prim.mGUID + ") " + prim.mName, prim.mGUID);
		}

		// Display the model in the world.
		if (_currentModelGOB != null)
		{
			GameObject.Destroy(_currentModelGOB);
		}
		
		_currentModelGOB = def.CreateGameObject();
	}

	private void _SelectPrimitive(int ID)
	{
		CUtility.DestroyChildren(PrimitiveFieldList);
		_selectedPrimitiveID = ID;

		if (ID == -1)
			return;

		CPrimitiveDefinition def = CGame.Datastore.mPrimitives[ID];

		_AddStringField(PrimitiveFieldList, "Name", def.mName, (string val) => { def.mName = val; _UpdateModel(); });
		_AddStringField(PrimitiveFieldList, "Shape", def.mShape.ToString());

		// Create appropriate fields depending on line shape.

		_AddVec3Field(PrimitiveFieldList, "Position (XYZ)", def.mPosition, (Vector3 val) => { def.mPosition = val; _UpdateModel(); });
		_AddVec3Field(PrimitiveFieldList, "Size (XYZ)", def.mSize, (Vector3 val) => { def.mSize = val; _UpdateModel(); });
		_AddVec3Field(PrimitiveFieldList, "Rotation (XYZ)", def.mOrientation, (Vector3 val) => { def.mOrientation = val; _UpdateModel(); });

		_AddStringField(PrimitiveFieldList, "Fill Brush", def.mFillBrush.ToString());
		_AddStringField(PrimitiveFieldList, "Outline Brush", def.mOutlineBrush.ToString());

		_AddToggleField(PrimitiveFieldList, "Edge 1", def.mEdge1, (bool val) => { def.mEdge1 = val; _UpdateModel(); });
		_AddToggleField(PrimitiveFieldList, "Edge 2", def.mEdge2, (bool val) => { def.mEdge2 = val; _UpdateModel(); });
		_AddToggleField(PrimitiveFieldList, "Edge 3", def.mEdge3, (bool val) => { def.mEdge3 = val; _UpdateModel(); });
		_AddToggleField(PrimitiveFieldList, "Edge 4", def.mEdge4, (bool val) => { def.mEdge4 = val; _UpdateModel(); });

		// Offset for each point
		_AddVec3Field(PrimitiveFieldList, "Point 1 (XYZ)", def.mPosition, (Vector3 val) => { def.mPosition = val; _UpdateModel(); });
	}

	private void _SelectBrush(int ID)
	{
		CUtility.DestroyChildren(BrushFieldList);
		_selectedBrushID = ID;

		if (ID == -1)
			return;

		CBrushDefinition def = CGame.Datastore.mBrushes[ID];
		_AddStringField(BrushFieldList, "Name", def.mName);
		_AddStringField(BrushFieldList, "R", def.mColor.r.ToString());
		_AddStringField(BrushFieldList, "G", def.mColor.g.ToString());
		_AddStringField(BrushFieldList, "B", def.mColor.b.ToString());
		_AddStringField(BrushFieldList, "Floor Mix", def.mColor.a.ToString());
		_AddStringField(BrushFieldList, "Weight", def.mWeight.ToString());
	}

	private GameObject _AddField(GameObject FieldPrefab, GameObject FieldList)
	{
		GameObject entry = GameObject.Instantiate(FieldPrefab) as GameObject;
		entry.GetComponent<RectTransform>().SetParent(FieldList.transform);
		entry.GetComponent<RectTransform>().localScale = Vector3.one;

		return entry;
	}

	private void _AddStringField(GameObject FieldList, string Name, string Value)
	{
		GameObject gob = _AddField(StringFieldPrefab, FieldList);
		gob.transform.GetChild(0).GetComponent<Text>().text = Name;
		gob.transform.GetChild(1).GetComponent<InputField>().text = Value;
		//gob.transform.GetChild(1).GetComponent<InputField>().onEndEdit.AddListener((string text) => Entity.SetFieldValue(index, text));
	}

	private void _AddStringField(GameObject FieldList, string Name, string Value, Action<string> Field)
	{
		GameObject gob = _AddField(StringFieldPrefab, FieldList);
		gob.transform.GetChild(0).GetComponent<Text>().text = Name;
		gob.transform.GetChild(1).GetComponent<InputField>().text = Value;
		gob.transform.GetChild(1).GetComponent<InputField>().onEndEdit.AddListener((string text) => {Field(text);});
	}

	private void _AddToggleField(GameObject FieldList, string Name, bool Value, Action<bool> Field)
	{
		GameObject gob = _AddField(ToggleFieldPrefab, FieldList);
		gob.transform.GetChild(0).GetComponent<Text>().text = Name;
		gob.transform.GetChild(1).GetComponent<Toggle>().isOn = Value;
		gob.transform.GetChild(1).GetComponent<Toggle>().onValueChanged.AddListener((bool text) => { Field(text); });
	}

	private void _AddButtonField(GameObject FieldList, string Name, Action Field)
	{
		GameObject gob = _AddField(ButtonFieldPrefab, FieldList);
		gob.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = Name;
		gob.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => Field());
	}

	private void _AddVec3Field(GameObject FieldList, string Name, Vector3 Value, Action<Vector3> Field)
	{
		GameObject gob = _AddField(Vec3FieldPrefab, FieldList);
		InputField x = gob.transform.GetChild(1).GetComponent<InputField>();
		InputField y = gob.transform.GetChild(2).GetComponent<InputField>();
		InputField z = gob.transform.GetChild(3).GetComponent<InputField>();

		gob.transform.GetChild(0).GetComponent<Text>().text = Name;
		x.text = Value.x.ToString();
		y.text = Value.y.ToString();
		z.text = Value.z.ToString();
		
		x.onEndEdit.AddListener((string text) => { Field(new Vector3(_ParseFloat(x.text), _ParseFloat(y.text), _ParseFloat(z.text))); });
		y.onEndEdit.AddListener((string text) => { Field(new Vector3(_ParseFloat(x.text), _ParseFloat(y.text), _ParseFloat(z.text))); });
		z.onEndEdit.AddListener((string text) => { Field(new Vector3(_ParseFloat(x.text), _ParseFloat(y.text), _ParseFloat(z.text))); });
	}

	private void _UpdateModel()
	{
		//if (_selectedModelID != 0)
		{
			CGame.Datastore.ReloadModels();
			//CGame.Datastore.mModels[_selectedModelID].CreateMesh()
		}
	}

	private float _ParseFloat(string Text)
	{
		float f = 0.0f;
		float.TryParse(Text, out f);
		return f;
	}

	private bool _ParseBool(string Text)
	{
		bool b = false;
		
		Text = Text.Trim();
		Text = Text.ToLower();

		if (Text == "0" || Text == "false")
			b = false;
		else if (Text == "1" || Text == "true")
			b = true;

		//bool.TryParse(Text, out b);
		return b;
	}

	public void Init()
	{
		_active = true;

		CUtility.DestroyChildren(GeneralFieldList);
		CUtility.DestroyChildren(ModelFieldList);
		CUtility.DestroyChildren(PrimitiveFieldList);
		CUtility.DestroyChildren(BrushFieldList);

		CUtility.DestroyChildren(GeneralFieldList);
		_AddToggleField(GeneralFieldList, "Scale Model", false, (bool val) => { });
		_AddButtonField(GeneralFieldList, "Save All", () => {});

		foreach (KeyValuePair<int, CModelDefinition> entry in CGame.Datastore.mModels)
		{			
			_AddModelResourceEntry("(" + entry.Value.mGUID + ") " + entry.Value.mName, entry.Value.mGUID);
		}

		foreach (KeyValuePair<int, CBrushDefinition> entry in CGame.Datastore.mBrushes)
		{
			_AddBrushResourceEntry("(" + entry.Value.mGUID + ") " + entry.Value.mName, entry.Value.mGUID);
		}
	}

	public void Shutdown()
	{
		_active = false;
	}
}
*/