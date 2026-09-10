using UnityEngine;

/// <summary>
/// Набор карт без внешних ассетов. Каждая карта меняет палитру, освещение,
/// расположение препятствий и форму маршрута трафика.
/// </summary>
public static class ArenaMapLibrary
{
    public struct MapTheme
    {
        public string name;
        public string description;
        public Color background;
        public Color floor;
        public Color wall;
        public Color lane;
        public Color hazard;
        public Color player;
        public Color playerTwo;
        public Color tail;
        public Color light;
        public float fogDensity;
    }

    public const int MapCount = 4;

    public static MapTheme Get(int index)
    {
        switch (Mathf.Abs(index) % MapCount)
        {
            case 1:
                return new MapTheme
                {
                    name = "DUSTLINE OUTPOST",
                    description = "Песчаный форпост",
                    background = new Color(0.13f, 0.055f, 0.025f),
                    floor = new Color(0.27f, 0.13f, 0.055f),
                    wall = new Color(0.55f, 0.25f, 0.08f),
                    lane = new Color(0.75f, 0.36f, 0.1f),
                    hazard = new Color(0.8f, 0.22f, 0.05f),
                    player = new Color(0.3f, 1f, 0.55f),
                    playerTwo = new Color(1f, 0.72f, 0.18f),
                    tail = new Color(0.25f, 0.42f, 0.1f),
                    light = new Color(1f, 0.55f, 0.23f),
                    fogDensity = 0.012f
                };
            case 2:
                return new MapTheme
                {
                    name = "FROSTBYTE LAB",
                    description = "Ледяная лаборатория",
                    background = new Color(0.02f, 0.08f, 0.14f),
                    floor = new Color(0.08f, 0.2f, 0.27f),
                    wall = new Color(0.25f, 0.75f, 0.9f),
                    lane = new Color(0.35f, 0.82f, 1f),
                    hazard = new Color(0.55f, 0.9f, 1f),
                    player = new Color(0.25f, 1f, 0.95f),
                    playerTwo = new Color(1f, 0.4f, 0.78f),
                    tail = new Color(0.05f, 0.35f, 0.55f),
                    light = new Color(0.35f, 0.75f, 1f),
                    fogDensity = 0.016f
                };
            case 3:
                return new MapTheme
                {
                    name = "TOXIC GARDENS",
                    description = "Токсичные сады",
                    background = new Color(0.018f, 0.09f, 0.045f),
                    floor = new Color(0.035f, 0.19f, 0.1f),
                    wall = new Color(0.2f, 0.7f, 0.24f),
                    lane = new Color(0.48f, 1f, 0.2f),
                    hazard = new Color(0.75f, 0.12f, 0.48f),
                    player = new Color(0.55f, 1f, 0.2f),
                    playerTwo = new Color(0.35f, 0.8f, 1f),
                    tail = new Color(0.1f, 0.38f, 0.12f),
                    light = new Color(0.35f, 1f, 0.3f),
                    fogDensity = 0.014f
                };
            default:
                return new MapTheme
                {
                    name = "NEON CIRCUIT",
                    description = "Ночная трасса",
                    background = new Color(0.025f, 0.055f, 0.12f),
                    floor = new Color(0.035f, 0.09f, 0.14f),
                    wall = new Color(0.08f, 0.22f, 0.3f),
                    lane = new Color(0.06f, 0.25f, 0.28f),
                    hazard = new Color(0.65f, 0.16f, 0.28f),
                    player = new Color(0.16f, 1f, 0.68f),
                    playerTwo = new Color(1f, 0.35f, 0.78f),
                    tail = new Color(0.05f, 0.34f, 0.42f),
                    light = new Color(0.1f, 0.85f, 0.95f),
                    fogDensity = 0.009f
                };
        }
    }

    public static Vector3 RoutePoint(int mapIndex, int pointIndex, int pointCount, Vector2 arenaSize)
    {
        float t = pointIndex * Mathf.PI * 2f / pointCount;
        float x = arenaSize.x * 0.38f;
        float z = arenaSize.y * 0.38f;
        switch (Mathf.Abs(mapIndex) % MapCount)
        {
            case 1: // Широкая песчаная петля.
                return new Vector3(Mathf.Cos(t) * x, 0.75f, Mathf.Sin(t) * z * 0.82f);
            case 2: // Лаборатория с мягкими «ледяными» волнами.
                return new Vector3(Mathf.Cos(t) * x * (0.84f + 0.1f * Mathf.Cos(3f * t)), 0.75f, Mathf.Sin(t) * z);
            case 3: // Перекрёстная траектория токсичного сада.
                return new Vector3(Mathf.Sin(t) * x, 0.75f, Mathf.Sin(2f * t) * z * 0.62f);
            default:
                return new Vector3(Mathf.Cos(t) * x, 0.75f, Mathf.Sin(t) * z);
        }
    }

    public static Vector3 ObstaclePoint(int mapIndex, int index, int obstacleCount, Vector2 arenaSize)
    {
        float halfX = arenaSize.x * 0.38f;
        float halfZ = arenaSize.y * 0.38f;
        switch (Mathf.Abs(mapIndex) % MapCount)
        {
            case 1:
                return new Vector3(((index % 5) - 2) * 7f, 1f, ((index / 5) % 3 - 1) * 9f);
            case 2:
                return new Vector3(Mathf.Cos(index * 2.4f) * 14f, 1f, Mathf.Sin(index * 2.4f) * 14f);
            case 3:
                return new Vector3(Mathf.Sin(index * 1.8f) * halfX * 0.7f, 1f, Mathf.Cos(index * 1.3f) * halfZ * 0.65f);
            default:
                float angle = index * 2.39996f;
                float radius = 11f + (index % 4) * 3.1f;
                return new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
        }
    }
}
