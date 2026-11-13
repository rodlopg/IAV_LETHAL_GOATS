using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.CoilHead
{
    public class Patrol : State
    {
        private bool destinationSet;

        public Patrol(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.COIL_HEAD_PATROL;
        }

        public override void Enter()
        {
            SetTrigger("isWalking");
            destinationSet = false;
            if (AgentReady())
            {
                agent.speed = settings.patrolSpeed;
                agent.isStopped = false;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            if (player != null && settings.CanSeePlayer())
            {
                if (settings.IsPlayerLookingAtMonster(settings.coilLookAngle))
                {
                    SetAgentStopped(true);
                }
                else
                {
                    TransitionTo(new Pursue(npc, agent, animator, player));
                    return;
                }
            }
            else
            {
                SetAgentStopped(false);
            }

            if (!AgentReady())
                return;

            if (!destinationSet || (!agent.pathPending && agent.remainingDistance <= settings.patrolArrivalDistance))
            {
                Vector3 destination;
                if (settings.TryGetPatrolDestination(out destination))
                {
                    agent.SetDestination(destination);
                    destinationSet = true;
                }
            }
        }

        public override void Exit()
        {
            ResetTrigger("isWalking");
            base.Exit();
        }
    }
}
