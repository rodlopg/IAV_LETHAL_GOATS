
using UnityEngine;
using UnityEngine.AI;

namespace LethalCompanyAI.CoilHead
{
    public class Pursue : State
    {
        private float chaseTimer;
        private bool isBeingWatched;

        public Pursue(
            GameObject _npc,
            NavMeshAgent _agent,
            Animator _animator,
            Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.COIL_HEAD_PURSUE;
        }

        public override void Enter()
        {
            SetTrigger("isPursuing");

            chaseTimer = 0f;
            isBeingWatched = false;

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
                TransitionTo(
                    new Patrol(npc, agent, animator, player)
                );
                return;
            }

            // Check if the player is watching the Coil-Head.
            bool watchedNow =
                settings.IsPlayerLookingAtMonster(
                    settings.coilLookAngle
                );

            // PLAYER LOOKS AT THE COIL-HEAD.
            if (watchedNow)
            {
                if (!isBeingWatched)
                {
                    isBeingWatched = true;
                    settings.SetCoilHeadFrozen(true);
                }

                // Remain a statue.
                return;
            }

            // PLAYER STOPS WATCHING.
            if (isBeingWatched)
            {
                isBeingWatched = false;

                // Release the physical freeze.
                settings.SetCoilHeadFrozen(false);

                // Return to patrol instead of following the player.
                TransitionTo(
                    new Patrol(npc, agent, animator, player)
                );

                return;
            }

            // PLAYER IS OUT OF THE COIL-HEAD'S LINE OF SIGHT.
            if (!settings.CanSeePlayer())
            {
                TransitionTo(
                    new Patrol(npc, agent, animator, player)
                );

                return;
            }

            // NORMAL PURSUIT.
            if (AgentReady())
            {
                agent.speed = settings.coilPursuitSpeed;
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }

            chaseTimer += Time.deltaTime;

            if (chaseTimer >= settings.coilChaseDuration)
            {
                TransitionTo(
                    new Recharging(npc, agent, animator, player)
                );
            }
        }

        public override void Exit()
        {
            // Ensure the NavMeshAgent is never left frozen.
            settings.SetCoilHeadFrozen(false);

            ResetTrigger("isPursuing");

            base.Exit();
        }
    }
}
