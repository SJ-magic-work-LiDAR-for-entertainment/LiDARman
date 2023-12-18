/************************************************************
************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering.PostProcessing; // need this

/************************************************************
■Scripting Post Processing at RUNTIME in Unity (Post Processing Stack Tutorial)
	https://www.youtube.com/watch?v=JF4t9pNaZxg
************************************************************/
public class ControlBloom : MonoBehaviour
{
	/****************************************
	****************************************/
	// KeyCode key_reset_bloom = KeyCode.A;
	private PostProcessVolume post_process_volume_;
	private Bloom bloom_;
	
	[SerializeField] Global global_;
	
	/****************************************
	****************************************/
	
	/******************************
	******************************/
	void Start()
	{
		post_process_volume_ = GetComponent<PostProcessVolume>();
		post_process_volume_.profile.TryGetSettings(out bloom_);
	}

	/******************************
	******************************/
	void Update()
	{
		bloom_.intensity.value = global_.bloom_intensity_;
	}
	
	/******************************
	******************************/
	void ResetBloom(){
		/********************
		bloom_自体のon/off
		********************/
		// bloom_.active = false;
		
		/********************
		value
		********************/
		bloom_.intensity.value = 10.0f;
		bloom_.threshold.value = 2.0f;
		
		/********************
		bloom_内 itemのon/off
		********************/
		// bloom_.intensity.overrideState = false;
		// bloom_.threshold.overrideState = true;
	}
}

