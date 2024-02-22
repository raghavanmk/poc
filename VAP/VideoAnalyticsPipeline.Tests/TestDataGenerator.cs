namespace VideoAnalyticsPipeline.Tests;
public class TestDataGenerator
{
    public static IEnumerable<(float[], long, string, int, float, bool)> GenerateTestDataFor_IfCoordinatesNotProcessed()
    {
        // 1706679450000 1/31/2024 5:37:30 AM +00:00

        // Arrange

        // first set of coordinates 
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(0), "Q2UV-N5GT-HURS", 2, 0.9f, true);

        // same coordinates, confidence, class but different camera and within same time interval
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(5), "Q2UV-5LPF-HURS", 2, 0.9f, true);

        // same coordinates, camera, confidence, class but and same time interval
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(10), "Q2UV-N5GT-HURS", 2, 0.9f, false);

        // same coordinates, camera, confidence,but different class id and within same time interval
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(20), "Q2UV-N5GT-HURS", 1, 0.9f, true);

        // same coordinates, camera, class, different confidence within same range and within same time interval
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(30), "Q2UV-N5GT-HURS", 2, 0.8f, false);

        // same coordinates, camera, class, confidence but different range and within same time interval
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(40), "Q2UV-N5GT-HURS", 2, 0.6f, true);

        // same coordinates, camera, confidence, class but different timestamp > 1000ms 
        yield return new([0.1f, 0.2f, 0.3f, 0.4f], Timestamp(1020), "Q2UV-N5GT-HURS", 2, 0.9f, true);
    }

    public static IEnumerable<(string, Output, long, bool)> GenerateTestDataFor_IfCoordinatesNotNeighbours()
    {
        // initialize the data
        yield return new("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 1, Location = [0.24f, 0.12f, 0.46f, 0.31f], Score = 0.75f }, Timestamp(30), true);

        // same data as the above, with the time difference less than time constraint
        yield return new("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 1, Location = [0.24f, 0.12f, 0.46f, 0.31f], Score = 0.75f }, Timestamp(40), false);

        // same data as the above, with the time difference greater that the time constaint
        yield return new("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 1, Location = [0.24f, 0.12f, 0.46f, 0.31f], Score = 0.75f }, Timestamp(1040), true);
        yield return new("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 4, Location = [0.41f, 0.28f, 0.62f, 0.47f], Score = 0.75f }, Timestamp(1050), true);

        // same camera, time difference less than time constraint, neighbor to the above data
        yield return new("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 5, Location = [0.31f, 0.18f, 0.52f, 0.37f], Score = 0.75f }, Timestamp(1060), false);

        // new camera, new data
        yield return new("Q2UV-9LPF-KURS", new Output { Class = 1, Id = 3, Location = [0.36f, 0.27f, 0.58f, 0.46f], Score = 0.75f }, Timestamp(2030), true);

        // same camera as the above, neighbor to the above data, but time difference with the above data is greater than time constraint 
        yield return new("Q2UV-9LPF-KURS", new Output { Class = 1, Id = 6, Location = [0.46f, 0.23f, 0.68f, 0.51f], Score = 0.75f }, Timestamp(3040), true);
    }
    static long Timestamp(int milliseconds)
    {
        const long baseTime = 1706679450000;

        return baseTime + milliseconds;
    }


}

