using UnityEngine;
using System.Collections;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f; // How long the bullet will exist before being destroyed
    [SerializeField] private float damage = 10f; // Damage the bullet deals on hit
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private GameObject bulletHolePrefab;

    void Start()
    {
        // Destroy the bullet after its lifetime
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Bullet collided with: {collision.gameObject.name}");

        // Get the contact point and normal of the collision
        ContactPoint contact = collision.contacts[0];
        Quaternion rot = Quaternion.LookRotation(contact.normal);
        Vector3 pos = contact.point;

        // Instantiate Impact Effect
        if (impactEffectPrefab != null)
        {
            GameObject impactGO = Instantiate(impactEffectPrefab, pos, rot);
            Destroy(impactGO, 2f); // Destroy impact effect after 2 seconds
        }

        // Instantiate Bullet Hole
        if (bulletHolePrefab != null)
        {
            GameObject bulletHoleGO = Instantiate(bulletHolePrefab, contact.point + contact.normal * 0.01f, Quaternion.LookRotation(contact.normal));
            // Parent the bullet hole to the collided object to make it move with the object
            bulletHoleGO.transform.SetParent(collision.gameObject.transform);
            Destroy(bulletHoleGO, 10f); // Destroy bullet hole after 10 seconds
        }

        // Deal damage to an object with a Health component
        Health targetHealth = collision.gameObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    // You can add a method to set initial velocity if not using Rigidbody.velocity directly
    public void SetInitialVelocity(Vector3 velocity)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }
}
