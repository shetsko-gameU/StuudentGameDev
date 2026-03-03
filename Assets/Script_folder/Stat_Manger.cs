using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Stat_Manger : MonoBehaviour
{
    // A simple Stat Manger to keep track of things
    public static Stat_Manger Instance;

    [Header("Atk")]// Attack damge and speed
    public int baseAtkDamge;
    public int currentAtkDamage;
    public int baseAtkSpeed;
    public int currentAtkSpeed;

    [Header("Def")]// Defence 
    public int baseDef;
    public int currentDef;

    [Header("Movement Stats")] // Movment. Need to add to movement script when we tincker with this
    public int speed;

    [Header("Health Stats")] // Health
    public int maxHealth;
    public int currentHealth;

}
