using System.Collections;
using UnityEngine;

public interface TriggerCheck
{
    bool isAggroed{get; set;}
    bool isWithinRange{get; set;}
    void SetAggroStatus (bool isAggroed);
    void SetRangeBool (bool isWithinRange);
    
}
