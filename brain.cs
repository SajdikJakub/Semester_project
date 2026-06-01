
using System;
using System.Collections.Generic;
using System.Linq;

public class Movie
{
    public string Name { get; init; }
    public string Genre { get; init; }
    public int Year { get; init; } //year when the movie was released
    public int Runtime { get; init; } //declared in minutes
    public double Rating { get; init; } //6.7 for example
    public string Description { get; init; }
    public bool HasCzechDub { get; init; }
}

public static class CsvLoader
{
    static List<string> ParseCsvLine(string line)
    /* 
    processed one line at the time. 
    We need some extra processing, because in our csv table we use ',' as a separator of fields,
    but ',' can appear in the name or description part as well. therefore, we use double quotes around
    these fields, and we need some extra logic to be able to handle everything
    */
    {
        List<string> result = new List<string>();
        bool inQuotes = false; //determines if we are inside quotes "" or not
        string substring = ""; //we store this value as we parse through the line
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') //if we were not inside quotes now we are, and the other way around
            {
                if (inQuotes) inQuotes = false;
                else inQuotes = true;
            }
            else if (inQuotes) substring += line[i]; //if we are inside quotes we can add the char to substring
            else
            {
                if (line[i] == ',') //whenever we hit ',' and we are not in quotes, we store the value
                {
                    result.Add(substring);
                    substring = "";
                }
                else substring += line[i]; //this stores things that are not in quotes such as year
             
            }
        }
        result.Add(substring); // without this HasCzechDub would not get stored
        return result;
    }
    
    public static List<Movie> Load(string filePath)
    //pretty straight forward
    {
        var movies = new List<Movie>();
        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> f = ParseCsvLine(lines[i]);

            Movie movie = new Movie
            {
                Name = f[0],
                Genre = f[1],
                Year = int.Parse(f[2]),
                Runtime = int.Parse(f[3]),
                Rating = double.Parse(f[4]),
                Description = f[5],
                HasCzechDub = bool.Parse(f[6])
            };
            movies.Add(movie);
        }

        return movies;
    }
}

public class UserPreferences
{
    public string? Genre { get; init; }
    public int? MinYear { get; init; } //from what year should the oldest films be
    public bool? PrefersShortFilm { get; init; }
    public double? MinRating { get; init; }
    public bool? HasCzechDub { get; init; }
}


public static class PreferencesCollector
{
    public static UserPreferences Collect(List<Movie> movies)
    {
        var genres = movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();

        
        static int? AskNumber(string question, int min, int max)
        {
            Console.WriteLine(question);
            if (int.TryParse(Console.ReadLine(), out int response))
            {
                if (response == 0) return null;
                if (response < min || response > max)
                {
                    Console.WriteLine("Number out of range");
                    return null;
                }
                else return response;
            }
            else
            {
                Console.WriteLine("Write a number next time");
                return null;
            }
        }


        static bool? AskYesNo(string question)
        {
            Console.WriteLine(question);
            string response = Console.ReadLine();
            if (response == "yes" || response == "Yes" || response == "y" || response == "Y") return true;
            else if (response == "no" || response == "No" || response == "n" || response == "N") return false;
            else
            {
                Console.WriteLine("Not a valid answer, response is being ignored");
                return null;
            }

        
        }

        Console.WriteLine("What genre are you in the mood for?");
        Console.WriteLine("0. Skip");
        for (int i = 0; i < genres.Count; i++) {
            Console.WriteLine($"{i+1}. {genres[i]}");
        }

        int? genre = AskNumber("Choose a number", 0, genres.Count);
        int? minYear = AskNumber("From what year? (0 to skip)", 1900, 2026);
        int? minRating = AskNumber("Minimum rating? (0 to skip)", 0, 10);
        bool? prefersShort = AskYesNo("Do you prefer shorter films?");
        bool? hasCzechDub = AskYesNo("Czech dub only?");


        return new UserPreferences
        {
            Genre = genre == null ? null : genres[genre.Value - 1],
            MinYear = minYear,
            MinRating = (double?)minRating,
            PrefersShortFilm = prefersShort,
            HasCzechDub = hasCzechDub
        };
        
    }
}


public static class Scorer
{
    public static List<Movie> GetRecommendations(List<Movie> movies, UserPreferences prefs, int topN = 3)
    {
        List<Movie> candidates = new List<Movie>();
        if (prefs.HasCzechDub == true)
        {
            for (int i = 0; i < movies.Count; i++)
            {
                if (movies[i].HasCzechDub) candidates.Add(movies[i]);
            }
        }
        else candidates = movies;
        return candidates.OrderByDescending(movie => ScoreMovie(movie, prefs)).Take(topN).ToList();

        
    }

    private static double ScoreMovie(Movie movie, UserPreferences prefs)
    {
        double sum = 0;
        if (prefs.Genre != null && movie.Genre == prefs.Genre) sum += 40;
        if (prefs.MinYear != null && movie.Year >= prefs.MinYear) sum += 10;
        if (prefs.MinRating != null && movie.Rating >= prefs.MinRating) sum += (movie.Rating - prefs.MinRating.Value)*10;
        if (prefs.PrefersShortFilm == true && movie.Runtime < 90) sum += 20;
        return sum;
    }
}


public static class QueryEngine
{
    public static void Run(List<Movie> movies)
    {
        
        List<string> questions = new List<string> {"Back", "Longest movie", "Highly rated overall", "Czech dub","Newest movie","Surprise me"};
        for (int i = 0; i < questions.Count; i++) Console.WriteLine($"{i}. {questions[i]}");
        
        if (int.TryParse(Console.ReadLine(), out int answer))
        {
            switch (answer)
            {
                case 1:
                    var longest = movies.OrderByDescending(m => m.Runtime).First();
                    Program.PrintMovie(longest);
                    break;
                    
            }
        }
        else
        {
            Console.WriteLine("Write a number");
        }
    }
}



class Program{

    public static void PrintMovie(Movie m)
    {
        Console.WriteLine($"\n{m.Name} ({m.Year}) — {m.Genre} — {m.Runtime} min — {m.Rating}/10");
        Console.WriteLine($"Czech dub: {(m.HasCzechDub ? "Yes" : "No")}");
        Console.WriteLine(m.Description);
    }

    public static void Main(string[] args)
    {
        var movies = CsvLoader.Load("data.csv");
        
        Console.WriteLine("1. Get recommendations");
        Console.WriteLine("2. Query the database");
        
        if (int.TryParse(Console.ReadLine(), out int choice))
        {
            // switch on choice
            // case 1: existing recommendations flow
            if (choice == 1)
                {
                            var prefs = PreferencesCollector.Collect(movies);
                            var recommendations = Scorer.GetRecommendations(movies, prefs);

                            Console.WriteLine("\n--- Tonight's picks ---");
                            foreach (var m in recommendations) PrintMovie(m);
                }
            else if (choice == 2)
                {
                    QueryEngine.Run(movies);
                }
        }
    }
}