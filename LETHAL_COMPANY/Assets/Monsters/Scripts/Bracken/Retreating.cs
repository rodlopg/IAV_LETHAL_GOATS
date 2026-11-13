using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Bracken
{
    public class Retreating : State
    {
        private float stateTimer;
        private bool destinationSet;

        public Retreating(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.BRACKEN_RETREATING;
        }

        public override void Enter()
        {
            SetTrigger("isRetreating");
            stateTimer = 0f;
            destinationSet = false;
            if (AgentReady())
            {
                agent.speed = settings.brackenRetreatSpeed;
                agent.isStopped = true; // Brief freeze before scuttling away.
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            stateTimer += Time.deltaTime;
            bool lookedAt = player != null && settings.IsPlayerLookingAtMonster(settings.brackenLookAngle);
            settings.UpdateBrackenStare(lookedAt);

            if (settings.ConsumeAttackedFlag() || settings.BrackenStareTimer >= settings.brackenStareToEnrage)
            {
                TransitionTo(new EnragedAttack(npc, agent, animator, player));
                return;
            }

            if (stateTimer >= settings.brackenRetreatFreezeTime && !destinationSet && AgentReady())
            {
                Vector3 threat = player != null ? player.position : npc.transform.position + npc.transform.forward;
                Vector3 destination;
                if (settings.TryGetRetreatDestination(threat, out destination))
                    agent.SetDestination(destination);
                agent.isStopped = false;
                destinationSet = true;
            }

            if (stateTimer >= settings.brackenRetreatDuration)
                TransitionTo(new Stalking(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isRetreating");
            base.Exit();
        }
    }
}
