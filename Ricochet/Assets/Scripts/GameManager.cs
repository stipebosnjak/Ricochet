using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public static Dictionary<int, PlayerManager> Players = new Dictionary<int, PlayerManager>();

        public GameObject localPlayerPrefab;
        public GameObject playerPrefab;

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

        public void SpawnPlayer(int id, string username, Vector3 position, Quaternion rotation)
        {
            GameObject player;
            if (id == Client.Instance.myId)
            {
                player = Instantiate(localPlayerPrefab, position, rotation);
            }
            else
            {
                player = Instantiate(playerPrefab, position, rotation);
            }

            player.GetComponent<PlayerManager>().id = id;
            player.GetComponent<PlayerManager>().username = username;
            Players.Add(id, player.GetComponent<PlayerManager>());
        }
    }
}
