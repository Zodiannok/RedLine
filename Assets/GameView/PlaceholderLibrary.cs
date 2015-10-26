using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaceholderLibrary : MonoBehaviour {

	private List<CardBaseData> _CardData; 

	// Use this for initialization
	void Start () {
		_CardData = new List<CardBaseData> ();

		CardBaseData nextData;

		// Fill in card data here.
		//nextData = GetDummyData (_CardData.Count);
		//_CardData.Add (nextData);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// Load all cards!
	public void LoadCards(CardManager library) {
		foreach (CardBaseData cardData in _CardData) {
			library.LoadCard(cardData);
		}

		// Fill in missing data with default cards.
		for (int i = _CardData.Count; i < 64; ++i) {
			CardBaseData nextData = GetDummyData(i);

			library.LoadCard(nextData);
		}
	}

	CardBaseData GetDummyData(int index) {
		CardBaseData result = new CardBaseData ();

		result.Name = string.Format ("Dummy Card {0}", index + 1);
		result.CardType = CardBaseData.Hash (result.Name);
		result.CharacterStats = new int[CardBaseData.NumCharacterStats];
		result.PrimaryAttributes = new int[CardBaseData.NumPrimaryAttributes];
		result.SpecialAbilities = new int[CardBaseData.NumSpecialAbilities];

		result.InitialHP = 10;
		result.MaxHP = 10;
		result.IsAgent = true;
		result.IsCustomer = true;
		result.Payment = 10000;

		return result;
	}
}
