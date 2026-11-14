using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Nutcracker
{
    public class Scanning : State
    {
        private float scanTimer;
        private Quaternion startRotation;

        public Scanning(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.NUTCRACKER_SCANNING;
        }

        public override void Enter()
        {
            SetTrigger("isScanning");
            scanTimer = 0f;
            startRotation = npc.transform.rotation;
            if (AgentReady())
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            scanTimer += Time.deltaTime;
            float progress = settings.nutcrackerScanDuration <= 0f ? 1f :
                Mathf.Clamp01(scanTimer / settings.nutcrackerScanDuration);
            float yaw = settings.nutcrackerScanAngle * progress;
            npc.transform.rotation = startRotation * Quaternion.Euler(0f, yaw, 0f);

            if (player != null && settings.CanSeePlayer())
            {
                bool playerCloseEnoughForMelee = settings.IsPlayerWithinNutcrackerMeleeRange();
                bool playerVisibleForShot =
                    Vector3.Distance(npc.transform.position, player.position) <= settings.nutcrackerShootingRange &&
                    settings.IsPlayerMoving(settings.nutcrackerMovementThreshold);

                // A nearby player can trigger a kick even when standing still.
                if (playerCloseEnoughForMelee || playerVisibleForShot)
                {
                    TransitionTo(new Shooting(npc, agent, animator, player));
                    return;
                }
            }

            if (scanTimer >= settings.nutcrackerScanDuration)
                TransitionTo(new Wandering(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isScanning");
            base.Exit();
        }
    }
}
