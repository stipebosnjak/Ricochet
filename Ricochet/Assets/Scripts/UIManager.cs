using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;

        public GameObject startMenu;
        public InputField usernameField;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.Log("Instance already exists, destroying object!");
                Destroy(this);
            }
        }

        public void ConnectToServer()
        {
            startMenu.SetActive(false);
            usernameField.interactable = false;
            Client.Instance.ConnectToServer();
        }
    }
}
