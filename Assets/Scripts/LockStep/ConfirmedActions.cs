using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ConfirmedActions {

	//if we use TCP we don't need to confirm between lock step turns.
	private bool isNeedConfirmed = false;

	private Dictionary<int, bool> confirmedCurrent;
	private Dictionary<int, bool> confirmedPrior;
	
	//Stop watches used to adjust lockstep turn length
	private Stopwatch currentSW;
	private Stopwatch priorSW;

	private LockStepManager lockStepManager;

	public void EnableConfirmedActions(bool isEnable) {
		isNeedConfirmed = isEnable;
	}
	public bool IsConfirmedActionsEnabled() {return isNeedConfirmed;}

	public ConfirmedActions(LockStepManager lsm) {

		this.lockStepManager = lsm;
		confirmedCurrent  = new Dictionary<int, bool>();
		confirmedPrior   = new Dictionary<int, bool>();

		currentSW = new Stopwatch();
		priorSW = new Stopwatch();
	}

	public void Reset() {
		confirmedCurrent.Clear();
		confirmedCurrent.Clear();
		currentSW.Reset();
		priorSW.Reset();
	}

	public int GetPriorTime() {
		if (!isNeedConfirmed) {return 0;}
		return ((int)priorSW.ElapsedMilliseconds);
	}
	
	public void StartTimer() {
		if (!isNeedConfirmed) {return;}
		currentSW.Start ();
	}

	public bool ReadyForNextTurn() {
		if (!isNeedConfirmed) {return true;}
		//check that the action that is going to be processed has been confirmed
		if (confirmedPrior.Count >= lockStepManager.GetPlayersNumber()) {
			return true;
		}
		//if 2nd turn, check that the 1st turns action has been confirmed
		if (lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
			return confirmedCurrent.Count == lockStepManager.GetPlayersNumber();
		}
		//no action has been sent out prior to the first turn
		if (lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}

	public void NextTurn() {
		if (!isNeedConfirmed) {return;}
		//clear prior actions
		confirmedPrior.Clear();

		Dictionary<int, bool> swap = confirmedPrior;
		Stopwatch swapSW = priorSW;

		//last turns actions is now this turns prior actions
		confirmedPrior = confirmedCurrent;
		priorSW = currentSW;

		//set this turns confirmation actions to the empty array
		confirmedCurrent = swap;
		currentSW = swapSW;
		currentSW.Reset();
	}

	public void ConfirmAction(int confirmingPlayerID, int currentLockStepTurn, int confirmedActionLockStepTurn) {
		if (!isNeedConfirmed) {return;}
		if (confirmedActionLockStepTurn == currentLockStepTurn) {
			//if current turn, add to the current Turn Confirmation
			confirmedCurrent[confirmingPlayerID] = true;
			//if we recieved the last confirmation, stop timer
			//this gives us the length of the longest roundtrip message
			if(confirmedCurrent.Count == lockStepManager.GetPlayersNumber()) {
				currentSW.Stop ();
			}
		} else if (confirmedActionLockStepTurn == currentLockStepTurn -1) {
			//if confirmation for prior turn, add to the prior turn confirmation
			confirmedPrior[confirmingPlayerID] = true;
			//if we recieved the last confirmation, stop timer
			//this gives us the length of the longest roundtrip message
			if (confirmedPrior.Count == lockStepManager.GetPlayersNumber()) {
				priorSW.Stop();
			}
		} else {
			//TODO: Error Handling
			Debug.Log("WARNING!!!! Unexpected lockstepID Confirmed : " + confirmedActionLockStepTurn + " from player: " + confirmingPlayerID);
		}
	}

	public int[] WhosNotConfirmed() {
		if (!isNeedConfirmed) {return null;}
		//check that the action that is going to be processed has been confirmed
		if (confirmedPrior.Count == lockStepManager.GetPlayersNumber()) {
			return null;
		}
		//if 2nd turn, check that the 1st turns action has been confirmed
		if (lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
			if (confirmedCurrent.Count == lockStepManager.GetPlayersNumber()) {
				return null;
			} else {
				return CheckWhosNotConfirmed(confirmedCurrent, confirmedCurrent.Count);
			}
		}
		//no action has been sent out prior to the first turn
		if (lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			return null;
		}

		return CheckWhosNotConfirmed(confirmedPrior, confirmedPrior.Count);
	}
	
	// Returns an array of player IDs of those players who have not confirmed are prior action.
	private int[] CheckWhosNotConfirmed(Dictionary<int, bool> confirmed, int confirmedCount) {
		if(confirmedCount < lockStepManager.GetPlayersNumber()) {
			//the number of "not confirmed" is the number of players minus the number of "confirmed"
			int[] notConfirmed = new int[lockStepManager.GetPlayersNumber() - confirmedCount];
			int count = 0;
			//loop through each player and see who has not confirmed
			for(int playerID = 0; playerID < lockStepManager.GetPlayersNumber(); playerID++) {
				if(!confirmed[playerID]) {
					//add "not confirmed" player ID to the array
					notConfirmed[count] = playerID;
					count++;
				}
			}
			return notConfirmed;
		} else {
			return null;
		}
	}
}