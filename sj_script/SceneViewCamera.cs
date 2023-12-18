/************************************************************
************************************************************/
using UnityEngine;
using UnityEngine.EventSystems;


/************************************************************
************************************************************/
[RequireComponent(typeof(Camera))]
public class SceneViewCamera : MonoBehaviour
{
	/****************************************
	****************************************/
	/********************
	********************/
	[SerializeField] RenderPointColud render_point_cloud;
	[SerializeField] Global global_;
	
	/********************
	********************/
	enum OperationState{
		kNormal_,
		kMoveXy_,
	};
	OperationState operation_state_ = OperationState.kNormal_;
	
	/********************
	********************/
	Camera my_camera;
	PhysicsRaycaster _PhysicsRaycaster;
	
	private string label = "";
	
	[SerializeField] Vector3 rot_center = new Vector3(0, 0, 15.0f);
	Vector3 rot_center_init = new Vector3(0, 0, 15.0f);
	
	/* */
	[SerializeField, Range(1f, 30f)]
	private float wheelSpeed = 10f;
	
	[SerializeField, Range(0.01f, 0.5f)]
	private float moveSpeed = 0.1f;
	
	[SerializeField, Range(0.1f, 10f)]
	private float rotateSpeed = 0.3f;
	
	/* */
	private Vector3 preMousePos;
	
	/* */
	private Quaternion rot_org;
	private Vector3 pos_org;
	
	/********************
	********************/
	[Header("cursor")]
	[SerializeField] Texture2D handCursor;
	/********************
	********************/
	bool b_option = false;
	bool b_command = false;
	bool b_shift = false;
	
	/********************
	********************/
	bool b_key_q = false;
	bool b_key_w = false;
	
	
	/****************************************
	****************************************/
	
	/******************************
	******************************/
	void Awake() { 
		my_camera = GetComponent<Camera>();
		_PhysicsRaycaster = my_camera.GetComponent<PhysicsRaycaster>();
		
		rot_org = transform.rotation;
		pos_org = transform.position;
		
		
	}
	
	/******************************
	******************************/
	private void OperationStateChart(){
		switch(operation_state_){
			case OperationState.kNormal_:
				if(b_key_q) { operation_state_ = OperationState.kMoveXy_; Cursor.SetCursor(handCursor, new Vector2(handCursor.width / 2, handCursor.height / 2), CursorMode.Auto); }
				break;
				
			case OperationState.kMoveXy_:
				if( (b_key_w) || (b_option) || (b_command) || (b_shift) ) { operation_state_ = OperationState.kNormal_; Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }
				break;
		}
		
		b_key_q = false;
		b_key_w = false;
	}
	
	/******************************
	■KeyCode
		https://docs.unity3d.com/ja/2019.4/ScriptReference/KeyCode.html
	******************************/
	private void UpdateOptionalKeys(){
		if(Input.GetKey(KeyCode.LeftAlt))		{ b_option = true; }
		else									{ b_option = false; }
		
		if(Input.GetKey(KeyCode.LeftCommand))	{ b_command = true; }
		else									{ b_command = false; }
		
		if(Input.GetKey(KeyCode.LeftShift))		{ b_shift = true; }
		else									{ b_shift = false; }
	}
	
	/******************************
	******************************/
	private void UpdateOperationStateChangeKey(){
		if(Input.GetKeyDown(KeyCode.Q))			{ b_key_q = true; }
		if(Input.GetKeyDown(KeyCode.W))			{ b_key_w = true; }
	}
	
	/******************************
	******************************/
	private void UpdateRotationCenter(){
		/*
		if(Input.GetMouseButtonDown(0)){
			Ray ray = my_camera.ScreenPointToRay(Input.mousePosition);
			if(_PhysicsRaycaster != null) _PhysicsRaycaster.enabled = true; // 一時的にon
			
			RaycastHit hit;
			if(Physics.Raycast(ray,out hit, Mathf.Infinity)){
				// if (hit.collider.tag == "Player"){
				if (hit.collider.gameObject.layer == LayerMask.NameToLayer("sj_test")){
					rot_center = hit.transform.position;
					Debug.Log("rot_center : changed");
				}
			}
			if(_PhysicsRaycaster != null) _PhysicsRaycaster.enabled = false; // 処理 終わったらoff
		}
		*/
		
		if(render_point_cloud != null)	rot_center = render_point_cloud.GetSoundCenter();
		
		label = string.Format("( {0:0.00}, {1:0.00}, {2:0.00} )", rot_center.x, rot_center.y, rot_center.z);
		if(operation_state_ == OperationState.kMoveXy_) label += " : <->";
	}
	
	/******************************
	******************************/
	private void UpdateOperationSpeed(){
		wheelSpeed = global_.param_cam_move_.wheelSpeed_;
		moveSpeed = global_.param_cam_move_.moveSpeed_;
		rotateSpeed = global_.param_cam_move_.rotateSpeed_;
	}
	
	/******************************
	******************************/
	private void Update()
	{
		/********************
		********************/
		UpdateOptionalKeys();
		UpdateOperationStateChangeKey();
		
		OperationStateChart();
		
		/********************
		********************/
		if(Input.GetKeyDown(KeyCode.R)) ResetTransform();
		if(Input.GetKeyDown(KeyCode.F))	transform.LookAt(rot_center, Vector3.up);
		
		// for test
		/*
		if(Input.GetKeyDown(KeyCode.T)){
			float angle = Vector3.SignedAngle(new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
			Debug.Log(angle);
			
			transform.Rotate(new Vector3(0, 0, 30), Space.Self); 
		}
		*/
		
		/********************
		********************/
		UpdateRotationCenter();
		UpdateOperationSpeed();
		
		/********************
		********************/
		MouseUpdate();
		
		return;
	}
	
	/******************************
	******************************/
	private void ResetTransform(){
		transform.position = pos_org;
		transform.rotation = rot_org;
		
		if(render_point_cloud != null)	rot_center = render_point_cloud.GetSoundCenter();
		else							rot_center = rot_center_init;
		
	}
	
	/******************************
	■How to get MouseScroll Input?
		https://stackoverflow.com/questions/5675472/how-to-get-mousescroll-input
			-	Hey Friend, Instead of Input.GetAxis you may use Input.GetAxisRaw. The value for GetAxis is smoothed and is in range -1 .. 1 , however GetAxisRaw is -1 or 0 or 1.
	
	■Input.GetAxis
		https://docs.unity3d.com/ScriptReference/Input.GetAxis.html
			-	The value will be in the range -1...1 for keyboard and joystick input devices.
	******************************/
	private void MouseUpdate()
	{
		float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
		if (scrollWheel != 0.0f)	MouseWheel(scrollWheel);

		if( Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) ){
			preMousePos = Input.mousePosition;
		}

		MouseDrag(Input.mousePosition);
	}

	/******************************
	******************************/
	private void MouseWheel(float delta)
	{
		// if(EventSystem.current.IsPointerOverGameObject())	return; // GUI上にpointerがある時は 何もせずreturn;
		
		transform.position += transform.forward * delta * wheelSpeed;
		return;
	}

	/******************************
	What is the kEpsilon field on Vectors?
		https://answers.unity.com/questions/580108/what-is-the-kepsilon-field-on-vectors.html
	******************************/
	private void MouseDrag(Vector3 mousePos)
	{
		Vector3 diff = mousePos - preMousePos;
		if(diff.magnitude < Vector3.kEpsilon) return;
		
		if (Input.GetMouseButton(0)){
			if( operation_state_ == OperationState.kMoveXy_ ){
				transform.Translate(-diff * moveSpeed);
				preMousePos = mousePos;
			}else{
				if(b_option)		{ CameraRotate(new Vector2(-diff.y, diff.x) * rotateSpeed);	preMousePos = mousePos; }
				else if(b_command)	{ CameraRotate_z();											preMousePos = mousePos; }
			}
		}
	}
	
	/******************************
	******************************/
	public void CameraRotate(Vector2 angle)
	{
		// transform.Rotate(new Vector3(angle.x, angle.y, 0), Space.Self); // transform.Rotate(new Vector3(angle.x, angle.y, 0), Space.World);
		
		/********************
		********************/
		transform.RotateAround(rot_center, transform.right, angle.x);
		transform.RotateAround(rot_center, transform.up, angle.y); // transform.RotateAround(transform.position, Vector3.up, angle.y);
		
		/********************
		********************/
		float _angle = Vector3.Angle(transform.forward, Vector3.up);
		const float _thresh = 25.0f;
		if( (_thresh < _angle)/* 上向 でない */ && (_angle < 180.0f - _thresh)/* 下向 でない */ ){	// camが真下 or 真上 を向いている時は、水平補正しない
			Plane plane = new Plane(transform.forward, transform.position);
			Vector3 pos_up = transform.position + new Vector3(0, 1, 0);
			Vector3 pos_up_on_plane = plane.ClosestPointOnPlane(pos_up);
			
			if(pos_up_on_plane != transform.position){
				Vector3 v_up = (pos_up_on_plane - transform.position).normalized;
				
				float diff_angle = Vector3.SignedAngle(transform.up, v_up, transform.forward); // public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis);
				// transform.Rotate(new Vector3(0, 0, diff_angle), Space.Self);	// direct
				transform.Rotate(new Vector3(0, 0, diff_angle * 0.35f), Space.Self);	// lerp
				/********************
				↑	Lerpさせると、自然な動きになるが、
					本関数に入ってきた時のみ、水平補正が入るので、
						cf.
							if(diff.magnitude < Vector3.kEpsilon) return;
							if(b_option)		CameraRotate(new Vector2(-diff.y, diff.x) * rotateSpeed);
					いつも少し、足りない程度しか補正が働かないことになる。
					しかし、完全に水平でなくても見た目的に問題ないのであれば、ここに示したlerpでもいいかもしれない。
				********************/
			}
		}
	}
	
	/******************************
	■【Unity入門】スクリプトで画面サイズを取得・設定しよう
		https://www.sejuku.net/blog/83691
	
	■【Unity】2つのベクトル間の角度を求める
		https://nekojara.city/unity-vector-angle
	******************************/
	public void CameraRotate_z()
	{
		Vector3 center = new Vector3(Screen.width/2, Screen.height/2, 0);
		Vector3 v_mouse		= Input.mousePosition - center;
		Vector3 v_PreMouse	= preMousePos - center;
		
		if( (v_mouse.magnitude == 0) || (v_PreMouse.magnitude == 0) ) return;
		
		float angle = Vector3.SignedAngle(v_PreMouse, v_mouse, new Vector3(0, 0, 1)); // public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis);
		
		transform.Rotate(new Vector3(0, 0, -angle), Space.Self); // transform.RotateAround(transform.position, transform.forward, angle);
	}
	
	/******************************
	******************************/
	void OnGUI()
	{
		GUI.color = Color.white;
		GUI.skin.label.fontSize = 30;
		
		// GUI.Label(new Rect(15, 20, 500, 50), label);
	}
	
}
