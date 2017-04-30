using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class CUIResources : MonoBehaviour
{
	public AudioSource UIAudioSource;
	public AudioSource UIMusicSource;

	public AudioMixer MasterMixer;
	public AudioMixerGroup SoundsMixer;

	// For profiler OnGUI
	public Font DebugFont;
	public Texture DebugBlock;
	public Texture DebugBlock2;

	public GameObject Console;
	public Text ConsoleText;
	public GameObject ConsoleInputText;
	
	public RectTransform CanvasTransform;
	public CanvasScaler PrimaryCanvasScaler;
}
