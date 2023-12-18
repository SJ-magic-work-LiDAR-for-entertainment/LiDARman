/************************************************************
************************************************************/
/* from PositinBuffer.cs */
using UnityEngine;
using Unity.Mathematics;

using Unity.Collections;
using System;

/* from udp_Receive.cs */
// using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/********************
for
	ObjectDisposedException
	Exception

これに伴い、
	// static Object sync_ = new Object();
	static UnityEngine.Object sync_ = new UnityEngine.Object();
********************/
// using System; // already appears

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
public class PacketData{
	// public int group_id_;
	public int num_points_in_this_frame_;
	// public int num_packets_for_this_frame_;
	public int packet_id_;
	public int ofs_;
	
	public List<float3> positions_ = new List<float3>();
	
	public PacketData(string[] block, bool b_invert_x, bool b_limit_x, float min_x, float max_x, bool b_limit_z, float min_z, float max_z, Vector3 p_far_away, float rot_deg){
		// group_id_					= (int)System.Convert.ToInt32(block[1]); // ToInt32, ToDouble, ToString, ...
		num_points_in_this_frame_		= (int)System.Convert.ToInt32(block[2]); // ToInt32, ToDouble, ToString, ...
		// num_packets_for_this_frame_	= (int)System.Convert.ToInt32(block[3]); // ToInt32, ToDouble, ToString, ...
		packet_id_						= (int)System.Convert.ToInt32(block[4]); // ToInt32, ToDouble, ToString, ...
		ofs_							= (int)System.Convert.ToInt32(block[5]); // ToInt32, ToDouble, ToString, ...
		
		const int id_ofs = 6;
		int num_points_in_this_packet = (block.Length - id_ofs) / 3 /* xyz */;
		for(int i = 0; i < num_points_in_this_packet; i++){
			float x = (float)System.Convert.ToDouble( block[i * 3 + 0 + id_ofs] );  // ToInt32, ToDouble, ToString, ...
			float y = (float)System.Convert.ToDouble( block[i * 3 + 1 + id_ofs] );  // ToInt32, ToDouble, ToString, ...
			float z = (float)System.Convert.ToDouble( block[i * 3 + 2 + id_ofs] );  // ToInt32, ToDouble, ToString, ...
			
			if(b_invert_x) x = -x;
			
			Vector3 rot_point;
			if(rot_deg == 0)	rot_point = new Vector3(x, y, z);
			else				rot_point = RotPoint( new Vector3(x, y, z), rot_deg );
			
			if( ( b_limit_x && !IsInRange(rot_point.x, min_x, max_x) ) || ( b_limit_z &&  !IsInRange(rot_point.z, min_z, max_z) ) ){
				var p = math.float3(p_far_away.x, p_far_away.y, p_far_away.z);
				positions_.Add(p);
			}else{
				var p = math.float3(rot_point.x, rot_point.y, rot_point.z);
				positions_.Add(p);
			}
		}
	}
	
	bool IsInRange(float val, float min_val, float max_val){
		if( (min_val < val) && (val < max_val) )	return true;
		else										return false;
	}
	
	/******************************
	■Matrix4x4.MultiplyPoint3x4
		https://docs.unity3d.com/jp/2018.4/ScriptReference/Matrix4x4.MultiplyPoint3x4.html
	******************************/
	Vector3 RotPoint(Vector3 org, float rot_deg){
		Quaternion rot = Quaternion.Euler(0, rot_deg, 0);
        Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
		
		var new_point = m.MultiplyPoint3x4(org);
		return new_point;
	}
};

/**************************************************
**************************************************/
public class UdpBuffer{
	UnityEngine.Object sync_ = new UnityEngine.Object();
	
	enum State{
		kWaitZero,
		kCollecting,
	};
	State state_ = State.kWaitZero;
	
	const int kSendPointsAtOnce_Min_ = 20;
	
	int group_id_;
	int max_points_;
	
	public PacketData[] packet_datas_;
	
	int i_in_ = 0;
	int num_points_collected_ = 0;
	
	bool b_Error_ = false;
	public Vector3[] point_data_ = null;
	
	/********************
	********************/
	readonly Color kMessageCol_Normal	= new Color(1.0f, 1.0f, 1.0f, 1.0f);
	readonly Color kMessageCol_Warning	= new Color(1.0f, 1.0f, 0.0f, 1.0f);
	readonly Color kMessageCol_Error		= new Color(1.0f, 0.0f, 0.0f, 1.0f);
	
	/********************
	********************/
	bool b_disp_message_			= false;
	bool b_disp_critical_message_	= false;
	string str_disp_message_ = "";
	Color col_disp_message_;
	
	/********************
	********************/
	bool b_invert_x_ = false;
	bool b_limit_x_ = false;
	float min_x_ = -50.0f;
	float max_x_ = 50.0f;
	bool b_limit_z_ = false;
	float min_z_ = 0.0f;
	float max_z_ = 50.0f;
	Vector3 p_far_away_ = new Vector3(0, 0, 0);
	float rot_deg_ = 0;
	bool b_disp_msg_packet_not_in_order_ = true;
	
	public UdpBuffer(int group_id, int max_points){
		group_id_ = group_id;
		max_points_ = max_points;
		
		int max_packets = max_points_ / kSendPointsAtOnce_Min_;
		if(max_points_ % kSendPointsAtOnce_Min_ != 0) max_packets++;
		
		packet_datas_ = new PacketData[max_packets];
		i_in_ = 0;
	}
	
	public int GetGroupId(){
		return group_id_;
	}
	
	void SetDispMessage(string str_disp_message, Color col_disp_message){
		lock(sync_){
			b_disp_message_ = true;
			str_disp_message_ = str_disp_message;
			col_disp_message_ = col_disp_message;
		}
	}
	
	public bool GetDispMessage(out string str_disp_message, out Color col_disp_message){
		bool b_disp_message;
		
		lock(sync_){
			b_disp_message		= b_disp_message_;
			str_disp_message	= str_disp_message_;
			col_disp_message	= col_disp_message_;
			
			b_disp_message_		= false;
		}
		
		return b_disp_message;
	}
	
	void SetCriticalMessage(string str_disp_message, Color col_disp_message){
		lock(sync_){
			b_disp_critical_message_	= true;
			str_disp_message_			= str_disp_message;
			col_disp_message_			= col_disp_message;
		}
	}
	
	public bool GetCriticalMessage(out string str_disp_message, out Color col_disp_message){
		bool b_disp_critical_message;
		
		lock(sync_){
			b_disp_critical_message	= b_disp_critical_message_;
			str_disp_message		= str_disp_message_;
			col_disp_message		= col_disp_message_;
			
			b_disp_message_			= false;
		}
		
		return b_disp_critical_message;
	}
	
	public void Enqueue(String str_message){
		/********************
		********************/
		bool b_invert_x;
		bool b_limit_x;
		float min_x;
		float max_x;
		bool b_limit_z;
		float min_z;
		float max_z;
		Vector3 p_far_away;
		float rot_deg;
		bool b_disp_msg_packet_not_in_order;
		
		lock(sync_) { b_invert_x = b_invert_x_; b_limit_x = b_limit_x_; min_x = min_x_; max_x = max_x_; b_limit_z = b_limit_z_; min_z = min_z_; max_z = max_z_; p_far_away = p_far_away_; rot_deg = rot_deg_; b_disp_msg_packet_not_in_order = b_disp_msg_packet_not_in_order_; }
		
		/********************
		********************/
		if(b_Error_) return;
		
		/********************
		********************/
		string[] block = str_message.Split(',', StringSplitOptions.None); // 1文字の場合は、シングルクォーテーション
		
		if(!IsMessageValid(block)) { SetDispMessage( string.Format("UdpBuffer_[{0}] > Received Invalid Message", group_id_), kMessageCol_Warning ); return; }
		
		/********************
		********************/
		PacketData packet = new PacketData(block, b_invert_x, b_limit_x, min_x, max_x, b_limit_z, min_z, max_z, p_far_away, rot_deg);
		
		/********************
		********************/
		switch(state_){
			case State.kWaitZero:
				if(packet.ofs_ == 0){
					state_ = State.kCollecting;
					EnqueueMainProcess(packet, b_disp_msg_packet_not_in_order);
				}
				break;
				
			case State.kCollecting:
				EnqueueMainProcess(packet, b_disp_msg_packet_not_in_order);
				break;
		}
	}
	
	void EnqueueMainProcess(PacketData packet, bool b_disp_msg_packet_not_in_order){
		if(num_points_collected_ != packet.ofs_){
			/*
			SetDispMessage(string.Format("UdpBuffer_[{0}] > Packet not in order", group_id_), kMessageCol_Warning);
			StateToWaitZero();
			return; // 一時的にappの更新 止まっていた可能性もある -> やり直し
			*/
			
			if(b_disp_msg_packet_not_in_order)	{ SetDispMessage(string.Format("UdpBuffer_[{0}] > Packet not in order", group_id_), kMessageCol_Warning); }
			
			string str_message = string.Format("UdpBuffer_[{0}] > Packet not in order : num_points_collected_ = {1}, packet.ofs_ = {2}, packet.num_points_in_this_frame_ = {3}", group_id_, num_points_collected_, packet.ofs_, packet.num_points_in_this_frame_);
			SjUtil.LogError(str_message, true);
			
			if( (num_points_collected_ < packet.ofs_) && ((int)(packet.num_points_in_this_frame_ * 0.9f) < num_points_collected_) ){
				i_in_--;
				lock(sync_) { SetFrame(); }
			}
			
			StateToWaitZero();
			
			// b_Error_ = true;
			return;
		}
		
		num_points_collected_ += packet.positions_.Count;
		if(max_points_ < num_points_collected_){
			string str_message = string.Format("UdpBuffer_[{0}] > over max points", group_id_);
			SjUtil.LogError(str_message, true);
			SetCriticalMessage(str_message, kMessageCol_Error);
			b_Error_ = true;
			return;
		}
		
		packet_datas_[i_in_] = packet;
		
		if(packet.num_points_in_this_frame_ <= num_points_collected_){
			lock(sync_) { SetFrame(); }
			StateToWaitZero();
			return;
		}else{
			i_in_++;
			if(packet_datas_.Length <= i_in_){
				string str_message = string.Format("UdpBuffer_[{0}] > over max packet length", group_id_);
				SjUtil.LogError(str_message, true);
				SetCriticalMessage(str_message, kMessageCol_Error);
				b_Error_ = true;
				return;
			}
		}
	}
	
	void SetFrame(){
		point_data_ = new Vector3[num_points_collected_];
		
		for(int i = 0; i <= i_in_; i++){
			for(int j = 0; j < packet_datas_[i].positions_.Count; j++){
				float x = packet_datas_[i].positions_[j].x;
				float y = packet_datas_[i].positions_[j].y;
				float z = packet_datas_[i].positions_[j].z;
				
				point_data_[packet_datas_[i].ofs_ + j] = new Vector3(x, y, z);
			}
		}
	}
	
	public void StateToWaitZero(){
		state_ = State.kWaitZero;
		i_in_ = 0;
		num_points_collected_ = 0;
	}
	
	private bool IsMessageValid(string[] block){
		if(	(9 <= block.Length)				&&
			((block.Length - 6) % 3 == 0)	&&
			(block[0] == "/pos")			&&
			((int)System.Convert.ToInt32(block[1]) == group_id_)
			)
		{
			return true;
		}else{
			return false;
		}
	}
	
	public void MovePointData(out Vector3[] point_data){
		lock(sync_){
			point_data = point_data_;
			point_data_ = null;
		}
	}
	
	public void PassParamToUseInEnqueue(bool b_invert_x, bool b_limit_x, float min_x, float max_x, bool b_limit_z, float min_z, float max_z, Vector3 p_far_away, float rot_deg, bool b_disp_msg_packet_not_in_order){
		lock(sync_){
			b_invert_x_						= b_invert_x;
			b_limit_x_						= b_limit_x;
			min_x_							= min_x;
			max_x_							= max_x;
			b_limit_z_						= b_limit_z;
			min_z_							= min_z;
			max_z_							= max_z;
			p_far_away_						= p_far_away;
			rot_deg_						= rot_deg;
			b_disp_msg_packet_not_in_order_ = b_disp_msg_packet_not_in_order;
		}
	}
};


/**************************************************
**************************************************/
// sealed public class PositionBufferUdp : IDisposable
sealed public class PositionBufferUdp : IDisposable
{
	/****************************************
	****************************************/
	/********************
	********************/
	enum State{
		kNoUdp,
		kCommunication,
	};
	State state = State.kNoUdp;
	float t_state_from = 0;
	
	/********************
	********************/
	public NativeArray<Vector3> Positions => arrays_.p.Reinterpret<Vector3>();
	public NativeArray<Matrix4x4> Matrices => arrays_.m.Reinterpret<Matrix4x4>();

	(NativeArray<float3> p, NativeArray<Matrix4x4> m) arrays_;
	int num_points_in_this_frame_ = 0;
	
	Quaternion rotation_;
	
	bool b_update_lidar_ = false;
	
	Vector3 last_ofs_ = new Vector3(0, 0, 0);
	
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
	********************/
	MessageManager message_manager_;
	
	/********************
	need to be locked when touch the params below.
	********************/
	volatile bool b_thread_running_ = true;
	UdpBuffer udp_buffer_;
	
	/****************************************
	****************************************/
	
	/******************************
	******************************/
	public PositionBufferUdp(int group_id, int max_points, int IN_PORT_WAVE, MessageManager message_manager)
	{
		/********************
		********************/
		arrays_ = ( new NativeArray<float3>(max_points, Allocator.Persistent), new NativeArray<Matrix4x4>(max_points, Allocator.Persistent) );
		
		/********************
		https://docs.microsoft.com/ja-jp/dotnet/api/system.net.sockets.socket.receivetimeout?view=net-6.0
			udp_.Client.ReceiveTimeout = 0;
			udp_.Client.ReceiveTimeout = -1;
		で無期限になる。
		-> thread_側のLoopをflagで抜けるので、NG
		********************/
		udp_ = new UdpClient(IN_PORT_WAVE);
		udp_.Client.ReceiveTimeout = 1000;
		
		udp_buffer_ = new UdpBuffer(group_id, max_points);
		
		thread_ = new Thread(new ThreadStart(ThreadMethod));
		thread_.Start(); 
		
		/********************
		********************/
		rotation_ = Quaternion.Euler(0.0f, 0.0f, 0.0f);
		
		/********************
		********************/
		message_manager_ = message_manager;
	}
	
	/******************************
	******************************/
	public void Dispose()
	{
		lock(sync_)	{ b_thread_running_ = false; }
		// thread_.Join();
		
		if (arrays_.p.IsCreated) arrays_.p.Dispose();
		if (arrays_.m.IsCreated) arrays_.m.Dispose();
		
		string str_message = string.Format("PositionBufferUdp_[{0}] > PositionBufferUdp Disposed.", udp_buffer_.GetGroupId());
		Debug.Log(str_message);
	}
	
	/******************************
	******************************/
	bool IsSoundSyncUpdated(SoundSyncFromUdp sound_sync_from_udp){
		if( (sound_sync_from_udp != null) && (sound_sync_from_udp.IsUpdated()) )	return true;
		else																		return false;
	}
	
	/******************************
	******************************/
	bool IsAddSoundyncTrans( bool b_apply_sound, SoundSyncFromUdp sound_sync_from_udp, int anim_type, bool b_update_lidar ){
		if(	(b_apply_sound) &&
			(IsSoundSyncUpdated(sound_sync_from_udp)) && 
			(anim_type != (int)Global.SoundSyncType.kColor)
		){
			return true;
			
		}else if(	(b_apply_sound) &&
					(!IsSoundSyncUpdated(sound_sync_from_udp)) && 
					(anim_type != (int)Global.SoundSyncType.kColor) &&
					(b_update_lidar)
		){
			return true;
			
		}else{
			return false;
			
		}
	}
	
	/******************************
	******************************/
	bool IsCopyPtoM( bool b_apply_sound, SoundSyncFromUdp sound_sync_from_udp, int anim_type, bool b_update_lidar ){
		if( !b_apply_sound && b_update_lidar )																										return true;
		else if( b_apply_sound && IsSoundSyncUpdated(sound_sync_from_udp) && (anim_type == (int)Global.SoundSyncType.kColor) && b_update_lidar )	return true;
		else if( b_apply_sound && !IsSoundSyncUpdated(sound_sync_from_udp) && (anim_type == (int)Global.SoundSyncType.kColor) && b_update_lidar )	return true;
		else																																		return false;
	}
	
	/******************************
	******************************/
	void DispMessageIfAny(){
		string str;
		Color col;
		if( udp_buffer_.GetDispMessage(out str, out col) ){
			message_manager_.DispMessage(str, col);
		}
	}
	
	/******************************
	******************************/
	void DispCriticalMessageIfAny(){
		string str;
		Color col;
		if( udp_buffer_.GetCriticalMessage(out str, out col) ){
			message_manager_.DispCriticalMessage(str, col);
		}
	}
	
	/******************************
	******************************/
	public void Update(	Vector3 ofs, float scale_lidar, bool b_apply_sound, SoundSyncFromUdp sound_sync_from_udp, int anim_coord, int anim_type, float sound_sync_size_l, float sound_sync_size_h, bool b_enable_base_wave,
						bool b_invert_x, bool b_limit_x, float min_x, float max_x, bool b_limit_z, float min_z, float max_z, Vector3 p_far_away, float rot_deg, bool b_disp_msg_packet_not_in_order)
	{
		/********************
		********************/
		DispMessageIfAny();
		DispCriticalMessageIfAny();
		
		/********************
		********************/
		Vector3[] point_data;
		udp_buffer_.MovePointData(out point_data); 																												// 向こう側で lock(sync_)
		udp_buffer_.PassParamToUseInEnqueue(b_invert_x, b_limit_x, min_x, max_x, b_limit_z, min_z, max_z, p_far_away, rot_deg, b_disp_msg_packet_not_in_order);	// 向こう側で lock(sync_)
		
		b_update_lidar_ = false;
		
		StateChart( point_data != null );
		
		if(point_data != null){
			num_points_in_this_frame_ = point_data.Length;
			
			for(int i = 0; i < point_data.Length; i++){
				float x = point_data[i].x;
				
				float y = point_data[i].y;
				if(!b_enable_base_wave) y = 0;
				
				float z = point_data[i].z;
				
				var p = math.float3(x, y, z);
				arrays_.p[i] = p; // save base.
			}
			
			b_update_lidar_ = true;
		}
		
		if(ofs != last_ofs_) { b_update_lidar_ = true; }
		last_ofs_ = ofs;
		
		/********************
		********************/
		if( IsAddSoundyncTrans( b_apply_sound, sound_sync_from_udp, anim_type, b_update_lidar_ ) ){
			for(int i = 0; i < num_points_in_this_frame_; i++){
				Vector3 scale_ = new Vector3(scale_lidar, scale_lidar, scale_lidar);
				
				float x = arrays_.p[i].x + ofs.x;
				float y = arrays_.p[i].y + ofs.y;
				float z = arrays_.p[i].z + ofs.z;
				
				float wave_val = sound_sync_from_udp.GetWaveVal(x, y, z, anim_coord, anim_type);
				
				if(anim_type == (int)Global.SoundSyncType.kSync_y)		{ y += wave_val; }
				else if(anim_type == (int)Global.SoundSyncType.kSync_z)	{ z -= wave_val; }
				else if(anim_type == (int)Global.SoundSyncType.kSize)	{
					float f_scale = SjUtil.MyMap(wave_val, 0, 1, sound_sync_size_l, sound_sync_size_h);
					scale_ = new Vector3(f_scale, f_scale, f_scale);
				}
				
				arrays_.m[i] = Matrix4x4.TRS(new Vector3(x, y, z), rotation_, scale_); // p はbaseなので、no touch.
			}
		}else if( IsCopyPtoM( b_apply_sound, sound_sync_from_udp, anim_type, b_update_lidar_ ) ){
			Vector3 scale_ = new Vector3(scale_lidar, scale_lidar, scale_lidar);
			
			for(int i = 0; i < num_points_in_this_frame_; i++){
				float x = arrays_.p[i].x + ofs.x;
				float y = arrays_.p[i].y + ofs.y;
				float z = arrays_.p[i].z + ofs.z;
				
				arrays_.m[i] = Matrix4x4.TRS(new Vector3(x, y, z), rotation_, scale_);
			}
		}
	}
	
	/******************************
	******************************/
	void StateChart(bool b_point_data_completed){
		switch(state){
			case State.kNoUdp:
				if(b_point_data_completed){
					state = State.kCommunication;
					t_state_from = Time.time;
				}
				break;
				
			case State.kCommunication:
				if(b_point_data_completed) { t_state_from = Time.time; }
				
				if(3.0 < Time.time - t_state_from){
					state = State.kNoUdp;
					num_points_in_this_frame_ = 0;
					udp_buffer_.StateToWaitZero();
					
					string str_message = string.Format("PositionBufferUdp_[{0}] > Udp Comminication Stopped.", udp_buffer_.GetGroupId());
					Debug.Log(str_message);
				}
				break;
		}
	}
	
	/******************************
	******************************/
	public int GetNumPointsInThisFrame(){
		return num_points_in_this_frame_;
	}
	/******************************
	******************************/
	public bool IsUpdated(){
		return b_update_lidar_;
	}
	
	/******************************
	******************************/
	private void ThreadMethod()
	{
		bool b_thread_running = true;
		int group_id;
		lock(sync_)	{ group_id = udp_buffer_.GetGroupId(); }
		
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
				
				// lock(sync_) { udp_buffer_.Enqueue(str_message); }
				udp_buffer_.Enqueue(str_message); // 向こう側でlock
				
			}catch(SocketException){
				/********************
				UdpClient投げる例外
					https://msdn.microsoft.com/ja-jp/library/82dxxas0(v=vs.110).aspx
				********************/
				// Debug.Log("Receive udp_ timeout : No client??");
			}catch (ObjectDisposedException e){
				string str_message = string.Format("PositionBufferUdp_[{0}] > !!! udp_receive : thread_ : ObjectDisposedException !!! : ", group_id);
				Debug.Log(str_message + e.ToString());
			}catch (Exception e ) {
				string str_message = string.Format("PositionBufferUdp_[{0}] > !!! udp_receive : thread_ : Exception !!! : ", group_id);
				Debug.Log(str_message + e.ToString());
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
				string str_message = string.Format("PositionBufferUdp_[{0}] > SocketException : udp_ receive : OnDestroy : udp_.Close()", group_id);
				Debug.Log(str_message);
			}
		}
		
		/********************
		********************/
		{
			string str_message = string.Format("PositionBufferUdp_[{0}] > Exit thread_ : udp_receive", group_id);
			Debug.Log(str_message);
		}
	}
}
