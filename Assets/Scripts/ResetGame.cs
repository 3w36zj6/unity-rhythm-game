using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ResetGame : MonoBehaviour {
    // Start is called before the first frame update
    public void ButtonClicked() {
        SceneManager.LoadScene("GameScene");
        Debug.Log("aaaa");
    }
}