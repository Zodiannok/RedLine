using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Stored in a character card for a one-directional relationship modifier (this character's preference towards another character)
public struct RelationBaseData {
	// Store the other character's name, as its hash can change while name remains stable.
	public string OtherName;
	
	public int Relationship;
}

public struct AttributeData {
	public static readonly AttributeData EmptyAttribute = new AttributeData();

	public string Name;
}

public struct AbilityData {
	public static readonly AbilityData EmptyAbility = new AbilityData();

	public string Name;
}

public struct CardBaseData {
	public static readonly int NumCharacterStats = 2;
	public static readonly int NumPrimaryAttributes = 2;
	public static readonly int NumSpecialAbilities = 4;

	public static readonly CardBaseData EmptyCard = new CardBaseData();

	// Card Type ID built from card data hash.
	// Stored here as well for easier reverse lookup.
	public long CardType;

	// TODO: Add card visual so that the UI can draw this card
	// public Texture Portrait
	public string Name;

	// Card data as player agent.
	public int InitialHP;
	public int MaxHP;
	public int [] CharacterStats;
	public int [] PrimaryAttributes;
	public int [] SpecialAbilities;

	// Card data as customer.
	public int Payment;

	// Can this card show up as a player-side agent?
	public bool IsAgent;

	// Can this card show up as a customer?
	public bool IsCustomer;

	// Base relationship.
	public RelationBaseData [] Relations;
}

// Attach this to the root to manage all cards in the game.
public class CardManager : MonoBehaviour {

	private Dictionary<long, CardBaseData> _Cards;
	private AttributeData[] _Attributes;
	private AbilityData[] _Abilities;

	public static readonly int NumAttributes = 16;
	public static readonly int NumAbilities = 32;

	// Use this for initialization
	void Start () {
		_Cards = new Dictionary<long, CardBaseData> ();
		_Attributes = new AttributeData[NumAttributes];
		_Abilities = new AbilityData[NumAbilities];
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// Load attribute and ability data
	public bool LoadAttributeAbilityData() {
		// TODO: add new attributes and abilities here.
		AttributeData attrData;
		attrData.Name = "属性1";

		_Attributes [0] = attrData;

		AbilityData abData;
		abData.Name = "技能1";

		_Abilities [0] = abData;

		return true;
	}

	// Load a card from disk.
	public bool LoadCard(string fileName) {
		// TODO: use file content to calculate hash.
		long cardHash = 0;
		if (fileName.Length != 0) {
			// Use a simple method from StackOverflow to get a 64-bit hash.
			string s1 = fileName.Substring(0, fileName.Length / 2);
			string s2 = fileName.Substring(fileName.Length / 2);
			cardHash = (((long)s1.GetHashCode()) << 0x20) | (long)s2.GetHashCode();
		}

		// TODO: Actually load data from file.
		CardBaseData data = new CardBaseData ();
		data.CardType = cardHash;
		data.Name = fileName;
		data.CharacterStats = new int[CardBaseData.NumCharacterStats];
		data.PrimaryAttributes = new int[CardBaseData.NumPrimaryAttributes];
		data.SpecialAbilities = new int[CardBaseData.NumSpecialAbilities];

		_Cards [cardHash] = data;
		return true;
	}

	// If we ever want to unload a card.
	public bool UnloadCard(long cardType) {
		return _Cards.Remove (cardType);
	}

	// List all cards.
	public IEnumerable<CardBaseData> ListCards() {
		return _Cards.Values;
	}

	// Find all cards with a given name.
	public IEnumerable<CardBaseData> FindCardsWithName(string cardName) {
		List<CardBaseData> list = new List<CardBaseData> ();
		foreach (CardBaseData data in _Cards.Values) {
			if (data.Name == cardName) {
				list.Add(data);
			}
		}
		return list;
	}

	// Returns the card base data of a specified card.
	public CardBaseData GetCardBaseData(long cardType) {
		if (_Cards.ContainsKey (cardType)) {
			return _Cards [cardType];
		} else {
			return CardBaseData.EmptyCard;
		}
	}

	// Returns an attribute data
	public AttributeData GetAttributeData(int attributeIndex) {
		if (attributeIndex >= 0 && attributeIndex < NumAttributes) {
			return _Attributes [attributeIndex];
		} else {
			return AttributeData.EmptyAttribute;
		}
	}

	// Returns an ability data
	public AbilityData GetAbilityData(int abilityIndex) {
		if (abilityIndex >= 0 && abilityIndex < NumAbilities) {
			return _Abilities [abilityIndex];
		} else {
			return AbilityData.EmptyAbility;
		}
	}
}
