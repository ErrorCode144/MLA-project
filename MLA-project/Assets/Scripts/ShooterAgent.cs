using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 標的に向かって弾を撃つエージェント
/// 放った弾が命中すると報酬を得る
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DecisionRequester))]
public class ShooterAgent : Agent {
	// ターゲット
	[SerializeField] private Transform Target = null;
	// 回転の速さ
	[SerializeField] private float RotateSpeed = 1F;
	// 移動の速さ
	[SerializeField] private float MoveSpeed = 1F;

	// センサー
	private RayPerceptionSensorComponent3D _sensor = null;	// キャッシュ
	private RayPerceptionOutput _sensorOutput = new();		// 出力先
	private RayPerceptionInput _sensorInput;				// センサーを定義するデータ

	// Heuristicで用いる入力アクション
	private InputAction _turnAction = null;	// 旋回
	private InputAction _moveAction = null;	// 移動
	private InputAction _fireAction = null;	// 弾の発射

	// 剛体
	private Rigidbody _rigidbody = null;

	// 弾
	private Bullet _bullet = null;

	// センサーのレイ毎の報酬の値
	private float[] _sensorThresholds = null;

	// 放った弾が命中したかどうか
	public bool Hit { get; set; } = false;

	/// <summary>
	/// 環境の情報を集め、エージェントのブレインに渡す
	/// </summary>
	/// <param name="sensor">情報を特定の順番で覚える構造体</param>
	public override void CollectObservations(VectorSensor sensor) {
		// エージェントの向き
		sensor.AddObservation(transform.forward);

		// エージェントの位置
		sensor.AddObservation(transform.localPosition);

		// 標的の位置
		sensor.AddObservation(Target.localPosition);
	}

	/// <summary>
	/// エピソード開始時の各オブジェクトの初期状態を設定する
	/// </summary>
	public override void OnEpisodeBegin() {
		if (transform.localPosition.y < 0F) {
			_rigidbody.angularVelocity = Vector3.zero;
			_rigidbody.linearVelocity = Vector3.zero;
			transform.localPosition = new Vector3(0F, 0.5F, 0F);
		}

		Hit = false;

		// ターゲットの初期位置はランダムに決める
		Target.localPosition = new Vector3(
			Random.Range(-8F, 8F),
			0.5F,
			Random.Range(-8F, 8F)
		);

		_bullet.Init();
	}

	/// <summary>
	/// エージェントの行動に基づいて、動作の指定や報酬の設定などを行う
	/// </summary>
	/// <param name="actions">選択された行動</param>
	public override void OnActionReceived(ActionBuffers actions) {
		// 行動を促すため、1ステップ毎に報酬を少し減らす
		AddReward(-0.001F);

		_sensorOutput = RayPerceptionSensor.Perceive(_sensorInput, false);

		for (byte i = 0; i < _sensorThresholds.Length; ++i) {
			if (_sensorOutput.RayOutputs[i].HitTagIndex == 0) {
				AddReward(_sensorThresholds[i]);
			}
		}

		if (Hit) {
			// 大きな報酬を与える
			AddReward(10F);
			// エピソードを終える
			EndEpisode();
		}

		// エージェントがステージから落ちたら
		if (transform.position.y < 0F) {
			// エピソードを終える
			EndEpisode();
		}

		// エージェントの姿勢更新に使うベクトル
		Vector3 moveDirection = Vector3.zero;	// 回転方向
		Vector3 rotateDirection = Vector3.zero;	// 移動方向

		ActionSegment<int> segments = actions.DiscreteActions;
		int rotate = segments[0];		// 回転
		int moveZAxis = segments[1];	// Z軸移動
		int moveXAxis = segments[2];	// X軸移動
		int shoot = segments[3];		// 弾を発射

		/*
		 * rotate =
		 * 0 : 回転しない
		 * 1 : 左回転
		 * 2 : 右回転
		 */
		switch (rotate) {
			case 1:
				rotateDirection = transform.up;
				break;

			case 2:
				rotateDirection = -1F * transform.up;
				break;

			default:
				break;
		}

		/*
		 * moveZAxis =
		 * 0 : 移動しない
		 * 1 : 前進
		 * 2 : 後退
		 */
		switch (moveZAxis) {
			case 1:
				moveDirection += transform.forward;
				break;

			case 2:
				moveDirection += -1F * transform.forward;
				break;

			default:
				break;
		}

		/*
		 * moveXAxis =
		 * 0 : 移動しない
		 * 1 : 右へ平行移動
		 * 2 : 左へ平行移動
		 */
		switch (moveXAxis) {
			case 1:
				moveDirection += transform.right;
				break;

			case 2:
				moveDirection += -1F * transform.right;
				break;

			default:
				break;
		}

		/*
		 * shoot =
		 * 0 : 発射しない
		 * 1 : 発射する
		 */
		if (shoot == 1) {
			_bullet.Fire(transform);
		}

		// 角速度を加える
		_rigidbody.angularVelocity = RotateSpeed * rotateDirection;
		// 速度を加える
		_rigidbody.linearVelocity = MoveSpeed * moveDirection;
	}

	public override void Heuristic(in ActionBuffers actionsOut) {
		ActionSegment<int> actions = actionsOut.DiscreteActions;
		actions.Clear();

		if (_turnAction.WasPerformedThisFrame()) {
			float axis = _turnAction.ReadValue<float>();

			if (axis < 0) {
				actions[0] = 1;
			} else if (axis > 0) {
				actions[0] = 2;
			}
		}

		if (_moveAction.WasPerformedThisFrame()) {
			Vector2 vec = _moveAction.ReadValue<Vector2>();

			if (vec.y > 0) {
				actions[1] = 1;
			} else if (vec.y < 0) {
				actions[1] = 2;
			}

			if (vec.x > 0) {
				actions[2] = 1;
			} else if (vec.x < 0) {
				actions[2] = 2;
			}
		}

		if (_fireAction.WasPerformedThisFrame()) {
			actions[3] = 1;
		}
	}

	/// <summary>
	/// 各コンポーネントの参照を取得
	/// </summary>
	protected override void Awake() {
		_sensor = GetComponent<RayPerceptionSensorComponent3D>();
		_sensorInput = _sensor.GetRayPerceptionInput();

		InputActionMap actionMap = GetComponent<PlayerInput>().currentActionMap;
		_turnAction = actionMap.FindAction("Turn");
		_moveAction = actionMap.FindAction("Move");
		_fireAction = actionMap.FindAction("Fire");

		_rigidbody = GetComponent<Rigidbody>();
		_bullet = GetComponentInChildren<Bullet>();

		_sensorThresholds = new float[]{
			0.006F,				// 真正面
			0.004F, 0.004F,
			0.001F, 0.001F		// 外側
		};
	}
}