using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    [Header("Elevator")]
    [SerializeField] private Transform cabin;
    [SerializeField] private float floorHeight = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [Min(1)]
    [SerializeField] private int floorsCount = 1;    // UI: count of floors (>=1)
    [Min(1)]
    [SerializeField] private int startFloor = 1;     // UI: 1-based floor index

    [Header("Buttons")]
    [SerializeField] private Transform buttonsParent;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private float buttonSpacing = 0.35f;

    private float baseY;
    private int currentFloor; // zero-based
    private int targetFloor;  // zero-based

    private void Awake()
    {
        if (cabin == null)
            cabin = transform;

        baseY = cabin.position.y;
        int maxFloors = Mathf.Max(1, floorsCount);
        currentFloor = Mathf.Clamp(startFloor - 1, 0, maxFloors - 1);
        targetFloor = currentFloor;
    }

    private void Start()
    {
        GenerateButtons();
        SnapToFloor(currentFloor);
    }

    private void Update()
    {
        float targetY = baseY + targetFloor * floorHeight;
        Vector3 pos = cabin.position;
        pos.y = Mathf.MoveTowards(pos.y, targetY, moveSpeed * Time.deltaTime);
        cabin.position = pos;

        if (Mathf.Approximately(pos.y, targetY))
        {
            currentFloor = targetFloor;
        }
    }

    public void GoToFloor(int floor)
    {
        targetFloor = Mathf.Clamp(floor, 0, Mathf.Max(0, floorsCount - 1));
    }

    public void GoToFloorOneBased(int floorOneBased)
    {
        GoToFloor(floorOneBased - 1);
    }

    public void SetFloors(int count)
    {
        floorsCount = Mathf.Max(1, count);
        GenerateButtons();
    }

    public void SetFloorHeight(float height)
    {
        floorHeight = Mathf.Max(0.1f, height);
    }

    private void SnapToFloor(int floor)
    {
        Vector3 pos = cabin.position;
        pos.y = baseY + floor * floorHeight;
        cabin.position = pos;
    }

    private void GenerateButtons()
    {
        if (buttonsParent == null || buttonPrefab == null)
            return;

        // clear existing
        for (int i = buttonsParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(buttonsParent.GetChild(i).gameObject);
        }

        int total = Mathf.Max(1, floorsCount);
        for (int i = 0; i < total; i++)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonsParent);
            btn.transform.localPosition = new Vector3(0f, -i * buttonSpacing, 0f);

            ElevatorButton eb = btn.GetComponent<ElevatorButton>();
            if (eb == null)
            {
                eb = btn.AddComponent<ElevatorButton>();
            }
            eb.Setup(this, i);

            Usable usable = btn.GetComponent<Usable>();
            if (usable != null)
            {
                usable.SetPrompt($"Floor {i + 1}");
                usable.ClearListeners();
                usable.AddListener(eb.Press);
            }
        }
    }
}
