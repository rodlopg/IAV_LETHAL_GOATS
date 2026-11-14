using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Nutcracker
{
    public class Shooting : State
    {
        private int shotsRemaining;
        private float nextShotTime;
        private float reloadTimer;
        private bool reloading;

        private bool meleeInProgress;
        private bool meleeDamageChecked;
        private float meleeTimer;

        public Shooting(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.NUTCRACKER_SHOOTING;
        }

        public override void Enter()
        {
            shotsRemaining = Mathf.Clamp(settings.nutcrackerShotsPerBurst, 1, 2);
            nextShotTime = Time.time;
            reloadTimer = 0f;
            reloading = false;
            meleeInProgress = false;
            meleeDamageChecked = false;
            meleeTimer = 0f;

            if (AgentReady())
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (!settings.IsPlayerWithinNutcrackerMeleeRange())
                SetTrigger("isFiring");

            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            if (player == null)
            {
                TransitionTo(new Wandering(npc, agent, animator, player));
                return;
            }

            // Keep the Nutcracker facing the player while attacking.
            Vector3 direction = player.position - npc.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                npc.transform.rotation = Quaternion.RotateTowards(
                    npc.transform.rotation,
                    targetRotation,
                    settings.nutcrackerScanTurnSpeed * 2f * Time.deltaTime);
            }

            // Finish an already-started melee swing before considering another attack.
            if (meleeInProgress)
            {
                meleeTimer += Time.deltaTime;

                if (!meleeDamageChecked && meleeTimer >= settings.nutcrackerMeleeHitDelay)
                {
                    settings.TryNutcrackerMeleeAttack();
                    meleeDamageChecked = true;
                }

                if (meleeTimer >= settings.nutcrackerMeleeDuration)
                {
                    meleeInProgress = false;
                    reloading = true;
                    reloadTimer = 0f;
                }

                return;
            }

            if (reloading)
            {
                reloadTimer += Time.deltaTime;
                if (reloadTimer >= settings.nutcrackerReloadDuration)
                {
                    if (player != null && settings.CanSeePlayer())
                        TransitionTo(new Scanning(npc, agent, animator, player));
                    else
                        TransitionTo(new Wandering(npc, agent, animator, player));
                }

                return;
            }

            // The Nutcracker kicks when the player gets too close instead of firing point-blank.
            if (settings.IsPlayerWithinNutcrackerMeleeRange())
            {
                meleeInProgress = true;
                meleeDamageChecked = false;
                meleeTimer = 0f;
                ResetTrigger("isFiring");
                settings.TriggerNutcrackerMeleeAnimation();
                return;
            }

            // Fire an actual projectile. Damage is applied later, only if it hits the player.
            if (Time.time >= nextShotTime)
            {
                if (settings.FireNutcrackerProjectile())
                {
                    shotsRemaining--;
                    nextShotTime = Time.time + Mathf.Max(0.05f, settings.nutcrackerFireInterval);

                    if (shotsRemaining <= 0)
                    {
                        reloading = true;
                        reloadTimer = 0f;
                    }
                }
            }
        }

        public override void Exit()
        {
            ResetTrigger("isFiring");
            ResetTrigger("isMelee");
            base.Exit();
        }
    }
}
