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
            if (player == null)
            {
                TransitionTo(new Wandering(npc, agent, animator, player));
                return;
            }

            scanTimer += Time.deltaTime;

            // Sweep from -half angle to +half angle, rather than rotating only one way.
            float duration = Mathf.Max(0.1f, settings.nutcrackerScanDuration);
            float progress = Mathf.Clamp01(scanTimer / duration);
            float halfAngle = settings.nutcrackerScanAngle * 0.5f;
            float yaw = Mathf.Lerp(-halfAngle, halfAngle, progress);

            npc.transform.rotation =
                startRotation * Quaternion.Euler(0f, yaw, 0f);

            // A close player gets the separate melee attack.
            if (settings.IsPlayerWithinNutcrackerMeleeRange())
            {
                TransitionTo(new Shooting(npc, agent, animator, player));
                return;
            }

            // Ranged detection uses the Nutcracker's own range and detection FOV,
            // not the generic MonsterController vision distance/FOV.
            bool playerVisible = settings.CanNutcrackerSeePlayer(
                settings.nutcrackerDetectionFov
            );

            bool playerMoving =
                settings.IsPlayerMoving(settings.nutcrackerMovementThreshold);

            if (playerVisible &&
                (!settings.nutcrackerRequireMovementToShoot || playerMoving))
            {
                TransitionTo(new Shooting(npc, agent, animator, player));
                return;
            }

            if (scanTimer >= duration)
                TransitionTo(new Wandering(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isScanning");
            base.Exit();
        }
    }
}
