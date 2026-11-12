using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Survival/Night Wave Schedule")]
public class EnemyWaveSchedule : ScriptableObject
{
    [Serializable]
    public class Night
    {
        public int night;
        public int[] counts;
    }

    public Night[] nights;
    // Fractions of authoritative night duration also work with development durations.
    public float[] waveFractions =
    {
        0,
        1f / 3,
        2f / 3
    };
}
