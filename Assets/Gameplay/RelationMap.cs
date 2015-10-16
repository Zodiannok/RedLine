using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RelationMap : MonoBehaviour {

	private Dictionary<long, Dictionary<long, int>> _RelationLookup;

	// Use this for initialization
	void Start () {
		_RelationLookup = new Dictionary<long, Dictionary<long, int>> ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// Completely clear the relation map.
	public void Clear() {
		_RelationLookup.Clear ();
	}

	// Setup initial relation value.
	public void SetupInitialRelations(CardManager manager, IEnumerable<long> cards) {
		foreach (long cardType in cards) {
			CardBaseData data = manager.GetCardBaseData(cardType);
			if (data.Equals(CardBaseData.EmptyCard)) {
				continue;
			}

			foreach (RelationBaseData relation in data.Relations) {
				var otherCards = manager.FindCardsWithName(relation.OtherName);
				foreach (CardBaseData otherCardData in otherCards) {
					// Can be optimized (doesn't need to do fromCard lookup every time), but oh well.
					ApplyRelationshipChange(data.CardType, otherCardData.CardType, relation.Relationship);
				}
			}
		}
	}

	// Apply relationship change.
	public void ApplyRelationshipChange(long fromCard, long toCard, int delta) {
		Dictionary<long, int> secondaryLookup;
		if (!_RelationLookup.ContainsKey (fromCard)) {
			secondaryLookup = new Dictionary<long, int> ();
			_RelationLookup [fromCard] = secondaryLookup;
		} else {
			secondaryLookup = _RelationLookup[fromCard];
		}

		if (secondaryLookup.ContainsKey (toCard)) {
			secondaryLookup[toCard] += delta;
		} else {
			secondaryLookup[toCard] = delta;
		}
	}

	// Query the relationship status
	public int GetRelationship(long fromCard, long toCard) {
		if (!_RelationLookup.ContainsKey (fromCard)) {
			return 0;
		}
		var secondaryLookup = _RelationLookup [fromCard];
		if (!secondaryLookup.ContainsKey (toCard)) {
			return 0;
		}
		return secondaryLookup[toCard];
	}
}
