using RKS.HadalZone.Core;
using UnityEngine;

public class TestTrigger : RKSBehaviour
{
    private void OnTriggerEnter(Collider player)
    {
        if (player.CompareTag("Player"))
        {
           Transition.LoadScene("LevelThree");
        }
    }
}
