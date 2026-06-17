
using System;
using System.Collections.Generic;
using System.Linq;
public class Movie
{
    // Immutable record of a single movie loaded from the csv
    public string Name { get; init; }
    public string Genre { get; init; }
    public int Year { get; init; }
    public int Runtime { get; init; } //in minutes
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
    //build the actual list of movies form data.csv,
    //we expect exactly 7 collumns in the correct order, Name, Genre, Year, Runtime, Rating, Description, HasCzechDub.
    //there is no exception handling, we expect the csv table to be in a correct format
    {
        var movies = new List<Movie>();
        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++) //we skip index , because thats the header of the table
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
    // class where we store the user's preferences, we use null for invalid input
    //or if any given field has no preferences
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
        // first we collect users preferences through the Console prompts
        // genres are pulled dynamically from the movies we have loaded
        // invalid or 'skip' answers become 'null'
        List<string> genres = new List<string>();
        foreach (Movie m in movies)
        {
            if (genres.Contains(m.Genre) == false)
            {
                genres.Add(m.Genre);
            }
        }
        genres.Sort();

        
        static int? AskNumber(string question, int min, int max)
        // helper function so that we dont need to rewrite this whole thing for each separate question
        // '0' is being used for skip which is fine, because if we for example want a movie
        // that has rating 0 or above, its all movies anyway, filter does not have to apply
        {
            Console.WriteLine(question);
            if (int.TryParse(Console.ReadLine(), out int response))
            // we make sure that the input from user is valid
            {
                if (response == 0) return null;
                // we use '0' for 'skip'
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
        // takes bool prompts. y/yes/n/no in either lower or upper case is okay,
        // any other input is 'null'
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
            // write out all the distinct genres we generated earlier
            Console.WriteLine($"{i+1}. {genres[i]}");
        }

        //beauty of the helper functions
        int? genre = AskNumber("Choose a number", 0, genres.Count);
        int? minYear = AskNumber("From what year? (0 to skip)", 1900, 2026);
        int? minRating = AskNumber("Minimum rating? (0 to skip)", 0, 10);
        bool? prefersShort = AskYesNo("Do you prefer shorter films?");
        bool? hasCzechDub = AskYesNo("Czech dub only?");


        return new UserPreferences
        {
            // we listed the actual genres from 1 not 0, thats why we use - 1
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
    //returns top N movies that match our preferences the best
    //we only take czech dub as a hard filter, otherwise we just dont add points,
    //to keep as many movies in the database as possible. If a movie does not match one of our criteria but
    //matches the rest perfectly, we shouldn't rule it out completely
    // scoring signal is handled by the function ScoreMovie
    public static List<Movie> GetRecommendations(List<Movie> movies, UserPreferences prefs, int topN = 3)
    {
        List<Movie> candidates = new List<Movie>();
        if (prefs.HasCzechDub == true)
        // czech dub is the only category where I decided to be strict, if movies does not
        // have czech dub, we ignore it completely
        {
            for (int i = 0; i < movies.Count; i++)
            {
                if (movies[i].HasCzechDub) candidates.Add(movies[i]);
            }
        }
        else candidates = movies;

        // sort the results high to low, return top N movies ScoreMovie function returns
        return candidates.OrderByDescending(movie => ScoreMovie(movie, prefs)).Take(topN).ToList();

        
    }

    private static double ScoreMovie(Movie movie, UserPreferences prefs)
    {
        //scores one movie at the time with our preferences. Higher the result, the better
        //weights are tuned by hand, might need optimization later, genre currently dominates

        double sum = 0;
        if (prefs.Genre != null && movie.Genre == prefs.Genre) sum += 40;
        // genre is the strongest signal, if I want to watch some romantical stuff I dont want to see
        // Dwayne the Rock Johnson fighting bunch of guys
        if (prefs.MinYear != null && movie.Year >= prefs.MinYear) sum += 10;
        // released in / after is a small bonus, I'm fine with watching old classics
        if (prefs.MinRating != null && movie.Rating >= prefs.MinRating) sum += (movie.Rating - prefs.MinRating.Value)*10;
        // we add bonus for rating dynanically, the hgiher it scores, the bigger bonus it schould get
        if (prefs.PrefersShortFilm == true && movie.Runtime < 90) sum += 20;
        // bonus for short films when the user prefers them.
        sum += new Random().NextDouble() * 5;
        // add some randomness so that the program is not deterministic
        return sum;
    }
}


public static class QueryEngine
{
    // 'interacting with user' mode: asks the user for genre, year, rating,
    // runtime and sort order, then filters the movie list and prints all the matches found.
    // every filter is optional, if the input is blank or malformed,
    // we skip it to not error out
    public static void Run(List<Movie> movies)
    {
        // every filter has the same structure: read input and if its not blank
        // and valid, we set filter X flag + bounds. If not, just skip

        // Genres - comma-separated list, case sensitiive
        Console.WriteLine("Select genre (lowercase, comma-separated; leave blank to skip):");
        var genreInput = Console.ReadLine();
        List<string>? genres = null;
        if (!string.IsNullOrWhiteSpace(genreInput))
            genres = genreInput.Split(',').Select(g => g.Trim()).ToList();



        // year range - must be in format YYYY-YYYY, both bounds must be correct
        Console.WriteLine("Select year range (YYYY-YYYY; leave blank to skip):");
        
        var yearInput = Console.ReadLine();
        int yearMin = 0;
        int yearMax = 0;
        bool filterYear = false;
        if (!string.IsNullOrWhiteSpace(yearInput))
        {
            var parts = yearInput.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out yearMin) && int.TryParse(parts[1], out yearMax))
                filterYear = true;
            else
                Console.WriteLine("Invalid format, skipping.");
        }

        // now we process rating range, we take doubles as a valid input, must be in format like this example: 7.0-9.5
        Console.WriteLine("Select rating range (e.g. 7.0-9.5; leave blank to skip):");

        var ratingInput = Console.ReadLine();
        double ratingMin = 0;
        double ratingMax = 0;
        bool filterRating = false;

        if (!string.IsNullOrWhiteSpace(ratingInput))
        {
            var parts = ratingInput.Split('-');
            if (parts.Length == 2 && double.TryParse(parts[0], out ratingMin) && double.TryParse(parts[1], out ratingMax))
                filterRating = true;
            else

                Console.WriteLine("Invalid format, skipping.");
        }

        // runtime range in minutes, in format 29-155 for example
        Console.WriteLine("Select runtime range in minutes (e.g. 0-120; leave blank to skip):");
        var runtimeInput = Console.ReadLine();
        int runtimeMin = 0, runtimeMax = 0;
        bool filterRuntime = false;
        if (!string.IsNullOrWhiteSpace(runtimeInput))
        {
            var parts = runtimeInput.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out runtimeMin) && int.TryParse(parts[1], out runtimeMax))
                filterRuntime = true;
            else
                Console.WriteLine("Invalid format, skipping.");
        }

        // order, this is our sort key, if there is an R in the end of users response, it means reversed (descending)
        Console.WriteLine("Order by (name/year/rating/runtime; add R to reverse e.g. ratingR; leave blank to skip):");
        var order = Console.ReadLine()?.Trim();


        // now we filter based on the inputs from user
        List<Movie> results = new List<Movie>(movies);
        if (genres != null)   results = results.Where(m => genres.Contains(m.Genre.ToLower())).ToList();
        if (filterYear)       results = results.Where(m => m.Year >= yearMin && m.Year <= yearMax).ToList();
        if (filterRating)     results = results.Where(m => m.Rating >= ratingMin && m.Rating <= ratingMax).ToList();
        if (filterRuntime)    results = results.Where(m => m.Runtime >= runtimeMin && m.Runtime <= runtimeMax).ToList();



        // sort based on 'order' key from user ('R')
        if (!string.IsNullOrWhiteSpace(order))
        {
            bool rev = order.EndsWith("R");
            string key = order.TrimEnd('R').ToLower();
            if (key == "name")    results = rev ? results.OrderByDescending(m => m.Name).ToList()    : results.OrderBy(m => m.Name).ToList();
            if (key == "year")    results = rev ? results.OrderByDescending(m => m.Year).ToList()    : results.OrderBy(m => m.Year).ToList();
            if (key == "rating")  results = rev ? results.OrderByDescending(m => m.Rating).ToList()  : results.OrderBy(m => m.Rating).ToList();
            if (key == "runtime") results = rev ? results.OrderByDescending(m => m.Runtime).ToList() : results.OrderBy(m => m.Runtime).ToList();
        }

        // print if matches found, else just say nothing found.
        var list = results.ToList();
        if (list.Count == 0) Console.WriteLine("You are too hard on the filters, no matches found.");
        else foreach (var m in list) Program.PrintMovie(m);
    }
}



class Program{


    public static void PrintMovie(Movie m)
    {
        //prints stuff about single movie in a readable format
        Console.WriteLine($"\n{m.Name} ({m.Year}) — {m.Genre} — {m.Runtime} min — {m.Rating}/10");
        Console.WriteLine($"Czech dub: {(m.HasCzechDub ? "Yes" : "No")}");
        Console.WriteLine(m.Description);
    }

    public static void Main(string[] args)
    {
        //first it loads movie databse, then it lets user pick between recommendations mode and 
        //query mode
        var movies = CsvLoader.Load("data.csv");
        
        Console.WriteLine("1. Get recommendations");
        Console.WriteLine("2. Query the database");
        

        // we route to the chosen mode. if users does not input number or number out of range,
        //return invalid input  
        if (int.TryParse(Console.ReadLine(), out int choice))
        {
            
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
            else
            {
                Console.WriteLine("Invalid input");
            }
        }
        else
        {
            Console.WriteLine("Invalid input");
        }
    }
}
