/************************************************************
************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;

using System.IO;
using System; // need this to use DateTime

using System.Text.RegularExpressions; // to use Regex.Replace


/************************************************************
************************************************************/

public class SjUtil
{
	/****************************************
	****************************************/
	static bool b_CriticalError = false;
	
	/****************************************
	****************************************/
	
	/******************************
	******************************/
	static public float MyMap(float x, float x0, float x1, float y0, float y1){
		if(x0 == x1){
			return y0;
		}else if(x < math.min(x0, x1)){
			if(x0 < x1)	return y0;
			else		return y1;
		}else if(math.max(x0, x1) < x){
			if(x0 < x1)	return y1;
			else		return y0;
		}else{
			float tan = (y1 - y0) / (x1 - x0);
			float y = tan * (x - x0) + y0;
			return y;
		}
	}
	
	/******************************
	******************************/
	static public bool IsCriticalError(){
		return b_CriticalError;
	}
	
	/******************************
	******************************/
	static public void MyQuitApp(){
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
#else
		Application.Quit();//ゲームプレイ終了
#endif
	
		b_CriticalError = true;
	}
	
	/******************************
	******************************/
	static public void LogError(string str_message, bool is_append = false){
		/********************
		********************/
		Debug.Log(str_message);
		
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
		string dirPath = Path.Combine (getPathOf_Assets(), @"sj_Log/");
		if(!Directory.Exists (dirPath)){
			try{
				Directory.CreateDirectory (dirPath);
			} catch(Exception e) {
				Debug.Log (e.Message);
				MyQuitApp();
				b_CriticalError = true;
				return;
			}
		}
		
		/********************
		usingステートメントを使えるのはIDisposableインタフェースを継承しているクラスに限りますが、File.Openの戻り値はFileStreamクラスでこのクラスはStreamクラスを継承して作成されており、
		StreamクラスはIDisposableインタフェースを継承しているのでusingステートメントを使用する事が出来ます。
		
		usingステートメントを使用すると、例外が発生してもDisposeが実行されます。
		********************/
		string filePath = Path.Combine (dirPath, "Log.txt");
		
		// write
		try{
			using( var fs_w = new StreamWriter( filePath, is_append, System.Text.Encoding.GetEncoding("UTF-8") ) ){
				/*
				int		data_0 = 10;
				float	data_1 = 99.4f;
				string	str_message = "Nobuhiro.Saijo";
				string str_Log = string.Format("> {0}\n{1}, {2}, {3}\n\n", DateTime.Now.ToString(), data_0, data_1, str_message);
				*/
				
				string str_total_log = string.Format("> {0}\n{1}\n\n", DateTime.Now.ToString(), str_message);
				
				fs_w.Write(str_total_log);
			}
		}catch(Exception e){
			Debug.Log(e.Message);
		}
	}
	
	/******************************
	******************************/
	public static string getPathOf_Assets(){
		string str_Path = Application.dataPath;
		
		if (Application.platform == RuntimePlatform.OSXEditor) {
			// str_Path = (プロジェクトフォルダ)/Assets
			str_Path += @"/";
		}else if (Application.platform == RuntimePlatform.OSXPlayer) {
			// str_Path = (プロジェクトフォルダ)/xxx.app/Contents
			str_Path += @"/../../Assets/";
		}else if (Application.platform == RuntimePlatform.WindowsEditor) {
			// str_Path = (プロジェクトフォルダ)/Assets
			str_Path += @"/";
		}else if (Application.platform == RuntimePlatform.WindowsPlayer) {
			/********************
			windows buildでは、empty directoryを保存先としてbuildしないと、Errorが出てしまう。
				(プロジェクトフォルダ)/exe
			と言うフォルダを作成して、ここを保存先とした場合、同フォルダ内に、
				"実行ファイル名_data"
			と言う名前のフォルダが生成される。
			この場合、
				Application.dataPath
			は、このフォルダを指す。
				e.g.
				c:\nobuhiro/source/Unity/myWorks/works/(プロジェクトフォルダ)/exe/mgl_getPath_Data
			********************/
			str_Path += @"/../../Assets/";
		}else{
			// please check by yourself.
		}
		
		return str_Path;
	}
}
