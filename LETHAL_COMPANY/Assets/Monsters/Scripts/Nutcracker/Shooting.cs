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

        public Shooting(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.NUTCRACKER_SHOOTING;
        }

        public override void Enter()
        {
            SetTrigger("isFiring");
            shotsRemaining = Mathf.Clamp(settings.nutcrackerShotsPerBurst, 1, 2);
            nextShotTime = 0f;
            reloadTimer = 0f;
            reloading = false;
            if (AgentReady())
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            if (!reloading)
            {
                if (player != null)
                {
                    Vector3 direction = player.position - npc.transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.01f)
                        npc.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                if (Time.time >= nextShotTime)
                {
                    settings.InvokeNutcrackerShot();
                    shotsRemaining--;
                    nextShotTime = Time.time + settings.nutcrackerFireInterval;
                    if (shotsRemaining <= 0)
                        reloading = true;
                }
                return;
            }

            reloadTimer += Time.deltaTime;
            if (reloadTimer >= settings.nutcrackerReloadDuration)
            {
                if (player != null && settings.CanSeePlayer())
                    TransitionTo(new Scanning(npc, agent, animator, player));
                else
                    TransitionTo(new Wandering(npc, agent, animator, player));
            }
        }

        public override void Exit()
        {
            ResetTrigger("isFiring");
            base.Exit();
        }
    }
}
