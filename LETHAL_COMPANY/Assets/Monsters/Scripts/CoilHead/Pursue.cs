using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.CoilHead
{
    public class Pursue : State
    {
        private float chaseTimer;

        public Pursue(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.COIL_HEAD_PURSUE;
        }

        public override void Enter()
        {
            SetTrigger("isPursuing");
            chaseTimer = 0f;
            if (AgentReady())
            {
                agent.speed = settings.coilPursuitSpeed;
                agent.isStopped = false;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            if (player == null)
            {
                TransitionTo(new Patrol(npc, agent, animator, player));
                return;
            }

            // The Coil-Head is completely motionless while directly observed.
            if (settings.IsPlayerLookingAtMonster(settings.coilLookAngle))
            {
                SetAgentStopped(true);
                return;
            }

            SetAgentStopped(false);
            chaseTimer += Time.deltaTime;

            if (AgentReady())
            {
                agent.speed = settings.coilPursuitSpeed;
                agent.SetDestination(player.position);
            }

            if (chaseTimer >= settings.coilChaseDuration)
                TransitionTo(new Recharging(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isPursuing");
            base.Exit();
        }
    }
}
