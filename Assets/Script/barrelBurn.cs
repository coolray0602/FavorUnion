using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class barrelBurn : MonoBehaviour
{
    public GameObject fireEffect;
    public GameObject smokeEffect;

    public void Burn()
    {
        StartCoroutine(BurnRoutine());
    }

    IEnumerator BurnRoutine()
    {
        PlayerPrefs.SetInt("burnedGoldPaper", 1);
        fireEffect.SetActive(true);
        smokeEffect.SetActive(false);
        yield return new WaitForSeconds(3f);

        fireEffect.SetActive(false);
    }
    public void Smoke()
    {
        StartCoroutine(SmokeRoutine());
    }

    IEnumerator SmokeRoutine()
    {
        smokeEffect.SetActive(true);
        fireEffect.SetActive(false);

        yield return new WaitForSeconds(3f);

        smokeEffect.SetActive(false);
    }
}
