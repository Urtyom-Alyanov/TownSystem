using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Services;

/// <summary>
/// здесь собирается карта, сначала рельеф и вода, потом ресурсы
/// </summary>
public class MapGenerator
{
    // типы местности сами не меняются, поэтому можно использовать одни и те же объекты для разных клеток
    private const int riverDepth = 3;
    private static readonly Plain plain = new Plain();
    private static readonly Forest forest = new Forest();
    private static readonly Mountain mountain = new Mountain();
    private static readonly WaterBody water = new WaterBody();
    private static readonly WaterBody river = new WaterBody(true);
    private static readonly NaturalResource[] mineralResources = { NaturalResource.Iron, NaturalResource.Copper, NaturalResource.Coal };
    private static readonly NaturalResource[] fuelResources = { NaturalResource.Oil, NaturalResource.Gas };
    private readonly Random random;

    // Если передали seed, запускаем с ним генератор случайных чисел чтоб карта повторялась
    public MapGenerator(int? seed = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    // создаём карту нужного размера, потом заполняем её местностью и ресурсами
    public Models.Map Generate(int width, int height)
    {
        Models.Map map = new Models.Map(width, height, fillWithPlains: false);

        // по редкой сетке делаем большие участки, по более частой добавляем неровности
        double[,] regions = CreateHeightPoints(6, 4);
        double[,] hills = CreateHeightPoints(12, 9);
        double[,] details = CreateHeightPoints(30, 19);

        // У края опускаем землю, чтоб там получился берег с заливами
        int coastColumn = random.Next(2) == 0 ? 0 : regions.GetLength(0) - 1;
        int coastRow = random.Next(regions.GetLength(1) - 1);
        regions[coastColumn, coastRow] = -1.2;
        regions[coastColumn, coastRow + 1] = -1.05;

        // дальше от воды поднимаем часть земли под горы
        int ridgeColumn = coastColumn == 0 ? regions.GetLength(0) - 2 : 1;
        int ridgeRow = random.Next(1, regions.GetLength(1) - 1);
        regions[ridgeColumn, ridgeRow] = 1.24;
        regions[ridgeColumn, ridgeRow + 1] = 0.85;

        double[,] surface = CreateHeightPoints(70, 43);
        int[,] heights = new int[width, height];

        for (int y = 0; y < height; y++)
        {
            // приводим координаты к диапазону от 0 до 1, это подходит и для прямоугольной карты
            double mapY = (double)y / Math.Max(1, height - 1);
            for (int x = 0; x < width; x++)
            {
                double mapX = (double)x / Math.Max(1, width - 1);
                double region = ReadHeight(regions, mapX, mapY);
                double hill = ReadHeight(hills, mapX, mapY);
                double detail = ReadHeight(details, mapX, mapY);
                double surfaceDetail = ReadHeight(surface, mapX, mapY);
                heights[x, y] = (int)(148 + 218 * region + 75 * hill + 27 * detail + 10 * surfaceDetail);
            }
        }

        bool[,]? rivers = null;
        if (width >= 128 && height >= 64)
        {
            int mouthX = coastColumn == 0 ? 0 : width - 1;
            int mouthY = (int)((coastRow + 0.35) * (height - 1) / (regions.GetLength(1) - 1));
            rivers = CreateRivers(heights, mouthX, mouthY);
            LevelLowRiverBanks(heights, rivers);
        }

        double[,] depositDensity = CreateHeightPoints(18, 11);
        double[,] preferredResources = CreateHeightPoints(15, 9);

        // Лес растёт отдельными участками, высота сама по себе его не определяет
        double[,] forestRegions = CreateHeightPoints(17, 11);
        double[,] forestEdges = CreateHeightPoints(38, 24);
        for (int y = 0; y < height; y++)
        {
            double mapY = (double)y / Math.Max(1, height - 1);
            for (int x = 0; x < width; x++)
            {
                double mapX = (double)x / Math.Max(1, width - 1);
                int elevation = heights[x, y];
                bool isRiver = rivers != null && rivers[x, y];
                Terrain terrain;
                if (isRiver)
                    terrain = river;
                else if (elevation == 0 && rivers != null && IsBesideRiver(rivers, x, y))
                    terrain = plain;
                else
                    terrain = GenerateTerrain(elevation, mapX, mapY, forestRegions, forestEdges);
                Relief relief = isRiver ? new Relief(FindRiverLevel(heights, x, y) - riverDepth, riverDepth) : new Relief(elevation);
                ResourceDeposit? deposit = GenerateDeposit(terrain, elevation, mapX, mapY, depositDensity, preferredResources);
                MapCell cell = new MapCell(new Coordinates(x, y), terrain, relief, deposit);
                map.SetCell(cell);
            }
        }

        return map;
    }

    // Делаем главную реку с притоками и отмечаем где проходит русло
    private bool[,] CreateRivers(int[,] heights, int mouthX, int mouthY)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        bool[,] rivers = new bool[width, height];
        int direction = mouthX == 0 ? -1 : 1;
        int sourceX = mouthX == 0 ? width * 4 / 5 : width / 5;
        int sourceY = random.Next(height / 4, height * 3 / 4);

        // сначала основная река до берега, потом к ней добавляем два притока
        int[] mainCourse = DrawRiver(heights, rivers, sourceX, sourceY, mouthX, mouthY, true);
        int firstJoin = mainCourse.Length / 3;
        int secondJoin = mainCourse.Length * 2 / 3;
        DrawTributary(heights, rivers, mainCourse, sourceX, direction, firstJoin, -1);
        DrawTributary(heights, rivers, mainCourse, sourceX, direction, secondJoin, 1);
        return rivers;
    }

    // рисуем приток от его истока до нужного места на главной реке
    private void DrawTributary(int[,] heights, bool[,] rivers, int[] mainCourse, int sourceX, int direction, int join, int side)
    {
        int height = heights.GetLength(1);
        int joinX = sourceX + join * direction;
        int startX = sourceX + join / 3 * direction;
        int startY = Math.Clamp(mainCourse[join] + side * height / 4, 0, height - 1);
        DrawRiver(heights, rivers, startX, startY, joinX, mainCourse[join], false);
    }

    // Проводим русло между двумя точками и запоминаем путь, к нему потом подойдут притоки
    private int[] DrawRiver(int[,] heights, bool[,] rivers, int startX, int startY, int endX, int endY, bool main)
    {
        int height = heights.GetLength(1);
        int length = Math.Abs(endX - startX);
        int direction = endX < startX ? -1 : 1;
        int[] course = new int[length + 1];
        double[,] bends = CreateHeightPoints(9, 2);
        double[,] smallBends = CreateHeightPoints(23, 2);
        double[,] bankBends = CreateHeightPoints(61, 2);
        int previousY = startY;

        for (int step = 0; step <= length; step++)
        {
            double progress = (double)step / Math.Max(1, length);
            double straight = startY + (endY - startY) * progress;
            double bend = ReadHeight(bends, progress, 0.25) * 0.1;
            bend += ReadHeight(smallBends, progress, 0.25) * 0.02;
            bend += ReadHeight(bankBends, progress, 0.25) * 0.006;

            // к истоку и устью убираем изгибы, чтоб река попала куда надо
            int y = (int)Math.Round(straight + bend * height * Math.Sin(Math.PI * progress));
            y = Math.Clamp(y, 0, height - 1);
            int x = startX + step * direction;
            course[step] = y;

            // Соединяем соседние столбцы, основная река расширяется ближе к устью
            int northBank = main && progress > 0.85 ? 1 : 0;
            int southBank = main && progress > 0.65 ? 1 : 0;
            int top = Math.Max(0, Math.Min(y, previousY) - northBank);
            int bottom = Math.Min(height - 1, Math.Max(y, previousY) + southBank);
            for (int row = top; row <= bottom; row++)
            {
                if (heights[x, row] < 0)
                    continue;

                rivers[x, row] = true;
            }

            previousY = y;
        }

        return course;
    }

    // Делаем короткие пологие берега там где земля и так почти на уровне моря
    private static void LevelLowRiverBanks(int[,] heights, bool[,] rivers)
    {
        for (int y = 0; y < heights.GetLength(1); y++)
        {
            for (int x = 0; x < heights.GetLength(0); x++)
            {
                int height = heights[x, y];
                if (height > 0 && height <= 3 && !rivers[x, y] && IsBesideRiver(rivers, x, y))
                    heights[x, y] = 0;
            }
        }
    }

    // Проверяем четыре соседние клетки, которые делят границу с этой клеткой
    private static bool IsBesideRiver(bool[,] rivers, int x, int y)
    {
        return (x > 0 && rivers[x - 1, y]) || (y > 0 && rivers[x, y - 1]) ||
               (x + 1 < rivers.GetLength(0) && rivers[x + 1, y]) || (y + 1 < rivers.GetLength(1) && rivers[x, y + 1]);
    }

    // Берём высоту воды рядом с руслом, чтобы река могла идти и выше уровня моря
    private static int FindRiverLevel(int[,] heights, int x, int y)
    {
        int level = heights[x, y];
        if (x > 0)
            level = Math.Min(level, heights[x - 1, y]);
        if (y > 0)
            level = Math.Min(level, heights[x, y - 1]);
        if (x + 1 < heights.GetLength(0))
            level = Math.Min(level, heights[x + 1, y]);
        if (y + 1 < heights.GetLength(1))
            level = Math.Min(level, heights[x, y + 1]);

        return Math.Max(0, level);
    }

    // Заполняем сетку случайными числами, потом используем её для высот и других расчётов
    private double[,] CreateHeightPoints(int width, int height)
    {
        double[,] points = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                points[x, y] = random.NextDouble() * 2 - 1;
            }
        }

        return points;
    }

    // берём значение между точками сетки, чтоб оно менялось плавно
    private static double ReadHeight(double[,] points, double x, double y)
    {
        double column = x * (points.GetLength(0) - 1);
        double row = y * (points.GetLength(1) - 1);
        int left = Math.Min((int)column, points.GetLength(0) - 2);
        int top = Math.Min((int)row, points.GetLength(1) - 2);

        // сначала считаем переход влево вправо, потом сверху вниз
        double across = Smooth(column - left);
        double down = Smooth(row - top);
        double upper = points[left, top] * (1 - across) + points[left + 1, top] * across;
        double lower = points[left, top + 1] * (1 - across) + points[left + 1, top + 1] * across;
        return upper * (1 - down) + lower * down;
    }

    // Делает переход между значениями помягче
    private static double Smooth(double part)
    {
        // рядом с точками значение меняется медленнее
        return part * part * (3 - 2 * part);
    }

    // По высоте определяем воду и горы, а на остальной суше решаем где растёт лес
    private Terrain GenerateTerrain(int height, double x, double y, double[,] forestRegions, double[,] forestEdges)
    {
        if (height < 0)
            return water;

        if (height >= 300)
            return mountain;

        double trees = ReadHeight(forestRegions, x, y) + 0.3 * ReadHeight(forestEdges, x, y);
        return trees > 0.16 ? forest : plain;
    }

    // Смотрим на местность и район, решаем будет ли тут ресурс и сколько его
    private ResourceDeposit? GenerateDeposit(Terrain terrain, int elevation, double x, double y, double[,] density, double[,] preference)
    {
        if (terrain is Forest)
            return new ResourceDeposit(NaturalResource.Wood, random.Next(500, 2001));

        bool inMountains = terrain is Mountain;
        if (!inMountains && (terrain is not Plain || elevation > 135))
            return null;

        // в богатом районе клеток с ресурсом больше
        double richness = (ReadHeight(density, x, y) + 1) / 2;
        double chance = inMountains ? 0.025 + 0.24 * richness * richness : 0.008 + 0.11 * richness * richness;
        if (random.NextDouble() >= chance)
            return null;

        // в районе чаще выпадает один ресурс, иногда попадаются и другие
        NaturalResource[] resources = inMountains ? mineralResources : fuelResources;
        double preferred = (ReadHeight(preference, x, y) + 1) / 2;
        int index = Math.Min((int)(preferred * resources.Length), resources.Length - 1);
        if (random.Next(4) == 0)
            index = random.Next(resources.Length);

        int amount = 500 + (int)(richness * 3000) + random.Next(100, 900);
        return new ResourceDeposit(resources[index], amount);
    }
}
