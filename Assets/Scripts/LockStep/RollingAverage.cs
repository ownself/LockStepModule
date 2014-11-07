using System.Collections.Generic;

public class RollingAverage {

	public Dictionary<int, int> currentValues;

	Dictionary<int, int> playerAverages;
	public RollingAverage(int numofPlayers, int initValue) {
		playerAverages = new Dictionary<int, int>();
		currentValues = new Dictionary<int, int>();
		for(int i=0; i<numofPlayers; i++) {
			playerAverages[i] = initValue;
			currentValues[i] = initValue;
		}
	}

	public void Reset() {
		currentValues.Clear();
	}

	// TODO :  Think about gradule averages
	public void Add(int newValue, int playerID) {
		// if(playerAverages.ContainsKey(playerID) && newValue > playerAverages[playerID]) {
			//rise quickly
			playerAverages[playerID] = newValue;
		// } else {
			//slowly fall down
			// playerAverages[playerID] = newValue;
			// playerAverages[playerID] = (playerAverages[playerID] * (9) + newValue * (1)) / 10;
		// }

		currentValues[playerID] = newValue;
	}

	public int GetMax(int defaultValue) {
		int max = defaultValue;
		foreach (KeyValuePair<int, int> playerAverage in playerAverages) {
			if(playerAverage.Value > max) {
				max = playerAverage.Value;
			}
		}

		return max;
	}
}