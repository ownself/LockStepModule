using System;
using System.Collections.Generic;
using UnityEngine;

public class PendingActions
{
	public Dictionary<int, Action> currentActions;
	private Dictionary<int, Action> nextActions;
	private Dictionary<int, Action> nextNextActions;
	//incase other players advance to the next step and send their action before we advance a step
	private Dictionary<int, Action> nextNextNextActions;

	LockStepManager lockStepManager;

	//Initialize all the arrays
	public PendingActions (LockStepManager lsm) {
		this.lockStepManager = lsm;
		currentActions = new Dictionary<int, Action>();
		nextActions = new Dictionary<int, Action>();
		nextNextActions = new Dictionary<int, Action>();
		nextNextNextActions = new Dictionary<int, Action>();
	}

	public void Reset() {
		currentActions.Clear();
		nextActions.Clear();
		nextNextActions.Clear();
		nextNextNextActions.Clear();
	}

	public bool ReadyForNextTurn() {
		if(nextNextActions.Count >= lockStepManager.GetPlayersNumber()) {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
				return true;
			}
			//Check if all Actions that will be processed next turn have been recieved
			if(nextActions.Count >= lockStepManager.GetPlayersNumber()) {
				return true;
			}
		}
		//if this is the 1st turn, no actions had the chance to be recieved yet
		if(lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}

	public void NextTurn() {
		currentActions.Clear();
		Dictionary<int, Action> swap = currentActions;

		//last turn's actions is now this turn's actions
		currentActions = nextActions;
		//last turn's next next actions is now this turn's next actions
		nextActions = nextNextActions;
		nextNextActions = nextNextNextActions;
		//set nextNextNextActions to the empty list
		nextNextNextActions = swap;
	}
	
	public void AddAction(Action action, int playerID, int currentLockStepTurn, int actionsLockStepTurn) {
		//add action for processing later
		if (actionsLockStepTurn == currentLockStepTurn + 1) {
			//if action is for next turn, add for processing 3 turns away
			if (nextNextNextActions.ContainsKey(playerID)) {
				//TODO: Error Handling
				Debug.Log("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			nextNextNextActions[playerID] = action;
		} else if (actionsLockStepTurn == currentLockStepTurn) {
			//if recieved action during our current turn
			//add for processing 2 turns away
			if (nextNextActions.ContainsKey(playerID)) {
				//TODO: Error Handling
				Debug.Log("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			nextNextActions[playerID] = action;
		} else if (actionsLockStepTurn == currentLockStepTurn - 1) {
			//if recieved action for last turn
			//add for processing 1 turn away
			if (nextActions.ContainsKey(playerID)) {
				//TODO: Error Handling
				Debug.Log("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			nextActions[playerID] = action;
		} else {
			//TODO: Error Handling
			Debug.Log("WARNING!!!! Unexpected lockstepID recieved : " + actionsLockStepTurn);
			return;
		}
	}

	public List<int> WhosNotReady() {
		if(nextNextActions.Count == lockStepManager.GetPlayersNumber()) {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID + 1) {
				return null;
			}

			//Check if all Actions that will be processed next turn have been recieved
			if(nextActions.Count == lockStepManager.GetPlayersNumber()) {
				return null;
			} else {
				return CheckWhosNotReady(nextActions, nextActions.Count);
			}

		} else if(lockStepManager.GetLockStepTurn() == LockStepManager.firstLockStepTurnID) {
			//if this is the 1st turn, no actions had the chance to be recieved yet
			return null;
		} else {
			return CheckWhosNotReady(nextNextActions, nextNextActions.Count);
		}
	}

	private List<int> CheckWhosNotReady(Dictionary<int, Action> actions, int count) {
		if (count < lockStepManager.GetPlayersNumber()) {
			List<int> notReadyPlayers = new List<int>();
			foreach (KeyValuePair<int, LockStepPlayer> player in lockStepManager.allPlayers) {
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