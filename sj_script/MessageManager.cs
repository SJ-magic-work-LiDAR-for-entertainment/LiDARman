using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class MessageManager : MonoBehaviour
{
	[SerializeField] GameObject GO_MessagePanel;
	[SerializeField] TextMeshProUGUI TextMeshPro;
	Image PanelImg;
	
	[SerializeField] float T_Fade = 4.0f;
	float t_FadeFrom = 0;
	
	[SerializeField] float text_a_max	= 1.0f;
	[SerializeField] float panel_a_max	= 0.5f;
	
	bool b_critical_error = false;
	
    void Start()
    {
		/********************
		********************/
		PanelImg = GO_MessagePanel.GetComponent<Image>();
		
		TextMeshPro.text = "Good morning";
		TextMeshPro.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		
		/********************
		********************/
		GO_MessagePanel.SetActive(false);
    }
	
    void Update()
    {
		/*
		if(Input.GetKeyDown(KeyCode.Alpha0)){
			StartCoroutine( DispMessage_Fadeout() );
		}
		*/
        
    }
	
	public void DispCriticalMessage(string str_message, Color text_color){
		/********************
		********************/
		TextMeshPro.color = text_color;
		TextMeshPro.text = str_message;
		
		GO_MessagePanel.SetActive(true);
		
		Color PanelCol		= PanelImg.color; // struct
		PanelImg.color		= new Color(PanelCol.r, PanelCol.g, PanelCol.b, panel_a_max);
		
		Color TextCol		= TextMeshPro.color; // struct
		TextMeshPro.color	= new Color(TextCol.r, TextCol.g, TextCol.b, text_a_max);
		
		/********************
		********************/
		b_critical_error = true;
	}
	
	public void DispMessage(string str_message, Color text_color){
		/********************
		********************/
		if(b_critical_error) return;
		
		/********************
		********************/
		TextMeshPro.color = text_color;
		TextMeshPro.text = str_message;
		StartCoroutine( DispMessage_Fadeout() );
	}
	
	IEnumerator DispMessage_Fadeout(){
		/********************
		********************/
		t_FadeFrom = Time.time;
		GO_MessagePanel.SetActive(true);
		
		/********************
		********************/
		Color PanelCol	= PanelImg.color; // struct
		Color TextCol	= TextMeshPro.color; // struct
		
		/********************
		********************/
		float b = 2.0f;
		float tan = -b / T_Fade;
		
		float alpha = b;
		
		while(0 < alpha){
			float now = Time.time;
			alpha = tan * (now - t_FadeFrom) + b;
			
			float a_Text = alpha;
			if(text_a_max < a_Text)	a_Text = text_a_max;
			if(a_Text < 0.0f)		a_Text = 0.0f;
			
			TextMeshPro.color = new Color(TextCol.r, TextCol.g, TextCol.b, a_Text);
			
			
			float a_Panel = alpha;
			if(panel_a_max < a_Panel)	a_Panel = panel_a_max;
			if(a_Panel < 0.0f)			a_Panel = 0.0f;
			
			PanelImg.color = new Color(PanelCol.r, PanelCol.g, PanelCol.b, a_Panel);
			
			
			yield return null;
		}
		
		GO_MessagePanel.SetActive(false);
	}
}
