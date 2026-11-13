using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Bracken
{
    public class Stalking : State
    {
        private float nextTrailUpdate;

        public Stalking(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.BRACKEN_STALKING;
        }

        public override void Enter()
        {
            SetTrigger("isStalking");
            if (AgentReady())
            {
                agent.speed = settings.brackenStalkSpeed;
                agent.isStopped = false;
            }
            nextTrailUpdate = 0f;
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            bool lookedAt = player != null && settings.IsPlayerLookingAtMonster(settings.brackenLookAngle);
            settings.UpdateBrackenStare(lookedAt);

            if (settings.ConsumeAttackedFlag() || settings.BrackenStareTimer >= settings.brackenStareToEnrage)
            {
                TransitionTo(new EnragedAttack(npc, agent, animator, player));
                return;
            }

            if (lookedAt && settings.BrackenStareTimer >= settings.brackenLookToRetreat)
            {
                TransitionTo(new Retreating(npc, agent, animator, player));
                return;
            }

            if (player != null && Time.time >= nextTrailUpdate && AgentReady())
            {
                Vector3 trailPosition = player.position - player.forward * settings.brackenTrailOffset;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(trailPosition, out hit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                nextTrailUpdate = Time.time + settings.brackenTrailUpdateInterval;
            }
        }

        public override void Exit()
        {
            ResetTrigger("isStalking");
            base.Exit();
        }
    }
}
