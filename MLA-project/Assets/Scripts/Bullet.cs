using UnityEngine;

/// <summary>
/// エージェントが放つ弾のクラス
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(MeshRenderer))]
public class Bullet : MonoBehaviour {
	[SerializeField] private float Speed = 2F;
	[SerializeField] private uint Lifetime = 250U;

	private Rigidbody _rigidbody = null;
	private SphereCollider _collider = null;
	private MeshRenderer _renderer = null;
	private int _currentLifeCount = 0;

	public bool IsActive { get; private set; } = false;

	public void Init() {
		transform.SetPositionAndRotation(new Vector3(0F, 0.25F, 0F), Quaternion.identity);
		_collider.enabled = false;
		_renderer.enabled = false;

		IsActive = false;
	}

	public void Fire(Transform owner) {
		if (IsActive) {
			return;
		}

		transform.SetPositionAndRotation(owner.position, owner.rotation);
		_collider.enabled = true;
		_renderer.enabled = true;

		_currentLifeCount = (int)Lifetime;

		IsActive = true;
	}

	public void Die() {
		// _rigidbody.linearVelocity = Vector3.zero;

		_collider.enabled = false;
		_renderer.enabled = false;

		IsActive = false;
	}

	private void Awake() {
		_rigidbody = GetComponent<Rigidbody>();
		_collider = GetComponent<SphereCollider>();
		_renderer = GetComponent<MeshRenderer>();
	}

	private void FixedUpdate() {
		if (IsActive) {
			if (_currentLifeCount < 0) {
				Die();
			}

			// _rigidbody.linearVelocity = Speed * transform.forward;
			_rigidbody.MovePosition(Speed * Time.fixedDeltaTime * transform.forward + transform.position);

			--_currentLifeCount;
		}
	}

	private void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Target")) {
			Die();
		}
	}
}