using System;
using System.Collections.Generic;
using UnityEngine;

public class PendingActions
{
	public Dictionary<int, Action> currentActions;
	private Dictionary<int, Action> _nextActions;
	private Dictionary<int, Action> _nextNextActions;
	//incase other players advance to the next step and send their action before we advance a step
	private Dictionary<int, Action> _nextNextNextActions;

	LockStepManager _lockStepManager;

	//Initialize all the arrays
	public PendingActions (LockStepManager lsm) {
		this._lockStepManager = lsm;
		currentActions = new Dictionary<int, Action>();
		_nextActions = new Dictionary<int, Action>();
		_nextNextActions = new Dictionary<int, Action>();
		_nextNextNextActions = new Dictionary<int, Action>();
	}

	public void Reset() {
		currentActions.Clear();
		_nextActions.Clear();
		_nextNextActions.Clear();
		_nextNextNextActions.Clear();
	}

	public bool ReadyForNextTurn() {
		if(_nextNextActions.Count >= _lockStepManager.GetPlayersNumber()) {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
				return true;
			}
			//Check if all Actions that will be processed next turn have been recieved
			if(_nextActions.Count >= _lockStepManager.GetPlayersNumber()) {
				return true;
			}
		}
		//if this is the 1st turn, no actions had the chance to be recieved yet
		if(_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}

	public void NextTurn() {
		currentActions.Clear();
		Dictionary<int, Action> swap = currentActions;

		//last turn's actions is now this turn's actions
		currentActions = _nextActions;
		//last turn's next next actions is now this turn's next actions
		_nextActions = _nextNextActions;
		_nextNextActions = _nextNextNextActions;
		//set _nextNextNextActions to the empty list
		_nextNextNextActions = swap;
	}
	
	public void AddAction(Action action, int playerID, int currentLockStepTurn, int actionsLockStepTurn) {
		//add action for processing later
		if (actionsLockStepTurn == currentLockStepTurn + 1) {
			//if action is for next turn, add for processing 3 turns away
			if (_nextNextNextActions.ContainsKey(playerID)) {
				//TODO: Error Handling
				Debug.Log("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			_nextNextNextActions[playerID] = action;
		} else if (actionsLockStepTurn == currentLockStepTurn) {
			//if recieved action during our current turn
			//add for processing 2 turns away
			if (_nextNextActions.ContainsKey(playerID)) {
				//TODO: Error Handling
				Debug.Log("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			_nextNextActions[playerID] = action;
		} else if (actionsLockStepTurn == currentLockStepTurn - 1) {
			//if recieved action for last turn
			//add for processing 1 turn away
			if (_nextActions.ContainsKey(playerID)) {
				//TODO: Error Handling
				Debug.Log("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			_nextActions[playerID] = action;
		} else {
			//TODO: Error Handling
			Debug.Log("WARNING!!!! Unexpected lockstepID recieved : " + actionsLockStepTurn);
			return;
		}
	}

	public List<int> WhosNotReady() {
		if(_nextNextActions.Count == _lockStepManager.GetPlayersNumber()) {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
				return null;
			}

			//Check if all Actions that will be processed next turn have been recieved
			if(_nextActions.Count == _lockStepManager.GetPlayersNumber()) {
				return null;
			} else {
				return CheckWhosNotReady(_nextActions, _nextActions.Count);
			}

		} else if(_lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			//if this is the 1st turn, no actions had the chance to be recieved yet
			return null;
		} else {
			return CheckWhosNotReady(_nextNextActions, _nextNextActions.Count);
		}
	}

	private List<int> CheckWhosNotReady(Dictionary<int, Action> actions, int count) {
		if (count < _lockStepManager.GetPlayersNumber()) {
			List<int> notReadyPlayers = new List<int>();
			foreach (KeyValuePair<int, LockStepPlayer> player in _lockStepManager.allPlayers) {
				if (!actions.ContainsKey(player.Key) || actions[player.Key] == null) {
					notReadyPlayers.Add(player.Key);
				}
			}
			return notReadyPlayers;
		} else {
			return null;
		}
	}
}