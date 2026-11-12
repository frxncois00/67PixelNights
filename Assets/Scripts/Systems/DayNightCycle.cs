using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class DayNightCycle : MonoBehaviour {
 public enum Phase { Day, Night }
 [SerializeField] float dayDuration=180, nightDuration=120, transitionDuration=15;
 [SerializeField] bool testMode;
 [SerializeField] float testDayDuration=20,testNightDuration=15,testTransitionDuration=3;
 public int CurrentDay {get;private set;}=1;
 public Phase CurrentPhase {get;private set;}=Phase.Day;
 public float ElapsedPhaseTime {get;private set;}
 public bool Paused {get;set;}
 public bool IsDaytime=>CurrentPhase==Phase.Day;
 public bool IsNighttime=>!IsDaytime;
 public float PhaseDuration=>Mathf.Max(.1f,IsDaytime?(testMode?testDayDuration:dayDuration):(testMode?testNightDuration:nightDuration));
 public float TransitionDuration=>Mathf.Clamp(testMode?testTransitionDuration:transitionDuration,0,PhaseDuration);
 public float TimeRemaining=>Mathf.Max(0,PhaseDuration-ElapsedPhaseTime);
 public float NormalizedPhaseProgress=>Mathf.Clamp01(ElapsedPhaseTime/PhaseDuration);
 public event Action OnDayStarted,OnNightStarted,OnPhaseChanged,OnDayChanged;
 void Start(){OnDayChanged?.Invoke();OnPhaseChanged?.Invoke();OnDayStarted?.Invoke();}
 void Update(){
 #if UNITY_EDITOR || DEVELOPMENT_BUILD
 var k=Keyboard.current;if(k!=null){if(k.f6Key.wasPressedThisFrame)SkipPhase();if(k.f7Key.wasPressedThisFrame)Paused=!Paused;}
 #endif
 if(!Paused)Advance(Time.deltaTime);
 }
 public void Advance(float seconds){if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;double remaining=seconds;while(remaining>=TimeRemaining){remaining-=TimeRemaining;SkipPhase();}ElapsedPhaseTime+=(float)remaining;}
 public void SkipPhase(){ElapsedPhaseTime=0;if(IsDaytime)CurrentPhase=Phase.Night;else{CurrentPhase=Phase.Day;CurrentDay++;OnDayChanged?.Invoke();}OnPhaseChanged?.Invoke();if(IsDaytime)OnDayStarted?.Invoke();else OnNightStarted?.Invoke();}
 void OnValidate(){dayDuration=Mathf.Max(.1f,dayDuration);nightDuration=Mathf.Max(.1f,nightDuration);testDayDuration=Mathf.Max(.1f,testDayDuration);testNightDuration=Mathf.Max(.1f,testNightDuration);transitionDuration=Mathf.Max(0,transitionDuration);testTransitionDuration=Mathf.Max(0,testTransitionDuration);}
}
