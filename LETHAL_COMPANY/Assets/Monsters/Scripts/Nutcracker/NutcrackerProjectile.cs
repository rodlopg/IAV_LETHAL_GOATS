using UnityEngine;

namespace LethalCompanyAI
{
    [RequireComponent(typeof(Rigidbody))]
    public class NutcrackerProjectile : MonoBehaviour
    {
        private Rigidbody body;
        private MonsterController shooter;
        private float despawnTime;
        private bool hasHit;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();

            // Ensure the projectile has a trigger collider, even for a simple custom prefab.
            Collider projectileCollider = GetComponent<Collider>();
            if (projectileCollider == null)
                projectileCollider = gameObject.AddComponent<SphereCollider>();

            projectileCollider.isTrigger = true;

            body.useGravity = false;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Launch(MonsterController owner, Vector3 direction, float speed, float lifetime)
        {
            shooter = owner;

            if (direction.sqrMagnitude < 0.001f)
                direction = transform.forward;

            direction.Normalize();
            transform.forward = direction;

            body.isKinematic = false;
            body.useGravity = false;
            body.linearVelocity = direction * speed;

            despawnTime = Time.time + Mathf.Max(0.1f, lifetime);
        }

        private void Update()
        {
            if (Time.time >= despawnTime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit || other == null)
                return;

            Transform hitTransform = other.transform;
            if (shooter != null &&
                (hitTransform == shooter.transform || hitTransform.IsChildOf(shooter.transform)))
                return;

            PlayerMonsterIntegration player = other.GetComponentInParent<PlayerMonsterIntegration>();
            if (player != null)
            {
                hasHit = true;

                // GameManager listens to this event and applies the configured shotgun damage.
                if (shooter != null)
                    shooter.InvokeNutcrackerShot();
                else
                    player.TakeDamage(45, "Nutcracker shotgun");

                Destroy(gameObject);
                return;
            }

            // Bullets stop on walls and other solid objects.
            if (!other.isTrigger)
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}
