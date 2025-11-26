using UnityEngine;

/// <summary>
/// Компонент для вращения объекта по всем осям с контролем скорости через слайдеры.
/// Диапазон значений слайдеров: -100 до +100, где:
/// -100 = вращение против часовой стрелки (минус направление)
///    0 = нет вращения
/// +100 = вращение по часовой стрелке (плюс направление)
/// </summary>
public class Rotator : MonoBehaviour
{
    [Header("Rotation Speed Settings")]
    [SerializeField]
    [Range(-100f, 100f)]
    private float xRotationSpeed = 0f;

    [SerializeField]
    [Range(-100f, 100f)]
    private float yRotationSpeed = 0f;

    [SerializeField]
    [Range(-100f, 100f)]
    private float zRotationSpeed = 0f;

    [Header("Speed Multiplier")]
    [SerializeField]
    private float speedMultiplier = 1f;

    private void Update()
    {
        RotateObject();
    }

    private void RotateObject()
    {
        // Создаем вектор вращения на основе значений слайдеров
        Vector3 rotationAxis = new Vector3(xRotationSpeed, yRotationSpeed, zRotationSpeed);

        // Применяем множитель скорости для удобства масштабирования
        rotationAxis *= speedMultiplier;

        // Вращаем объект
        transform.Rotate(rotationAxis * Time.deltaTime, Space.Self);
    }

    // Методы для программного управления скоростью вращения
    public void SetXRotationSpeed(float speed)
    {
        xRotationSpeed = Mathf.Clamp(speed, -100f, 100f);
    }

    public void SetYRotationSpeed(float speed)
    {
        yRotationSpeed = Mathf.Clamp(speed, -100f, 100f);
    }

    public void SetZRotationSpeed(float speed)
    {
        zRotationSpeed = Mathf.Clamp(speed, -100f, 100f);
    }

    public void SetAllRotationSpeeds(float x, float y, float z)
    {
        SetXRotationSpeed(x);
        SetYRotationSpeed(y);
        SetZRotationSpeed(z);
    }

    // Геттеры для получения текущих значений
    public float GetXRotationSpeed() => xRotationSpeed;
    public float GetYRotationSpeed() => yRotationSpeed;
    public float GetZRotationSpeed() => zRotationSpeed;

    // Метод для остановки вращения
    public void StopRotation()
    {
        xRotationSpeed = 0f;
        yRotationSpeed = 0f;
        zRotationSpeed = 0f;
    }

    // Метод для сброса в начальные значения
    public void ResetRotation()
    {
        StopRotation();
    }
}

