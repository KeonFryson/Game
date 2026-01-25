using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2.0f;
    public int maxEnemies = 5;
    public int currentEnemies = 0;
    private float timer;

    [SerializeField] bool ColliderSpwan = false;
    void Start()
    {
        timer = spawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        timer = Mathf.Max(0f, timer - Time.deltaTime);

        if (timer <= 0f && !ColliderSpwan)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    public void SpawnEnemy()
    {
        if (currentEnemies > maxEnemies)
        {
            return;
        }

        Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        timer = spawnInterval;
        currentEnemies++;
    }

    public void OnTriggerEnter(Collider other)
    {
        if(ColliderSpwan)
        {
            return;
        }


        if (other.CompareTag("Player"))
        {
           SpawnEnemy();
        }
    }
}
