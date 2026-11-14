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

            if (settings.IsPlayerWithinNutcrackerMeleeRange())
                settings.TriggerNutcrackerMeleeAnimation();
            else
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

            // Track the player while attacking.
            Vector3 direction = player.position - npc.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction.normalized, Vector3.up);

                npc.transform.rotation = Quaternion.RotateTowards(
                    npc.transform.rotation,
                    targetRotation,
                    settings.nutcrackerScanTurnSpeed * 2f * Time.deltaTime
                );
            }

            // Resolve a melee swing separately from shotgun fire.
            if (meleeInProgress)
            {
                meleeTimer += Time.deltaTime;

                if (!meleeDamageChecked &&
                    meleeTimer >= settings.nutcrackerMeleeHitDelay)
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
                    // Return to scanning so the Nutcracker must reacquire a target.
                    if (player != null)
                        TransitionTo(new Scanning(npc, agent, animator, player));
                    else
                        TransitionTo(new Wandering(npc, agent, animator, player));
                }

                return;
            }

            // If the player reaches the Nutcracker, use the kick instead of
            // attempting to fire a shotgun at point-blank range.
            if (settings.IsPlayerWithinNutcrackerMeleeRange())
            {
                meleeInProgress = true;
                meleeDamageChecked = false;
                meleeTimer = 0f;

                ResetTrigger("isFiring");
                settings.TriggerNutcrackerMeleeAnimation();
                return;
            }

            // Do not fire through walls or beyond the configured shotgun range.
            // A wide FOV is used here because the Nutcracker is already in its
            // attack state and is actively turning to face the detected target.
            if (!settings.CanNutcrackerSeePlayer(160f))
            {
                TransitionTo(new Scanning(npc, agent, animator, player));
                return;
            }

            if (Time.time >= nextShotTime)
            {
                if (settings.FireNutcrackerProjectile())
                {
                    shotsRemaining--;
                    nextShotTime = Time.time +
                        Mathf.Max(0.05f, settings.nutcrackerFireInterval);

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
