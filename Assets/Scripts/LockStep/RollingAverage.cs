using System.Collections.Generic;

public class RollingAverage {

	public Dictionary<int, int> currentValues;

	Dictionary<int, int> _playerAverages;
	public RollingAverage(int numofPlayers, int initValue) {
		_playerAverages = new Dictionary<int, int>();
		currentValues = new Dictionary<int, int>();
		for(int i=0; i<numofPlayers; i++) {
			_playerAverages[i] = initValue;
			currentValues[i] = initValue;
		}
	}

	public void Reset() {
		currentValues.Clear();
	}

	// TODO :  Think about gradule averages
	public void Add(int newValue, int playerID) {
		// if(_playerAverages.ContainsKey(playerID) && newValue > _playerAverages[playerID]) {
			//rise quickly
			_playerAverages[playerID] = newValue;
		// } else {
			//slowly fall down
			// _playerAverages[playerID] = newValue;
			// _playerAverages[playerID] = (_playerAverages[playerID] * (9) + newValue * (1)) / 10;
		// }

		currentValues[playerID] = newValue;
	}

	public int GetMax(int defaultValue) {
		int max = defaultValue;
		foreach (KeyValuePair<int, int> playerAverage in _playerAverages) {
			if(playerAverage.Value > max) {
				max = playerAverage.Value;
			}
		}

		return max;
	}
}