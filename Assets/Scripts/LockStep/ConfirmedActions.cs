using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ConfirmedActions {

	//if we use TCP we don't need to confirm between lock step turns.
	bool _isNeedConfirmed = false;

	Dictionary<int, bool> _confirmedCurrent;
	Dictionary<int, bool> _confirmedPrior;
	
	//Stop watches used to adjust lockstep turn length
	Stopwatch _currentSW;
	Stopwatch _priorSW;

	LockStepManager _lockStepManager;

	public void EnableConfirmedActions(bool isEnable) {
		_isNeedConfirmed = isEnable;
	}
	public bool IsConfirmedActionsEnabled() {return _isNeedConfirmed;}

	public ConfirmedActions(LockStepManager lsm) {

		this._lockStepManager = lsm;
		_confirmedCurrent  = new Dictionary<int, bool>();
		_confirmedPrior   = new Dictionary<int, bool>();

		_currentSW = new Stopwatch();
		_priorSW = new Stopwatch();
	}

	public void Reset() {
		_confirmedCurrent.Clear();
		_confirmedCurrent.Clear();
		_currentSW.Reset();
		_priorSW.Reset();
	}

	public int GetPriorTime() {
		if (!_isNeedConfirmed) {return 0;}
		return ((int)_priorSW.ElapsedMilliseconds);
	}
	
	public void StartTimer() {
		if (!_isNeedConfirmed) {return;}
		_currentSW.Start ();
	}

	public bool ReadyForNextTurn() {
		if (!_isNeedConfirmed) {return true;}
		//check that the action that is going to be processed has been confirmed
		if (_confirmedPrior.Count >= _lockStepManager.GetPlayersNumber()) {
			return true;
		}
		//if 2nd turn, check that the 1st turns action has been confirmed
		if (_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
			return _confirmedCurrent.Count == _lockStepManager.GetPlayersNumber();
		}
		//no action has been sent out prior to the first turn
		if (_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}

	public void NextTurn() {
		if (!_isNeedConfirmed) {return;}
		//clear prior actions
		_confirmedPrior.Clear();

		Dictionary<int, bool> swap = _confirmedPrior;
		Stopwatch swapSW = _priorSW;

		//last turns actions is now this turns prior actions
		_confirmedPrior = _confirmedCurrent;
		_priorSW = _currentSW;

		//set this turns confirmation actions to the empty array
		_confirmedCurrent = swap;
		_currentSW = swapSW;
		_currentSW.Reset();
	}

	public void ConfirmAction(int confirmingPlayerID, int currentLockStepTurn, int confirmedActionLockStepTurn) {
		if (!_isNeedConfirmed) {return;}
		if (confirmedActionLockStepTurn == currentLockStepTurn) {
			//if current turn, add to the current Turn Confirmation
			_confirmedCurrent[confirmingPlayerID] = true;
			//if we recieved the last confirmation, stop timer
			//this gives us the length of the longest roundtrip message
			if(_confirmedCurrent.Count == _lockStepManager.GetPlayersNumber()) {
				_currentSW.Stop ();
			}
		} else if (confirmedActionLockStepTurn == currentLockStepTurn -1) {
			//if confirmation for prior turn, add to the prior turn confirmation
			_confirmedPrior[confirmingPlayerID] = true;
			//if we recieved the last confirmation, stop timer
			//this gives us the length of the longest roundtrip message
			if (_confirmedPrior.Count == _lockStepManager.GetPlayersNumber()) {
				_priorSW.Stop();
			}
		} else {
			//TODO: Error Handling
			Debug.Log("WARNING!!!! Unexpected lockstepID Confirmed : " + confirmedActionLockStepTurn + " from player: " + confirmingPlayerID);
		}
	}

	public int[] WhosNotConfirmed() {
		if (!_isNeedConfirmed) {return null;}
		//check that the action that is going to be processed has been confirmed
		if (_confirmedPrior.Count == _lockStepManager.GetPlayersNumber()) {
			return null;
		}
		//if 2nd turn, check that the 1st turns action has been confirmed
		if (_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
			if (_confirmedCurrent.Count == _lockStepManager.GetPlayersNumber()) {
				return null;
			} else {
				return CheckWhosNotConfirmed(_confirmedCurrent, _confirmedCurrent.Count);
			}
		}
		//no action has been sent out prior to the first turn
		if (_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			return null;
		}

		return CheckWhosNotConfirmed(_confirmedPrior, _confirmedPrior.Count);
	}
	
	// Returns an array of player IDs of those players who have not confirmed are prior action.
	private int[] CheckWhosNotConfirmed(Dictionary<int, bool> confirmed, int confirmedCount) {
		if(confirmedCount < _lockStepManager.GetPlayersNumber()) {
			//the number of "not confirmed" is the number of players minus the number of "confirmed"
			int[] notConfirmed = new int[_lockStepManager.GetPlayersNumber() - confirmedCount];
			int count = 0;
			//loop through each player and see who has not confirmed
			for(int playerID = 0; playerID < _lockStepManager.GetPlayersNumber(); playerID++) {
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