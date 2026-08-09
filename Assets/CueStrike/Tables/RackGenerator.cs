using UnityEngine;

public static class RackGenerator
{
    // Assumes CueStrikePhysicsManager.Instance has ballPrefab assigned or asset created
    public static void Generate8BallRack(Vector3 center, float ballRadius = 0.0525f)
    {
        // standard 8-ball triangle (5 rows): 1+2+3+2+1 layout center at origin
        var mgr = CueStrikePhysicsManager.Instance;
        if (mgr == null) return;
        Vector3 start = center;
        float spacing = ballRadius * 2f * 1.01f;
        int id = 1;
        int rows = 5;
        for (int r = 0; r < rows; r++)
        {
            int count = r + 1;
            float offsetZ = -(rows - 1) * spacing * 0.5f + r * spacing;
            float rowStartX = - (count - 1) * spacing * 0.5f;
            for (int c = 0; c < count; c++)
            {
                Vector3 pos = start + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ);
                mgr.SpawnBall(pos, id++);
            }
        }
    }

    public static void Generate9BallRack(Vector3 center, float ballRadius = 0.0525f)
    {
        // 9-ball diamond layout: rows 1,2,3,2,1 (same as 8-ball but arranged as diamond)
        Generate8BallRack(center, ballRadius); // placeholder, caller can re-order if needed
    }

    public static void GenerateSnookerRack(Vector3 center, float ballRadius = 0.0525f)
    {
        // Snooker uses 15 red balls in a triangle (rows 1..5) and colored balls placed separately
        var mgr = CueStrikePhysicsManager.Instance;
        if (mgr == null) return;
        Vector3 start = center;
        float spacing = ballRadius * 2f * 1.01f;
        int id = 1;
        int rows = 5;
        for (int r = 0; r < rows; r++)
        {
            int count = r + 1;
            float offsetZ = -(rows - 1) * spacing * 0.5f + r * spacing;
            float rowStartX = - (count - 1) * spacing * 0.5f;
            for (int c = 0; c < count; c++)
            {
                Vector3 pos = start + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ);
                mgr.SpawnBall(pos, id++);
            }
        }

        // colored balls: place at standard snooker positions (simplified)
        mgr.SpawnBall(center + new Vector3(0f, ballRadius, 2f * spacing), 100); // pink
        mgr.SpawnBall(center + new Vector3(0f, ballRadius, -2f * spacing), 101); // black
        mgr.SpawnBall(center + new Vector3(-2f * spacing, ballRadius, -4f * spacing), 102); // blue as example
    }
}
