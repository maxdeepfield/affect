using UnityEngine;

public class ElevatorButton : MonoBehaviour
{
    [SerializeField] private int floorIndex = 0;

    private ElevatorController elevator;

    public void Setup(ElevatorController controller, int index)
    {
        elevator = controller;
        floorIndex = index;
    }

    public void Press()
    {
        if (elevator != null)
        {
            elevator.GoToFloor(floorIndex);
        }
    }
}
