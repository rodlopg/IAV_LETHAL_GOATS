using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Nutcracker
{
    public class Wandering : State
    {
        private float scanTimer;
        private bool destinationSet;

        public Wandering(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.NUTCRACKER_WANDERING;
        }

        public override void Enter()
        {
            SetTrigger("isWandering");
            scanTimer = 0f;
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
            scanTimer += Time.deltaTime;
            if (scanTimer >= settings.nutcrackerScanInterval)
            {
                TransitionTo(new Scanning(npc, agent, animator, player));
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
            ResetTrigger("isWandering");
            base.Exit();
        }
    }
}
