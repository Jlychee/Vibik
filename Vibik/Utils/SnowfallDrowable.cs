
namespace Vibik.Utils;

public class Snowflake
{
    public float X;
    public float Y;
    public float Radius;
    public float SpeedY;
    public float SwayAmplitude;
    public float SwayPhase;
}

public class SnowfallDrawable : IDrawable
{
    private readonly List<Snowflake> snowflakes = new();
    private readonly Random random = new();

    private float width;
    private float height;

    public SnowfallDrawable(int count = 80)
    {
        for (int i = 0; i < count; i++)
            snowflakes.Add(CreateSnowflake(
                x: (float)random.NextDouble(),
                y: (float)random.NextDouble()
            ));
    }

    private Snowflake CreateSnowflake(float x, float y)
    {
        return new Snowflake
        {
            X = x,
            Y = y,
            Radius = 2f + (float)random.NextDouble() * 3f,
            SpeedY = 40f + (float)random.NextDouble() * 60f,     // скорость падения
            SwayAmplitude = 10f + (float)random.NextDouble() * 20f, // амплитуда «качания»
            SwayPhase = (float)random.NextDouble() * MathF.Tau
        };
    }

    /// <summary>
    /// Обновляем координаты снежинок
    /// </summary>
    public void Update(float dt)
    {
        if (width <= 0 || height <= 0)
            return;

        foreach (var f in snowflakes)
        {
            f.Y += (f.SpeedY / height) * dt; // нормируем к высоте

            if (f.Y > 1.1f) // вышла за низ — перекидываем наверх
            {
                f.Y = -0.1f;
                f.X = (float)random.NextDouble();
            }
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        width = dirtyRect.Width;
        height = dirtyRect.Height;

        canvas.SaveState();
        canvas.Antialias = true;
        canvas.FillColor = Colors.White;

        foreach (var f in snowflakes)
        {
            // Немного горизонтального «качания»
            float sway = f.SwayAmplitude *
                         (float)Math.Sin(f.Y * 6f + f.SwayPhase);

            float x = f.X * width + sway;
            float y = f.Y * height;

            canvas.FillCircle(x, y, f.Radius);
        }

        canvas.RestoreState();
    }
}
