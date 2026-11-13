using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.EyelessDog
{
    public class Investigating : State
    {
        private Vector3 investigationPoint;
        private float lastNoiseLoudness;
        private float investigationTimer;
        private int lastNoiseVersion;

        public Investigating(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.DOG_INVESTIGATING;
        }

        public override void Enter()
        {
            SetTrigger("isInvestigating");
            lastNoiseVersion = settings.NoiseVersion;
            investigationTimer = 0f;
            if (settings.TryGetLatestNoise(out investigationPoint, out lastNoiseLoudness))
            {
                // Keep the current sound location as the investigation target.
            }
            else
            {
                investigationPoint = npc.transform.position;
            }

            if (AgentReady())
            {
                agent.speed = settings.dogInvestigateSpeed;
                agent.isStopped = false;
                agent.SetDestination(investigationPoint);
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            investigationTimer += Time.deltaTime;

            Vector3 newerNoise;
            float newerLoudness;
            if (settings.TryGetNewNoise(ref lastNoiseVersion, settings.dogHearingThreshold,
                out newerNoise, out newerLoudness))
            {
                investigationPoint = newerNoise;
                lastNoiseLoudness = newerLoudness;
                if (AgentReady())
                    agent.SetDestination(investigationPoint);
            }

            if (lastNoiseLoudness >= settings.dogChargeLoudnessThreshold)
            {
                TransitionTo(new BlindCharging(npc, agent, animator, player));
                return;
            }

            bool arrived = AgentReady() && !agent.pathPending && agent.hasPath &&
                           agent.remainingDistance <= settings.patrolArrivalDistance;
            if (arrived || investigationTimer >= settings.dogNoiseMemoryDuration)
                TransitionTo(new Roaming(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isInvestigating");
            base.Exit();
        }
    }
}
