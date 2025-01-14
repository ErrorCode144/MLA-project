using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// �W�I�Ɍ������Ēe�����G�[�W�F���g
/// �������e����������ƕ�V�𓾂�
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DecisionRequester))]
public class ShooterAgent : Agent {
	// �^�[�Q�b�g
	[SerializeField] private Transform Target = null;
	// ��]�̑���
	[SerializeField] private float RotateSpeed = 1F;
	// �ړ��̑���
	[SerializeField] private float MoveSpeed = 1F;

	// �Z���T�[
	private RayPerceptionSensorComponent3D _sensor = null;	// �L���b�V��
	private RayPerceptionOutput _sensorOutput = new();		// �o�͐�
	private RayPerceptionInput _sensorInput;				// �Z���T�[���`����f�[�^

	// Heuristic�ŗp������̓A�N�V����
	private InputAction _turnAction = null;	// ����
	private InputAction _moveAction = null;	// �ړ�
	private InputAction _fireAction = null;	// �e�̔���

	// ����
	private Rigidbody _rigidbody = null;

	// �e
	private Bullet _bullet = null;

	// �Z���T�[�̃��C���̕�V�̒l
	private float[] _sensorThresholds = null;

	// �������e�������������ǂ���
	public bool Hit { get; set; } = false;

	/// <summary>
	/// ���̏����W�߁A�G�[�W�F���g�̃u���C���ɓn��
	/// </summary>
	/// <param name="sensor">�������̏��ԂŊo����\����</param>
	public override void CollectObservations(VectorSensor sensor) {
		// �G�[�W�F���g�̌���
		sensor.AddObservation(transform.forward);

		// �G�[�W�F���g�̈ʒu
		sensor.AddObservation(transform.localPosition);

		// �W�I�̈ʒu
		sensor.AddObservation(Target.localPosition);
	}

	/// <summary>
	/// �G�s�\�[�h�J�n���̊e�I�u�W�F�N�g�̏�����Ԃ�ݒ肷��
	/// </summary>
	public override void OnEpisodeBegin() {
		if (transform.localPosition.y < 0F) {
			_rigidbody.angularVelocity = Vector3.zero;
			_rigidbody.linearVelocity = Vector3.zero;
			transform.localPosition = new Vector3(0F, 0.5F, 0F);
		}

		Hit = false;

		// �^�[�Q�b�g�̏����ʒu�̓����_���Ɍ��߂�
		Target.localPosition = new Vector3(
			Random.Range(-8F, 8F),
			0.5F,
			Random.Range(-8F, 8F)
		);

		_bullet.Init();
	}

	/// <summary>
	/// �G�[�W�F���g�̍s���Ɋ�Â��āA����̎w����V�̐ݒ�Ȃǂ��s��
	/// </summary>
	/// <param name="actions">�I�����ꂽ�s��</param>
	public override void OnActionReceived(ActionBuffers actions) {
		// �s���𑣂����߁A1�X�e�b�v���ɕ�V���������炷
		AddReward(-0.001F);

		_sensorOutput = RayPerceptionSensor.Perceive(_sensorInput, false);

		for (byte i = 0; i < _sensorThresholds.Length; ++i) {
			if (_sensorOutput.RayOutputs[i].HitTagIndex == 0) {
				AddReward(_sensorThresholds[i]);
			}
		}

		if (Hit) {
			// �傫�ȕ�V��^����
			AddReward(10F);
			// �G�s�\�[�h���I����
			EndEpisode();
		}

		// �G�[�W�F���g���X�e�[�W���痎������
		if (transform.position.y < 0F) {
			// �G�s�\�[�h���I����
			EndEpisode();
		}

		// �G�[�W�F���g�̎p���X�V�Ɏg���x�N�g��
		Vector3 moveDirection = Vector3.zero;	// ��]����
		Vector3 rotateDirection = Vector3.zero;	// �ړ�����

		ActionSegment<int> segments = actions.DiscreteActions;
		int rotate = segments[0];		// ��]
		int moveZAxis = segments[1];	// Z���ړ�
		int moveXAxis = segments[2];	// X���ړ�
		int shoot = segments[3];		// �e�𔭎�

		/*
		 * rotate =
		 * 0 : ��]���Ȃ�
		 * 1 : ����]
		 * 2 : �E��]
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
		 * 0 : �ړ����Ȃ�
		 * 1 : �O�i
		 * 2 : ���
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
		 * 0 : �ړ����Ȃ�
		 * 1 : �E�֕��s�ړ�
		 * 2 : ���֕��s�ړ�
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
		 * 0 : ���˂��Ȃ�
		 * 1 : ���˂���
		 */
		if (shoot == 1) {
			_bullet.Fire(transform);
		}

		// �p���x��������
		_rigidbody.angularVelocity = RotateSpeed * rotateDirection;
		// ���x��������
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
	/// �e�R���|�[�l���g�̎Q�Ƃ��擾
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
			0.006F,				// �^����
			0.004F, 0.004F,
			0.001F, 0.001F		// �O��
		};
	}
}