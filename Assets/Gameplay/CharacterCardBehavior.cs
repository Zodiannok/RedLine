using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Store character card data in this component.
// This represents one character card during a play session, thus it can upgrade as the game goes on.
public class CharacterCardBehavior : MonoBehaviour {

	// ID of this card in this game.
	// A card will be assigned an ID when it's created.
	public int CardID { get; internal set; }

	// A 64-bit int designating the card's type.
	// Call CardManager.GetCardBaseData(CardType) to get this card's base data.
	public long CardType { get; internal set; }

	// Player index that currently owns the card.
	// A -1 index means that the card is currently free.
	public int Owner { get; internal set; }

	// Current HP
	public int CurrentHP { get; internal set; }

	// Max HP
	public int MaxHP { get; internal set; }

	// Character stats
	public int[] CharacterStats { get; internal set; }

	// Primary attribute values
	public int[] PrimaryAttributes { get; internal set; }

	// Special abilities (ID)
	public int[] SpecialAbilities { get; internal set; }

	// TODO: Add card visual so that the UI can draw this card
	// public Texture Portrait
	public string CardName { get; internal set; }

	// Activity log.
	public List<string> ActivityLog { get; internal set; }

	// TODO: Item slot
	// public Item

	// Whether the card is exhausted (can't be used again) in this round.
	public bool Exhausted { get; internal set; }

	// Stores the card manager for easier future references.
	private CardManager _CardManagerRef;

	// Object construction
	void Awake () {
		CharacterStats = new int[CardBaseData.NumCharacterStats];
		PrimaryAttributes = new int[CardBaseData.NumPrimaryAttributes];
		SpecialAbilities = new int[CardBaseData.NumSpecialAbilities];
		ActivityLog = new List<string> ();
	}

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// Initialize the card's data
	public void InitializeData(int cardID, CardManager manager, long cardType)
	{
		_CardManagerRef = manager;
		CardID = cardID;
		CardType = cardType;

		CardBaseData data = _CardManagerRef.GetCardBaseData (cardType);
		if (!data.Equals(CardBaseData.EmptyCard)) {
			CurrentHP = data.InitialHP;
			MaxHP = data.MaxHP;
			for (int i = 0; i < CardBaseData.NumCharacterStats; ++i) {
				CharacterStats[i] = data.CharacterStats[i];
			}
			for (int i = 0; i < CardBaseData.NumPrimaryAttributes; ++i) {
				PrimaryAttributes[i] = data.PrimaryAttributes[i];
			}
			for (int i = 0; i < CardBaseData.NumSpecialAbilities; ++i) {
				SpecialAbilities[i] = data.SpecialAbilities[i];
			}
			// Portrait = data.Portrait;
			CardName = data.Name;
		}

		Owner = -1;
		Exhausted = false;
		ActivityLog.Clear ();
	}

	// Converts a stat value (integer) to a letter grade (SABCDE)
	static public string StatLetterGrade(int statValue) {
		return "A";
	}
}
