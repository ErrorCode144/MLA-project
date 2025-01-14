using UnityEngine;

/// <summary>
/// 弾を当てる標的のクラス
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class Target : MonoBehaviour {
	[SerializeField] private ShooterAgent Shooter = null;

	private void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Bullet")) {
			Debug.Log("Hit!");

			Shooter.Hit = true;
		}
	}
}