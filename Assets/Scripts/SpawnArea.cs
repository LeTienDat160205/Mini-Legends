using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public static SpawnArea FindFor(Team team)
    {
        foreach (SpawnArea spawn in FindObjectsByType<SpawnArea>(FindObjectsSortMode.None))
        {
            TeamMember member = spawn.GetComponent<TeamMember>();
            if (member != null && member.team == team) return spawn;
        }
        return null;
    }

    public void PlaceFeet(Transform character)
    {
        if (character == null) return;
        Rigidbody2D body = character.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;

        SpriteRenderer sprite = character.GetComponent<SpriteRenderer>();
        Collider2D collider = character.GetComponent<Collider2D>();
        float feetY = collider != null ? collider.bounds.min.y :
            sprite != null ? sprite.bounds.min.y : character.position.y;
        Vector3 destination = character.position;
        destination.x += transform.position.x - character.position.x;
        destination.y += transform.position.y - feetY;
        character.position = destination;
        if (body != null) body.position = destination;
    }
}
