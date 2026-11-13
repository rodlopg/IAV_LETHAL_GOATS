using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Thumper
{
    public class Patrolling : State
    {
        private bool destinationSet;

        public Patrolling(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.THUMPER_PATROLLING;
        }

        public override void Enter()
        {
            SetTrigger("isPatrolling");
            destinationSet = false;
            if (AgentReady())
            {
                agent.speed = settings.patrolSpeed;
                agent.isStopped = false;
                agent.updateRotation = true;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            if (player != null && settings.CanSeePlayer(settings.thumperFieldOfView))
            {
                TransitionTo(new StraightLineCharge(npc, agent, animator, player));
                return;
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
            ResetTrigger("isPatrolling");
            base.Exit();
        }
    }
}
