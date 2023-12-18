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
	// static Object sync = new Object();
	static UnityEngine.Object sync = new UnityEngine.Object();
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
public class SoundSyncFromUdp
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
	float sound_wave_h_;
	float sound_wave_space_;
	NativeArray<float> art_sin_;
	int num_samples_ = 0;
	bool b_1st_data_ = true;
	bool b_udp_ = false;
	
	Vector3 center_;
	public NativeArray<Matrix4x4> m_sound_center_;
	Quaternion rotation_;
	
	bool b_updated_ = false;
	
	/********************
	********************/
	// static Object sync = new Object();
	UnityEngine.Object sync = new UnityEngine.Object();

	/********************
	********************/
	// int IN_PORT = 12345;
	UdpClient udp_ = null; // need to be "static" to be touched in thread_.
	Thread thread_;
	
	/********************
	need to be locked when touch the params below.
	********************/
	volatile bool b_thread_running_ = true;
	string str_message_;
	
	
	/****************************************
	****************************************/
	
	/******************************
	******************************/
	public SoundSyncFromUdp(int IN_PORT)
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
		
		/********************
		********************/
		rotation_ = Quaternion.Euler(0.0f, 0.0f, 0.0f);
		
		center_ = new Vector3(0.0f, 0.0f, 0.0f);
		m_sound_center_ = new NativeArray<Matrix4x4>(1, Allocator.Persistent);
	}
	
	/******************************
	******************************/
	public void Dispose()
	{
		lock(sync)	{ b_thread_running_ = false; }
		// thread_.Join();
		
		if (art_sin_.IsCreated) art_sin_.Dispose();
		if (m_sound_center_.IsCreated) m_sound_center_.Dispose();
		
		Debug.Log("SoundSyncFromUdp > Disposed");
	}
	
	/******************************
	******************************/
	void InitArray(int num_samples)
	{
		num_samples_ = num_samples;
		
		if (art_sin_.IsCreated) art_sin_.Dispose();
		art_sin_ = new NativeArray<float>(num_samples_, Allocator.Persistent);
		
		string str_message = String.Format("SoundSyncFromUdp > num_samples_ = {0}", num_samples_);
		Debug.Log(str_message);
	}
	
	/******************************
	******************************/
	public bool IsUpdated(){
		return b_updated_;
	}
	
	/******************************
	******************************/
	public void Update(float scale_sound_center = 0.5f){
		/********************
		********************/
		b_updated_ = false;
		
		/********************
		********************/
		string str_message;
		bool b_udp;
		
		lock(sync){
			str_message = str_message_;
			b_udp = b_udp_;
			b_udp_ = false;
		}
		
		/********************
		********************/
		StateChart(b_udp);
		
		/********************
		********************/
		if(b_udp){
			string[] val = str_message.Split(',', StringSplitOptions.None); // 1文字の場合は、シングルクォーテーション
			if (IsMessageValid(val) ){
				ExtractCenterFromMessage(val, scale_sound_center);
				
				sound_wave_h_		= (float)System.Convert.ToDouble(val[4]); // ToInt32, ToDouble, ToString, ...
				sound_wave_space_	= (float)System.Convert.ToDouble(val[5]); // ToInt32, ToDouble, ToString, ...
				
				ExtractArtSinFromMessage(val);
				
				b_1st_data_ = false;
				b_updated_ = true;
			}
		}
	}
	
	/******************************
	******************************/
	void StateChart(bool b_udp){
		switch(state){
			case State.kNoUdp:
				if(b_udp){
					state = State.kCommunication;
					t_state_from = Time.time;
				}
				break;
				
			case State.kCommunication:
				if(b_udp) { t_state_from = Time.time; }
				
				if(3.0 < Time.time - t_state_from){
					state = State.kNoUdp;
					Debug.Log("SoundSyncFromUdp > Udp Comminication Stopped.");
				}
				break;
		}
	}
	
	/******************************
	******************************/
	void ExtractArtSinFromMessage(string[] val){
		int ofs = 6;
		
		int num_samples = val.Length - ofs;
		
		if(b_1st_data_)							{ InitArray(num_samples); }
		else if(num_samples != num_samples_)	{ InitArray(num_samples); }
		
		for(int i = 0; i < num_samples_; i++){
			art_sin_[i] = (float)System.Convert.ToDouble(val[i + ofs]); // ToInt32, ToDouble, ToString, ...
		}
	}
	
	/******************************
	******************************/
	void ExtractCenterFromMessage(string[] val, float scale_sound_center){
		float c_x = (float)System.Convert.ToDouble(val[1]); // ToInt32, ToDouble, ToString, ...
		float c_y = (float)System.Convert.ToDouble(val[2]); // ToInt32, ToDouble, ToString, ...
		float c_z = (float)System.Convert.ToDouble(val[3]); // ToInt32, ToDouble, ToString, ...
		center_ = new Vector3(c_x, c_y, c_z);
		
		Vector3 scale = new Vector3(scale_sound_center, scale_sound_center, scale_sound_center);
		
		m_sound_center_[0] = Matrix4x4.TRS(center_, rotation_, scale);
	}
	
	/******************************
	******************************/
	public Vector3 GetSoundCenter(){
		return center_;
	}
	
	/******************************
	******************************/
	public float GetWaveVal(float x, float y, float z, int anim_coord, int anim_type){
		/********************
		********************/
		if( (num_samples_ == 0) || (state == State.kNoUdp) ) return 0;
		
		/********************
		********************/
		float d;
		if(anim_coord == (int)Global.SoundSyncCoord.kX)				d = math.abs(x - center_.x) * sound_wave_space_;
		else if(anim_coord == (int)Global.SoundSyncCoord.kZ)		d = math.abs(z - center_.z) * sound_wave_space_;
		else if(anim_coord == (int)Global.SoundSyncCoord.kCircle)	d = math.sqrt( (x - center_.x) * (x - center_.x) + (z - center_.z) * (z - center_.z) ) * sound_wave_space_;
		else /* Global.SoundSyncCoord.kSphere */					d = math.sqrt( (x - center_.x) * (x - center_.x) + (y - center_.y) * (y - center_.y) + (z - center_.z) * (z - center_.z) ) * sound_wave_space_;
		
		/********************
		********************/
		float a = (float)(num_samples_ - 1) / 2;
		int n = (int)(d / a);
		
		float D;
		if(n % 2 == 0)	{ D =  d - a * n; }
		else			{ D = -d + (n + 1) * a; }
		
		/********************
		********************/
		int id;
		id = (int)(a - D);
		
		/*
		int id;
		if(anim_coord == (int)Global.SoundSyncCoord.kZ){
			if(0 <= z - center_.z)	{ id = (int)((float)num_samples_ / 2 + d); }
			else					{ id = (int)((float)num_samples_ / 2 - d); }
		}else{
			if(0 <= x - center_.x)	{ id = (int)((float)num_samples_ / 2 + d); }
			else					{ id = (int)((float)num_samples_ / 2 - d); }
		}
		*/
		
		if(id < 0)				id = 0;
		if(num_samples_ <= id)	id = num_samples_ - 1;
		
		/********************
		********************/
		float height;
		if( (anim_type == (int)Global.SoundSyncType.kSize) || (anim_type == (int)Global.SoundSyncType.kColor) )	{ height = 1.0f; }
		else																									{ height = sound_wave_h_; }
		
		return art_sin_[id] * height;
	}
	
	/******************************
	******************************/
	private bool IsMessageValid(string[] val){
		if(	(10 <= val.Length) && (val[0] == "/SoundWave") ){
			return true;
		}else{
			return false;
		}
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
				
				lock(sync) { Enqueue(str_message); }
				
			}catch(SocketException){
				/********************
				UdpClient投げる例外
					https://msdn.microsoft.com/ja-jp/library/82dxxas0(v=vs.110).aspx
				********************/
				// Debug.Log("Receive udp_ timeout : No client??");
			}catch (ObjectDisposedException e){
				Debug.Log("SoundSyncFromUdp > !!! udp_receive : thread_ : ObjectDisposedException !!! : " + e.ToString());
			}catch (Exception e ) {
				Debug.Log("SoundSyncFromUdp > !!! udp_receive : thread_ : Exception !!! : " + e.ToString());
			}
			
			// Thread.Sleep(1);	// byte[] message = udp_.Receive(ref remoteEP); でwait入るので、ここは不要
								// 逆にmessageが溜まっている時は、waitナシで回る
			
			lock(sync) { b_thread_running = b_thread_running_; }
		}
		
		/********************
		********************/
		if(udp_ != null){
			try{
				udp_.Close();
				udp_ = null;
			}catch(SocketException){
				Debug.Log("SoundSyncFromUdp > SocketException : udp_ receive : OnDestroy : udp_.Close()");
			}
		}
		
		/********************
		********************/
		Debug.Log("SoundSyncFromUdp > Exit thread_ : udp_receive");
	}
}
