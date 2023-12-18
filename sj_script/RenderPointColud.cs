/************************************************************
#define
************************************************************/

/************************************************************
************************************************************/
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

using System;				// need this to use Array
using Unity.Collections;	// need this to use NativeArray

/********************
********************/
using System.IO;
// using System; // need this to use DateTime

using System.Text.RegularExpressions; // to use Regex.Replace


/************************************************************
************************************************************/

/**************************************************
**************************************************/
[System.Serializable]
public class PointCloudParam
{
	[NonSerialized] public int port_;
	[SerializeField] public Mesh mesh_lidar_ = null;
	[SerializeField] public Material material_lidar_ = null;
	
	public PositionBufferUdp position_buffer_udp_ = null;
	public NativeArray<Color> colors_;
	public GraphicsBuffer color_buffer_;
	public MaterialPropertyBlock mat_props_;
	
	public PointCloudParam(){
	}
	
	public void Setup(int group_id, int max_points, int port, MessageManager message_manager){
		port_ = port;
		
		position_buffer_udp_ = new PositionBufferUdp(group_id, max_points, port_, message_manager);
		mat_props_ = new MaterialPropertyBlock();
		
		colors_ = new NativeArray<Color>(max_points, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		for(int i = 0; i < colors_.Length; i++){
			colors_[i] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		}
		
		/********************
		********************/
		color_buffer_ = new GraphicsBuffer(GraphicsBuffer.Target.Structured, max_points, sizeof(float) * 4);
		MaterialSetBuffer();
	}
	
	public void MaterialSetBuffer(){
		if(material_lidar_ != null) material_lidar_.SetBuffer("_InstanceColorBuffer", color_buffer_);
	}
	
	public void Dispose(){
		/********************
		********************/
		if(position_buffer_udp_ != null) position_buffer_udp_.Dispose();
		
		/********************
		********************/
		if(colors_.IsCreated) colors_.Dispose();
		
		if(color_buffer_ != null){
			if(color_buffer_.IsValid())	color_buffer_.Dispose();
		}
	}
}

/**************************************************
**************************************************/
struct ConfigParam{
	public int max_points_;
	public int port_;
}

/**************************************************
**************************************************/
public class RenderPointColud : MonoBehaviour
{
	/****************************************
	param
	****************************************/
	/********************
	********************/
	enum LidarInput{
		kRealLidar,
		kLog,
		kNumLidarInput,
	};
	
	/********************
	********************/
	ConfigParam[] config_param_ = new ConfigParam[(int)LidarInput.kNumLidarInput];
	
	[SerializeField] Global global_;
	[SerializeField] MessageManager message_manager_;
	
	/********************
	********************/
	[Header("LiDAR & Log")]
	[SerializeField]
	PointCloudParam[] point_cloud_param_ = new PointCloudParam[(int)LidarInput.kNumLidarInput]{new PointCloudParam(), new PointCloudParam()};
	
	/********************
	********************/
	[Header("sound_sync")]
	[SerializeField] int port_sound_ = 12348;
    [SerializeField] Mesh mesh_sound_center_ = null;
    [SerializeField] Material material_sound_center_ = null;
	
	SoundSyncFromUdp sound_sync_from_udp_ = null;
	
	
	/****************************************
	func
	****************************************/
	
	/******************************
	******************************/
    void Start(){
		/********************
		********************/
		sound_sync_from_udp_ = new SoundSyncFromUdp(port_sound_);
		
		/********************
		********************/
		if(point_cloud_param_.Length != (int)LidarInput.kNumLidarInput){
			Debug.Log("RenderPointColud > point_cloud_param_.Length must be just two.");
			SjUtil.MyQuitApp();
			return;
		}
		
		/********************
		********************/
		if( !ReadAndSetConfigParam() ) return;
		
		
		for(int i = 0; i < point_cloud_param_.Length; i++){
			point_cloud_param_[i].Setup(i, config_param_[i].max_points_, config_param_[i].port_, message_manager_);
		}
		
		/********************
		********************/
		SjUtil.LogError("start Log.", false); // initialize Log File.
	}
	
	/******************************
	******************************/
	string RemoveWhiteSpaces(string str) {
		return Regex.Replace(str, @"\s+", String.Empty);
    }
	
	/******************************
	******************************/
	bool ReadAndSetConfigParam(){
		/********************
		********************/
		Debug.Log("RenderPointCloud > config");
		
		/********************
		Path.Combine(1st, 2nd);
			1st のお尻に、"/"が ない場合は、自動的にこれを挿入してくれる。
			-> 2nd の頭には"/"を入れないのが正解。
			
			下記は、OK.
				"sj_testDir/File/SubFolder"	+ "data.txt"
				"sj_testDir/File/SubFolder/"	+ "data.txt"
			
			下記は、NG.
				"sj_testDir/File/SubFolder"	+ "/data.txt"
				"sj_testDir/File/SubFolder/"	+ "/data.txt"
		********************/
		string file_path = Path.Combine (SjUtil.getPathOf_Assets(), @"Config/config.txt");
		
		if (!File.Exists (file_path)){
			SjUtil.LogError("config file not exits.", true);
			SjUtil.MyQuitApp();
			return false;
		}
		
		/********************
		usingステートメントを使えるのはIDisposableインタフェースを継承しているクラスに限りますが、File.Openの戻り値はFileStreamクラスでこのクラスはStreamクラスを継承して作成されており、
		StreamクラスはIDisposableインタフェースを継承しているのでusingステートメントを使用する事が出来ます。
		
		usingステートメントを使用すると、例外が発生してもDisposeが実行されます。
		********************/
		// 1行ずつ
		try{
			using( var fs_r = new StreamReader(file_path, System.Text.Encoding.GetEncoding("UTF-8")) ){
				int group_id = 0;
				
				// while (fs_r.Peek() != -1){
				while (!fs_r.EndOfStream){
					string str_line = fs_r.ReadLine();
					// Debug.Log(str_line);
					str_line = RemoveWhiteSpaces(str_line);
					// Debug.Log(str_line);
					
					string[] block = str_line.Split(',', StringSplitOptions.None); // 1文字の場合は、シングルクォーテーション
					if(2 <= block.Length){
						/********************
						System.Convert.ToInt32
						前後にspaceが入っていても、(多分 それらを削除した上で)、所望の通り、変換を行ってくれた
						間にspaceがあると、NG.
						********************/
						if(block[0] == "group_id"){
							int value = (int)System.Convert.ToInt32(block[1]); // ToInt32, ToDouble, ToString, ...
							if( (0 <= value) && (value < config_param_.Length) )	{ group_id = value; }
						} 
						else if(block[0] == "port")			{ config_param_[group_id].port_			= (int)System.Convert.ToInt32(block[1]); } // ToInt32, ToDouble, ToString, ...
						else if(block[0] == "max_point")	{ config_param_[group_id].max_points_	= (int)System.Convert.ToInt32(block[1]); } // ToInt32, ToDouble, ToString, ...
					}
				}
			}
		}catch(Exception e){
			Debug.Log(e.Message);
			return false;
		}
		
		/********************
		********************/
		for(int i = 0; i < config_param_.Length; i++){
			string str_message = string.Format("group_id = {0}, port = {1}, max_point = {2}\n", i, config_param_[i].port_, config_param_[i].max_points_);
			Debug.Log(str_message);
		}
		
		return true;
	}
	
	/******************************
	******************************/
    void OnDestroy(){
		for(int i = 0; i < point_cloud_param_.Length; i++){
			point_cloud_param_[i].Dispose();
		}
		
		if(sound_sync_from_udp_ != null) sound_sync_from_udp_.Dispose();
	}
	
	/******************************
	******************************/
	bool IsSoundSyncUpdated(SoundSyncFromUdp sound_sync_from_udp){
		if( (sound_sync_from_udp != null) && (sound_sync_from_udp.IsUpdated()) )	return true;
		else																		return false;
	}
	
	/******************************
	******************************/
	bool IsSoundSyncColor( bool b_apply_sound, SoundSyncFromUdp sound_sync_from_udp, int anim_type, bool b_update_lidar ){
		if( b_apply_sound && IsSoundSyncUpdated(sound_sync_from_udp) && (anim_type == (int)Global.SoundSyncType.kColor) )							return true;
		else if( b_apply_sound && !IsSoundSyncUpdated(sound_sync_from_udp) && (anim_type == (int)Global.SoundSyncType.kColor) && b_update_lidar )	return true;
		else																																		return false;
	}
	
	/******************************
	******************************/
	bool IsNoTouchColor( bool b_apply_sound, SoundSyncFromUdp sound_sync_from_udp, int anim_type, bool b_update_lidar ){
		if( b_apply_sound && !IsSoundSyncUpdated(sound_sync_from_udp) && (anim_type == (int)Global.SoundSyncType.kColor) && !b_update_lidar )	return true;
		else																																	return false;
	}
	
	/******************************
	******************************/
    void Update()
    {
		/********************
		********************/
		if( SjUtil.IsCriticalError() ) return;
		
		/********************
		********************/
		if( global_.param_sound_sync_[0].b_apply_ || global_.param_sound_sync_[1].b_apply_ )	{sound_sync_from_udp_.Update(global_.scale_sound_center_); }
		
		/********************
		********************/
		for(int i = 0; i < point_cloud_param_.Length; i++){
			point_cloud_param_[i].position_buffer_udp_.Update(	global_.point_ofs_[i], global_.scale_lidar_[i], global_.param_sound_sync_[i].b_apply_, sound_sync_from_udp_, global_.param_sound_sync_[i].sync_coord_, global_.param_sound_sync_[i].sync_anim_type_, 
																global_.sound_sync_size_l_, global_.sound_sync_size_h_, global_.b_enable_base_wave_, global_.param_area_filter_[i].b_invert_x_, global_.param_area_filter_[i].b_limit_x_, global_.param_area_filter_[i].min_x_, global_.param_area_filter_[i].max_x_,
																global_.param_area_filter_[i].b_limit_z_, global_.param_area_filter_[i].min_z_, global_.param_area_filter_[i].max_z_, global_.p_far_away_, global_.rot_deg_[i],
																global_.b_disp_msg_packet_not_in_order_);
			
			int num_points_in_this_frame = point_cloud_param_[i].position_buffer_udp_.GetNumPointsInThisFrame();
			if(num_points_in_this_frame <= 0) continue;
			
			/********************
			********************/
			if( IsSoundSyncColor(global_.param_sound_sync_[i].b_apply_, sound_sync_from_udp_, global_.param_sound_sync_[i].sync_anim_type_, point_cloud_param_[i].position_buffer_udp_.IsUpdated()) ){
			#if false
				// for debug : Sj
				var points = point_cloud_param_[i].position_buffer_udp_.Positions;
				for(int j = 0; j < num_points_in_this_frame; j++){
					float x = points[j].x + global_.point_ofs_[i].x;
					float y = points[j].y + global_.point_ofs_[i].y;
					float z = points[j].z + global_.point_ofs_[i].z;
					
					float hue;
					// if(x < 0)	hue = global_.param_sound_sync_[i].sync_hue_l_;
					if(z < 0)	hue = global_.param_sound_sync_[i].sync_hue_l_;
					else		hue = global_.param_sound_sync_[i].sync_hue_h_;
					point_cloud_param_[i].colors_[j] = Color.HSVToRGB(hue, global_.saturation_, global_.brightness_);
				}
			#else
				var points = point_cloud_param_[i].position_buffer_udp_.Positions;
				
				for(int j = 0; j < num_points_in_this_frame; j++){
					float x = points[j].x + global_.point_ofs_[i].x;
					float y = points[j].y + global_.point_ofs_[i].y;
					float z = points[j].z + global_.point_ofs_[i].z;
					
					float val = sound_sync_from_udp_.GetWaveVal(x, y, z, (int)global_.param_sound_sync_[i].sync_coord_, (int)global_.param_sound_sync_[i].sync_anim_type_);
					float hue = SjUtil.MyMap(val, 0, 1, global_.param_sound_sync_[i].sync_hue_l_, global_.param_sound_sync_[i].sync_hue_h_);
					
					point_cloud_param_[i].colors_[j] = Color.HSVToRGB(hue, global_.saturation_, global_.brightness_);
				}
			#endif
				
			}else if( IsNoTouchColor(global_.param_sound_sync_[i].b_apply_, sound_sync_from_udp_, global_.param_sound_sync_[i].sync_anim_type_, point_cloud_param_[i].position_buffer_udp_.IsUpdated()) ){
				// notouch
			}else{
				var points = point_cloud_param_[i].position_buffer_udp_.Positions;
				
				for(int j = 0; j < num_points_in_this_frame; j++){
					Vector3 pos = points[j] +  global_.point_ofs_[i] - global_.col_fade_org_point_;
					
					float hue;
					if(global_.b_col_fade_use_sqr_for_distance_){
						float sqr_distance = pos.sqrMagnitude;
						hue = SjUtil.MyMap(sqr_distance, global_.near_ * global_.near_, global_.far_ * global_.far_, global_.hue_near_, global_.hue_far_);
					}else{
						float distance = pos.magnitude;
						hue = SjUtil.MyMap(distance, global_.near_, global_.far_, global_.hue_near_, global_.hue_far_);
					}
					
					point_cloud_param_[i].colors_[j] = Color.HSVToRGB(hue, global_.saturation_, global_.brightness_);
				}
			}
			
			point_cloud_param_[i].color_buffer_.SetData(point_cloud_param_[i].colors_);
			
			/********************
			********************/
			if(global_.b_draw_points_[i]){
				var matrices = point_cloud_param_[i].position_buffer_udp_.Matrices;
				var rparams = new RenderParams(point_cloud_param_[i].material_lidar_)	{ receiveShadows = true, shadowCastingMode = ShadowCastingMode.On, layer = LayerMask.NameToLayer("sj_test"), matProps = point_cloud_param_[i].mat_props_, };
				
				/********************
				■METAL SHADER ERROR WHEN USING A STRUCTUREDBUFFER
					https://issuetracker.unity3d.com/issues/metal-shader-error-when-using-a-structuredbuffer
				********************/
				const int DrawAtOnce = 511; // 1023 is NG. less than 511 is OK.
				for(int ofs = 0; ofs < num_points_in_this_frame; ofs += DrawAtOnce){
					int count = Mathf.Min(DrawAtOnce, num_points_in_this_frame - ofs);
					point_cloud_param_[i].MaterialSetBuffer();
					point_cloud_param_[i].mat_props_.SetInteger("_InstanceIdOffset", ofs);
					Graphics.RenderMeshInstanced(rparams, point_cloud_param_[i].mesh_lidar_, 0, matrices, count, ofs);
				}
			}
		} // for(int i = 0; i < 2; i++){
		
		/********************
		********************/
		if(global_.b_disp_sound_center_){
			var rparams_sound_center = new RenderParams(material_sound_center_)	{ receiveShadows = true, shadowCastingMode = ShadowCastingMode.On, layer = LayerMask.NameToLayer("sj_test"), };
			Graphics.RenderMeshInstanced(rparams_sound_center, mesh_sound_center_, 0, sound_sync_from_udp_.m_sound_center_, 1, 0);
		}
    }
	
	/****************************************
	****************************************/
	public Vector3 GetSoundCenter(){
		return sound_sync_from_udp_.GetSoundCenter();
	}
	
	/******************************
	******************************/
	void OnGUI()
	{
	}
}
