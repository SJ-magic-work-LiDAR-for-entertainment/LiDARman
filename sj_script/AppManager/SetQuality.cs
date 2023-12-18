#if false
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetQuality : MonoBehaviour
{
    enum IMAGE_QUALITY{
        VERY_LOW,
        LOW,
        MID,
        HIGH,
        VERY_HIGH,
        ULTRA,
    };
    
    // Start is called before the first frame update
    void Start()
    {
		QualitySettings.SetQualityLevel((int)IMAGE_QUALITY.HIGH, true);
		// QualitySettings.SetQualityLevel((int)IMAGE_QUALITY.MID, true);
		
		Screen.SetResolution(1920, 1080, false/* full screen */);
		// Screen.SetResolution(1280, 720, false, 30/* Hz */);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

#endif


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetQuality : MonoBehaviour
{
	KeyCode Key_Disp = KeyCode.P;
	bool b_Disp = false;
	
	private string label = "";
	
    enum IMAGE_QUALITY{
        VERY_LOW,
        LOW,
        MID,
        HIGH,
        VERY_HIGH,
        ULTRA,
    };
	
	// readonly Vector2 resolution = new Vector2(1280, 720);
	readonly Vector2 resolution = new Vector2(1920, 1080);
	// readonly Vector2 resolution = new Vector2(1920 * 2, 1080 * 2);
	
	bool b_ResolutionSet = false;
	
	/******************************
	******************************/
    void Awake()
    {
		/********************
		Screen.SetResolution()は別threadで実行され、完了までに時間が掛かる(環境依存だが、1sec - )。
		********************/
		QualitySettings.SetQualityLevel((int)IMAGE_QUALITY.HIGH, true/* applyExpensiveChanges */);
		Screen.SetResolution((int)resolution.x, (int)resolution.y, false/* full screen */);
		
		/********************
		Coroutineを使って、SetResolutionの完了をjudge.
		********************/
		StartCoroutine(_IsSetResolution_OK(resolution));
	}
	/******************************
	******************************/
    void Start()
    {
    }

	/******************************
	******************************/ 
    void Update()
    {
		if(Input.GetKeyDown(Key_Disp)) b_Disp = !b_Disp;
		
		if(b_ResolutionSet)	label = "o:" + Screen.currentResolution.ToString() + "/ (" + Screen.width + ", " + Screen.height + ")"; // macの解像度 @ macのRefeshRate Hz/ (appの解像度)
		else				label = "x:" + Screen.currentResolution.ToString() + "/ (" + Screen.width + ", " + Screen.height + ")"; // macの解像度 @ macのRefeshRate Hz/ (appの解像度)
    }
	
	/****************************************
	****************************************/
	IEnumerator _IsSetResolution_OK(Vector2 targetResolution) {
		yield return new WaitUntil(() => Screen.width == (int)targetResolution.x && Screen.height == (int)targetResolution.y);
		
		b_ResolutionSet = true;
	}
	
	/****************************************
	■GUIのフォントサイズ変更など
		http://himascript.blog.fc2.com/blog-entry-6.html
	****************************************/
	void OnGUI()
	{
		/********************
		GUI.xxxは、OnGUIの中でのみtouch可.
		********************/
		if(b_Disp){
			GUI.skin.label.fontSize = 20;
			GUI.color = Color.white;
			
			GUI.Label(new Rect(15, 50, 700, 50), label);
		}
	}
}


