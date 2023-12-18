/************************************************************
■参考
	Github
		https://github.com/SJ-magic-study-unity/study__UnityFixFramerate
		
	UnityでFPSを設定する方法
		http://unityleaning.blog.fc2.com/blog-entry-2.html
************************************************************/
using UnityEngine;
using System.Collections;

/************************************************************
************************************************************/
public class setFrameRate : MonoBehaviour {
	
	[SerializeField] public int app_fps_ = 30;
	[SerializeField] float lpf_ = 0.1f;
	float current_fps_ = 0;
	
	private string label = "";
	KeyCode Key_Disp = KeyCode.P;
	bool b_Disp = false;
	
	// [SerializeField]	int FrameRate = 30;
	
	void Awake() { 
		QualitySettings.vSyncCount = 0; // Don't Sync : 元はevery v blank
		Application.targetFrameRate = app_fps_;
	}
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(Key_Disp)) b_Disp = !b_Disp;
		
		float fps = 1.0f / Time.deltaTime;
		current_fps_ = lpf_ * fps + (1 - lpf_) * current_fps_;
		label = string.Format("{0:000.0}", (int)(current_fps_));
	}
	
	/****************************************
	****************************************/
	void OnGUI()
	{
		/********************
		********************/
		if(b_Disp){
			GUI.skin.label.fontSize = 20;
			GUI.color = Color.white;
			
			GUI.Label(new Rect(15, 100, 500, 40), label);
		}
	}
}
