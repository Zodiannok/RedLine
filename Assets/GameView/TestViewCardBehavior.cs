using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TestViewCardBehavior : MonoBehaviour {

	private CharacterCardBehavior _CardInstance;  

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public CharacterCardBehavior CardInstance {
		get {
			return _CardInstance;
		}
		set {
			_CardInstance = value;
			if (_CardInstance != null) {
				RefreshDisplay();
			}
		}
	}

	void RefreshDisplay() {
		SetCardName (_CardInstance.CardName);
		SetCardStats (_CardInstance.CharacterStats);
	}

	void SetCardName(string name) {
		Text nameText = transform.FindChild ("NameText").GetComponent<Text> ();
		nameText.text = name;
	}

	void SetCardStats(int[] charStats) {
		Text propertyText = transform.FindChild ("PropertyText").GetComponent<Text> ();
		string grade0 = CharacterCardBehavior.StatLetterGrade(charStats[0]);
		string grade1 = CharacterCardBehavior.StatLetterGrade(charStats[1]);
		propertyText.text = string.Format ("容姿: {0} 技术: {1}", grade0, grade1);
	}

	public void OnMouseEnter() {
		Image image = GetComponent<Image> ();
		image.color = new Color (1, 0, 0, 100.0f / 255.0f);
	}

	public void OnMouseExit() {
		Image image = GetComponent<Image> ();
		image.color = new Color (1, 1, 1, 100.0f / 255.0f);
	}
}
