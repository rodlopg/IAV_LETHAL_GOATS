using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.EyelessDog
{
    public class Roaming : State
    {
        private bool destinationSet;
        private int lastNoiseVersion;

        public Roaming(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.DOG_ROAMING;
        }

        public override void Enter()
        {
            SetTrigger("isRoaming");
            lastNoiseVersion = settings.NoiseVersion;
            destinationSet = false;
            if (AgentReady())
            {
                agent.speed = settings.dogRoamSpeed;
                agent.isStopped = false;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            Vector3 noisePosition;
            float loudness;
            if (settings.TryGetNewNoise(ref lastNoiseVersion, settings.dogHearingThreshold,
                out noisePosition, out loudness))
            {
                TransitionTo(new Investigating(npc, agent, animator, player));
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
            ResetTrigger("isRoaming");
            base.Exit();
        }
    }
}
