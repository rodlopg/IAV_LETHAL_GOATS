
using UnityEngine;
using LethalCompanyAI;

public class ManualNoiseTrigger : MonoBehaviour
{
    private PlayerMonsterIntegration playerNoise;

    void Start()
    {
        playerNoise = GetComponent<PlayerMonsterIntegration>();

        if (playerNoise == null)
            Debug.LogError("PlayerMonsterIntegration not found!");
    }

    void Update()
    {
        if (playerNoise == null) return;

        if (Input.GetKeyDown(KeyCode.G))
            playerNoise.EmitNoise(0.35f, 28f); // Quiet

        if (Input.GetKeyDown(KeyCode.H))
            playerNoise.EmitNoise(0.65f, 28f); // Medium

        if (Input.GetKeyDown(KeyCode.J))
            playerNoise.EmitNoise(1f, 28f); // Loud
    }
}
