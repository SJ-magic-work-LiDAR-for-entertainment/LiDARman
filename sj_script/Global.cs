/************************************************************
************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

/********************
for
	ObjectDisposedException
	Exception

これに伴い、
	// static Object sync_ = new Object();
	static UnityEngine.Object sync_ = new UnityEngine.Object();
********************/
using System; // already appears

// for udp_.
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using System.Text.RegularExpressions;



/************************************************************
************************************************************/

/**************************************************
**************************************************/
[System.Serializable]
public class ParamSoundSync{
	[SerializeField]					public bool b_apply_;
	[SerializeField, Range(0.0f, 3.0f)] public int sync_coord_;
	[SerializeField, Range(0.0f, 3.0f)] public int sync_anim_type_;
	[SerializeField, Range(0.0f, 1f)]	public float sync_hue_h_;
	[SerializeField, Range(0.0f, 1f)]	public float sync_hue_l_;
	
	public ParamSoundSync(bool b_apply, int sync_coord, int sync_anim_type, float sync_hue_h, float sync_hue_l ){
		b_apply_		= b_apply;
		sync_coord_		= sync_coord;
		sync_anim_type_	= sync_anim_type;
		sync_hue_h_		= sync_hue_h;
		sync_hue_l_		= sync_hue_l;
	}
}

/**************************************************
**************************************************/
[System.Serializable]
public class ParamAreaFilter{
	[SerializeField]					public bool b_invert_x_;
	[SerializeField] 					public bool b_limit_x_;
	[SerializeField, Range(-50, 0)]		public float min_x_;
	[SerializeField, Range(0, 50)]		public float max_x_;
	[SerializeField] 					public bool b_limit_z_;
	[SerializeField, Range(0, 50)] 		public float min_z_;
	[SerializeField, Range(0, 200)] 	public float max_z_;
	
	public ParamAreaFilter(bool b_invert_x, bool b_limit_x, float min_x, float max_x, bool b_limit_z, float min_z, float max_z){
		b_invert_x_	= b_invert_x;
		b_limit_x_	= b_limit_x;
		min_x_		= min_x;
		max_x_		= max_x;
		b_limit_z_	= b_limit_z;
		min_z_		= min_z;
		max_z_		= max_z;
	}
}

/**************************************************
**************************************************/
[System.Serializable]
public class ParamCamMove{
	[SerializeField, Range(1f, 30f)]		public float wheelSpeed_ = 10f;
	[SerializeField, Range(0.01f, 0.5f)]	public float moveSpeed_ = 0.1f;
	[SerializeField, Range(0.1f, 10f)]		public float rotateSpeed_ = 0.3f;
}


/**************************************************
**************************************************/
public class Global : MonoBehaviour
{
	/****************************************
	func.
	****************************************/
	
	/******************************
	******************************/
	public void Init(int IN_PORT)
	{
		/********************
		https://docs.microsoft.com/ja-jp/dotnet/api/system.net.sockets.socket.receivetimeout?view=net-6.0
			udp_.Client.ReceiveTimeout = 0;
			udp_.Client.ReceiveTimeout = -1;
		で無期限になる。
		-> thread_側のLoopをflagで抜けるので、NG
		********************/
		udp_ = new UdpClient(IN_PORT);
		udp_.Client.ReceiveTimeout = 1000;
		
		thread_ = new Thread(new ThreadStart(ThreadMethod));
		thread_.Start(); 
	}

	/******************************
	******************************/
	// public void Dispose()
	void OnDestroy() {
		lock(sync_)	{ b_thread_running_ = false; }
		// thread_.Join();
		
		Debug.Log("Global > Disposed");
	}
	
	/******************************
	******************************/
	public bool IsUpdated(){
		return b_updated_;
	}
	
	/******************************
	******************************/
	public void Start(){
		Init(12346);
	}
	
	/******************************
	******************************/
	public void Update(){
		/********************
		********************/
		string str_message;
		bool b_udp;
		
		lock(sync_){
			str_message = str_message_;
			b_udp = b_udp_;
			b_udp_ = false;
		}
		
		/********************
		********************/
		b_updated_ = false;
		
		if(b_udp){
			string[] block = str_message.Split(',', StringSplitOptions.None); // 1文字の場合は、シングルクォーテーション
			if (IsMessageFromRemoteControl(block) ){
				ExtractParameters(block);
				b_updated_ = true;
			}else if( IsMessageAboutSoundSyncPointSize(block) ){
				ExtractParametersSoundSyncPointSize(block);
				b_updated_ = true;
			}
		}
	}
	
	/******************************
	******************************/
	private bool IsMessageFromRemoteControl(string[] block){
		if(	block[0] == "/RemoteControl" )	{ return true; }
		else								{ return false; }
	}
	
	/******************************
	******************************/
	private bool IsMessageAboutSoundSyncPointSize(string[] block){
		if(	block[0] == "/RemoteControl_SoundSyncPointSize" )	{ return true; }
		else													{ return false; }
	}
	
	/******************************
	******************************/
	void Enqueue(string str_message)
	{
		str_message_ = str_message;
		b_udp_ = true;
	}
	
	/******************************
	******************************/
	void ExtractParametersSoundSyncPointSize(string[] block){
		int id = 1;
		
		sound_sync_size_h_	= (float)System.Convert.ToDouble(block[id++]);
		sound_sync_size_l_	= (float)System.Convert.ToDouble(block[id++]);
	}
	
	/******************************
	******************************/
	void ExtractParameters(string[] block){
		int id = 1;
		
		// Group_Camera_
		param_cam_move_.wheelSpeed_		= (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
		param_cam_move_.moveSpeed_		= (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
		param_cam_move_.rotateSpeed_	= (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
		
		// Group_Misc_
		saturation_			= (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
		brightness_			= (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
		bloom_intensity_	= (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
		
		if( (int)System.Convert.ToInt32(block[id++]) == 0 )	b_disp_msg_packet_not_in_order_ = false;
		else												b_disp_msg_packet_not_in_order_ = true;
		
		// Group_ColorFade_
		if( (int)System.Convert.ToInt32(block[id++]) == 0 )	b_col_fade_use_sqr_for_distance_ = false;
		else												b_col_fade_use_sqr_for_distance_ = true;
		
		col_fade_org_point_ = new Vector3( (float)System.Convert.ToDouble(block[id++]), (float)System.Convert.ToDouble(block[id++]), (float)System.Convert.ToDouble(block[id++]) );
		hue_near_	= (float)System.Convert.ToDouble(block[id++]);
		hue_far_	= (float)System.Convert.ToDouble(block[id++]);
		near_		= (float)System.Convert.ToDouble(block[id++]);
		far_		= (float)System.Convert.ToDouble(block[id++]);
		
		// Group_SoundSync
		// sound_sync_size_h_	= (float)System.Convert.ToDouble(block[id++]);
		// sound_sync_size_l_	= (float)System.Convert.ToDouble(block[id++]);
		
		if( (int)System.Convert.ToInt32(block[id++]) == 0 )	b_disp_sound_center_ = false;
		else												b_disp_sound_center_ = true;
		
		scale_sound_center_	= (float)System.Convert.ToDouble(block[id++]);
		
		// Group_Array_[]
		for(int i = 0; i < 2; i++){
			if( (int)System.Convert.ToInt32(block[id++]) == 0 )	b_draw_points_[i] = false;
			else												b_draw_points_[i] = true;
			
			point_ofs_[i] = new Vector3( (float)System.Convert.ToDouble(block[id++]), (float)System.Convert.ToDouble(block[id++]), (float)System.Convert.ToDouble(block[id++]) );
			
			rot_deg_[i] = (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
			scale_lidar_[i] = (float)System.Convert.ToDouble(block[id++]); // ToInt32, ToDouble, ToString, ...
			
			if( (int)System.Convert.ToInt32(block[id++]) == 0 )	param_sound_sync_[i].b_apply_ = false;
			else												param_sound_sync_[i].b_apply_ = true;
			
			param_sound_sync_[i].sync_coord_		= (int)System.Convert.ToInt32(block[id++]);
			param_sound_sync_[i].sync_anim_type_	= (int)System.Convert.ToInt32(block[id++]);
			
			param_sound_sync_[i].sync_hue_h_		= (float)System.Convert.ToDouble(block[id++]);
			param_sound_sync_[i].sync_hue_l_		= (float)System.Convert.ToDouble(block[id++]);
		}
		
		// Group_AreaFilter_
		for(int i = 0; i < 2; i++){
			if( (int)System.Convert.ToInt32(block[id++]) == 0 )	param_area_filter_[i].b_invert_x_ = false;
			else												param_area_filter_[i].b_invert_x_ = true;
			
			if( (int)System.Convert.ToInt32(block[id++]) == 0 )	param_area_filter_[i].b_limit_x_ = false;
			else												param_area_filter_[i].b_limit_x_ = true;
			param_area_filter_[i].min_x_	= (float)System.Convert.ToDouble(block[id++]);
			param_area_filter_[i].max_x_	= (float)System.Convert.ToDouble(block[id++]);
			
			if( (int)System.Convert.ToInt32(block[id++]) == 0 )	param_area_filter_[i].b_limit_z_ = false;
			else												param_area_filter_[i].b_limit_z_ = true;
			param_area_filter_[i].min_z_	= (float)System.Convert.ToDouble(block[id++]);
			param_area_filter_[i].max_z_	= (float)System.Convert.ToDouble(block[id++]);
		}
		
		p_far_away_ = new Vector3( (float)System.Convert.ToDouble(block[id++]), (float)System.Convert.ToDouble(block[id++]), (float)System.Convert.ToDouble(block[id++]) );
	}
	
	
	/******************************
	******************************/
	private void ThreadMethod()
	{
		bool b_thread_running = true;

		while(b_thread_running){
			try{
				string str_message = "";
				
				/********************
				ここをlockしてしまうと、
					byte[] message = udp_.Receive(ref remoteEP);
				でtimeoutするまで、main thread_に制御が渡らない。
				udp_は、startのみ、mainで触り、終了処理も、thread_抜ける時に行うようにする。
				********************/
				if(udp_ != null){
					IPEndPoint remoteEP = null;
					byte[] message = udp_.Receive(ref remoteEP);
					str_message = Encoding.ASCII.GetString(message);
				}
				
				lock(sync_) { Enqueue(str_message); }
				
			}catch(SocketException){
				/********************
				UdpClient投げる例外
					https://msdn.microsoft.com/ja-jp/library/82dxxas0(v=vs.110).aspx
				********************/
				// Debug.Log("Receive udp_ timeout : No client??");
			}catch (ObjectDisposedException e){
				Debug.Log("Global > !!! udp_receive : thread_ : ObjectDisposedException !!! : " + e.ToString());
			}catch (Exception e ) {
				Debug.Log("Global > !!! udp_receive : thread_ : Exception !!! : " + e.ToString());
			}
			
			// Thread.Sleep(1);	// byte[] message = udp_.Receive(ref remoteEP); でwait入るので、ここは不要
								// 逆にmessageが溜まっている時は、waitナシで回る
			
			lock(sync_) { b_thread_running = b_thread_running_; }
		}
		
		/********************
		********************/
		if(udp_ != null){
			try{
				udp_.Close();
				udp_ = null;
			}catch(SocketException){
				Debug.Log("Global > SocketException : udp_ receive : OnDestroy : udp_.Close()");
			}
		}
		
		/********************
		********************/
		Debug.Log("Global > Exit thread_ : udp_receive");
	}
	
	
	
	/****************************************
	param
	****************************************/
	bool b_updated_ = false;
	
	/********************
	********************/
	// static Object sync_ = new Object();
	UnityEngine.Object sync_ = new UnityEngine.Object();

	/********************
	********************/
	// int IN_PORT = 12345;
	UdpClient udp_ = null; // need to be "static" to be touched in thread_.
	Thread thread_;
	
	/********************
	need to be locked when touch the params below.
	********************/
	volatile bool b_thread_running_ = true;
	string str_message_ = "";
	bool b_udp_ = false;
	
	
	/****************************************
	param
	****************************************/
	public enum SoundSyncCoord{
		kX,
		kZ,
		kCircle,
		kSphere,
	};
	
	public enum SoundSyncType{
		kSync_y,
		kSync_z,
		kSize,
		kColor,
	};
	
	/****************************************
	param
	****************************************/
	const int kInspectorSpace = 15;
	
	/********************
	********************/
	[Header("[Camera]")]
	[SerializeField] public ParamCamMove param_cam_move_ = new ParamCamMove();
	
	/********************
	********************/
	[Header("[Misc]")]
	[SerializeField] public bool b_enable_base_wave_ = true;
	[SerializeField] public bool[] b_draw_points_ = new bool[2]{true, true};
	[SerializeField] public Vector3[] point_ofs_ = new Vector3[2]{ new Vector3(0f, -0.75f, 12.5f), new Vector3(0f, 0f, 0f) };
	[SerializeField] public float[] rot_deg_ = new float[2]{ 0.0f, 180.0f };
	[SerializeField] public float[] scale_lidar_ = new float[2]{ 0.03f, 0.035f };
	[SerializeField, Range(0.0f, 1f)]		public float saturation_ = 0.75f;
	[SerializeField, Range(0.0f, 1f)]		public float brightness_ = 0.8f;
	[SerializeField, Range(0.0f, 20f)]		public float bloom_intensity_ = 3.3f;
	
	[SerializeField] public bool b_disp_msg_packet_not_in_order_ = true;
	
	/********************
	********************/
	[Header("[ColorFade]")]
	[SerializeField] public bool b_col_fade_use_sqr_for_distance_ = true;
	[SerializeField] public Vector3 col_fade_org_point_ = new Vector3(0f, 0f, 0f);
	[SerializeField, Range(0.0f, 1f)]	public float hue_near_ = 0f;
	[SerializeField, Range(0.0f, 1f)]	public float hue_far_ = 0.65f;
	[SerializeField, Range(0.0f, 200f)]	public float near_ = 1f;
	[SerializeField, Range(0.0f, 200f)]	public float far_ = 80f;
	
	/********************
	********************/
	[Header("[SoundSync]")]
	[SerializeField] public ParamSoundSync[] param_sound_sync_ = new ParamSoundSync[2]{ new ParamSoundSync(true/* b_apply */,	0/* sync_coord */,	3/* sync_anim_type */,	0.25f/* sync_hue_h */,	0.65f/* sync_hue_l */),
																						new ParamSoundSync(true/* b_apply */,	0/* sync_coord */,	2/* sync_anim_type */,	0.10f/* sync_hue_h */,	0.65f/* sync_hue_l */) }; 
	[SerializeField, Range(0.0f, 1f)]	public float sound_sync_size_h_ = 0.07f;
	[SerializeField, Range(0.0f, 1f)]	public float sound_sync_size_l_ = 0.03f;
	[SerializeField] public bool b_disp_sound_center_ = false;
	[SerializeField, Range(0.1f, 5.0f)]	public float scale_sound_center_ = 1.0f;
	
	/********************
	********************/
	[Header("[Area Filter]")]
	[SerializeField] public ParamAreaFilter[] param_area_filter_ = new ParamAreaFilter[2]{	new ParamAreaFilter(true/* b_invert_x_ */,	true/* b_limit_x_ */,	-1.3f/* min_x_ */,	0.62f/* max_x_ */,	true/* b_limit_z_ */,	0.0f/* min_z_ */,	4.0f/* max_z_ */),
																							new ParamAreaFilter(true/* b_invert_x_ */,	false/* b_limit_x_ */,	-10.0f/* min_x_ */,	10.0f/* max_x_ */,	true/* b_limit_z_ */,	3.7f/* min_z_ */,	300.0f/* max_z_ */) };
	
	[SerializeField] public Vector3 p_far_away_ = new Vector3(0f, 0f, -100f);
}
